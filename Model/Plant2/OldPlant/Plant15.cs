using System;
using System.Collections.Generic;
using System.Text;
using ModelFramework;
using CSGeneral;
using System.Reflection;
using System.Collections;


public class Plant15
{
    [Link]
    Phenology Phenology = null;

    [Link]
    Component My = null;

    public string Name { get { return My.Name; } }
    public SowPlant2Type SowingData;

 #region Outputs
    [Output("Crop_Type")]
    [Param]
    public string CropType = "";
 #endregion

 #region Low level variable finding routines
    /// <summary>
    /// Return an internal plant object. PropertyName can use dot and array notation (square brackets).
    /// e.g. Leaf.MinT
    ///      Leaf.Leaves[].Live.Wt              - sums all live weights of all objects in leaves array.
    ///      Leaf.Leaves[1].Live.               - returns the live weight of the 2nd element of the leaves array.
    ///      Leaf.Leaves[Leaf.NodeNo].Live.Wt)  - returns the live weight of the leaf as specified by Leaf.NodeNo
    /// </summary>
    public double GetObject(string PropertyName)
    {
        int PosBracket = PropertyName.IndexOf('[');
        if (PosBracket == -1)
            return Convert.ToDouble(GetValueOfVariable(PropertyName));
        else
        {
            object ArrayObject = GetValueOfVariable(PropertyName.Substring(0, PosBracket));
            if (ArrayObject != null)
            {
                string RemainderOfPropertyName = PropertyName;
                string ArraySpecifier = StringManip.SplitOffBracketedValue(ref RemainderOfPropertyName, '[', ']');
                int PosRemainder = PropertyName.IndexOf("].");
                if (PosRemainder == -1)
                    throw new Exception("Invalid name path found in CompositeBiomass. Path: " + PropertyName);
                RemainderOfPropertyName = PropertyName.Substring(PosRemainder + 2);

                IList Array = (IList)ArrayObject;
                int ArrayIndex;
                if (int.TryParse(ArraySpecifier, out ArrayIndex))
                    return Convert.ToDouble(GetValueOfMember(RemainderOfPropertyName, Array[ArrayIndex]));

                else if (ArraySpecifier.Contains("."))
                {
                    int Index;
                    if (My.Get(ArraySpecifier, out Index))
                    {
                        if (Index < 0 || Index >= Array.Count)
                            throw new Exception("Invalid index of " + Index.ToString() + " found while indexing into variable: " + PropertyName);
                        return Convert.ToDouble(GetValueOfMember(RemainderOfPropertyName, Array[Index]));
                    }
                }
                else
                {
                    double Sum = 0.0;
                    for (int i = 0; i < Array.Count; i++)
                    {
                        object Obj = GetValueOfMember(RemainderOfPropertyName, Array[i]);
                        if (Obj == null)
                            throw new Exception("Cannot evaluate: " + RemainderOfPropertyName);

                        if (ArraySpecifier == "" || Utility.IsOfType(Array[i].GetType(), ArraySpecifier))
                            Sum += Convert.ToDouble(Obj);
                    }
                    return Sum;
                }
            }
            else
                throw new Exception("Cannot find property: " + PropertyName);
        return 0;
        }
    }


    private object GetValueOfVariable(string VariableName)
    {
        
        //// Look internally
        //Info I = GetMemberInfo(VariableName, this);
        //if (I != null)
        //{
        //    VariableCache.Add(VariableName, I);
        //    return I.Value;
        //}

        // Look externally
        object Data;
        if (My.Get(VariableName, out Data))
            return Data;
        
        // Still didn't find it
        throw new Exception("Cannot find variable: " + VariableName);
    }




    internal static object GetValueOfMember(string PropertyName, object Target)
    {
        object ReturnTarget;
        MemberInfo ReturnMember;
        GetMemberInfo(PropertyName, Target, out ReturnMember, out ReturnTarget);
        if (ReturnMember is FieldInfo)
            return (ReturnMember as FieldInfo).GetValue(ReturnTarget);
        else
            return (ReturnMember as PropertyInfo).GetValue(ReturnTarget, null);
    }

    /// <summary>
    /// Return the value (using Reflection) of the specified property on the specified object.
    /// Returns null if not found.
    /// </summary>
    internal static void GetMemberInfo(string PropertyName, object Target, out MemberInfo ReturnMember, out object ReturnTarget)
    {
        FieldInfo FI;
        PropertyInfo PI;
        string[] Bits = PropertyName.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < Bits.Length - 1; i++)
        {
            // First check for organs.
            bool Found = false;
            if (Target is Plant)
            {
                foreach (Organ O in (Target as Plant).Organs)
                {
                    if (O.Name == Bits[i])
                    {
                        Found = true;
                        Target = O;
                        break;
                    }
                }
            }

            if (!Found)
            {
                FI = Target.GetType().GetField(Bits[i], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (FI == null)
                {
                    PI = Target.GetType().GetProperty(Bits[i], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    if (PI == null)
                    {
                        Target = null;
                        break;
                    }
                    else
                        Target = PI.GetValue(Target, null);
                }
                else
                    Target = FI.GetValue(Target);
            }
        }

        if (Target == null)
            ReturnMember = null;
        else
        {
            // By now we should have a target - go get the field / property.
            FI = Target.GetType().GetField(Bits[Bits.Length - 1], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (FI == null)
            {
                PI = Target.GetType().GetProperty(Bits[Bits.Length - 1], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (PI == null)
                    ReturnMember = null;
                else
                    ReturnMember = PI;
            }
            else
                ReturnMember = FI;
        }
        ReturnTarget = Target;
    }

 #endregion

 #region Event handlers and publishers
    [Event]
    public event NewCropDelegate NewCrop;
    [Event]
    public event NullTypeDelegate Sowing;
    [Event]
    public event BiomassRemovedDelegate BiomassRemoved;

    [EventHandler]
    public void OnSow(SowPlant2Type Sow)
    {
        SowingData = Sow;

        // Go through all our children and find all organs.
        Organ1s.Clear();
        foreach (object ChildObject in My.ChildrenAsObjects)
        {
            Organ1 Child1 = ChildObject as Organ1;
            if (Child1 != null)
            {
                Organ1s.Add(Child1);
                if (Child1 is AboveGround)
                    Tops.Add(Child1);
            }

        }

        if (NewCrop != null)
        {
            NewCropType Crop = new NewCropType();
            Crop.crop_type = CropType;
            Crop.sender = Name;
            NewCrop.Invoke(Crop);
        } 
        
        if (Sowing != null)
            Sowing.Invoke();
    }
 #endregion


    #region Plant1 functionality

    [Link]
    NStress NStress = null;

    [Link]
    RadiationPartitioning RadiationPartitioning = null;

    [Link]
    Function NFixRate = null;

    [Link]
    CompositeBiomass TopsGreen = null;

    [Link]
    SWStress SWStress = null;

    [Link]
    Function TempStress = null;

    [Link]
    Root1 Root = null;

    [Link]
    Stem1 Stem = null;

    [Link]
    Leaf1 Leaf = null;

    [Link]
    Pod Pod = null;

    [Link]
    Grain Grain = null;

    [Link]
    Population1 Population = null;

    [Link]
    PlantSpatial1 PlantSpatial = null;

    [Link]
    GenericArbitratorXY Arbitrator1 = null;

    [Param]
    double EOCropFactor = 1.5;

    [Param]
    string NSupplyPreference = "";

    [Param]
    bool DoRetranslocationBeforeNDemand = false;

    [Input]
    double EO = 0;

    [Input]
    public DateTime Today; // for debugging.

    [Event]
    public event NewCanopyDelegate New_Canopy;

    [Event]
    public event NewPotentialGrowthDelegate NewPotentialGrowth;

    public List<Organ1> Organ1s = new List<Organ1>();
    public List<Organ1> Tops = new List<Organ1>();
    private double ext_n_demand;
    private string AverageStressMessage;

    public double TopsSWDemand
    {
        get
        {
            double SWDemand = 0.0;
            foreach (Organ1 Organ in Tops)
                SWDemand += Organ.SWDemand;
            return SWDemand;
        }
    }
    private double TopsGreenWt
    {
        get
        {
            double SWDemand = 0.0;
            foreach (Organ1 Organ in Tops)
                SWDemand += Organ.Green.Wt;
            return SWDemand;
        }
    }
    public double SWSupplyDemandRatio
    {
        get
        {
            double Demand = TopsSWDemand;
            if (Demand > 0)
                return Root.SWSupply / Demand;
            else
                return 1.0;
        }
    }

    /// <summary>
    /// Old PLANT1 compat. eventhandler. Not used in Plant2
    /// </summary>
    [EventHandler]
    public void OnPrepare()
    {
        Util.Debug("\r\nPREPARE=%s", Today.ToString("d/M/yyyy"));
        Util.Debug("       =%i", Today.DayOfYear);

        foreach (Organ1 Organ in Organ1s)
            Organ.OnPrepare();

        NStress.DoPlantNStress();
        RadiationPartitioning.DoRadiationPartition();
        foreach (Organ1 Organ in Organ1s)
            Organ.DoPotentialRUE();

        // Calculate Plant Water Demand
        double SWDemandMaxFactor = EOCropFactor * EO;
        foreach (Organ1 Organ in Organ1s)
            Organ.DoSWDemand(SWDemandMaxFactor);

        DoNDemandEstimate();

        // PUBLISH NewPotentialGrowth event.
        NewPotentialGrowthType NewPotentialGrowthData = new NewPotentialGrowthType();
        NewPotentialGrowthData.frgr = (float)Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                                        Math.Min(SWStress.OxygenDeficitPhoto, 1.0 /*PStress.Photo*/));  // FIXME
        NewPotentialGrowthData.sender = Name;
        NewPotentialGrowth.Invoke(NewPotentialGrowthData);
        Util.Debug("NewPotentialGrowth.frgr=%f", NewPotentialGrowthData.frgr);
        //Prepare_p();   // FIXME
    }

    /// <summary>
    ///  Old PLANT1 compat. process eventhandler.
    /// </summary>
    [EventHandler]
    public void OnProcess()
    {
        Util.Debug("\r\nPROCESS=%s", Today.ToString("d/M/yyyy"));
        Util.Debug("       =%i", Today.DayOfYear);
        Root.DoPlantRootDepth();
        Root.DoWaterUptake(TopsSWDemand);
        SWStress.DoPlantWaterStress(TopsSWDemand);
        Root.DoNitrogenSupply();
        Phenology.DoTimeStep();
        Stem.Morphology();
        Leaf.DoCanopyExpansion();

        foreach (Organ1 Organ in Organ1s)   // NIH - WHY IS THIS HERE!!!!?????  Not needed I hope.
            Organ.DoPotentialRUE();         // DPH - It does make a small difference!

        Arbitrator1.PartitionDM(Organ1s);
        Arbitrator1.RetranslocateDM(Organ1s);
        Leaf.Actual();
        Pod.CalcDltPodArea();
        Root.RootLengthGrowth();
        Leaf.LeafDeath();
        Leaf.LeafAreaSenescence();

        foreach (Organ1 Organ in Organ1s)
            Organ.DoSenescence();

        Root.DoSenescenceLength();
        Grain.DoNDemandGrain();

        //  g.n_fix_pot = _fixation->Potential(biomass, swStress->swDef.fixation);
        double n_fix_pot = 0;

        if (DoRetranslocationBeforeNDemand)
            Arbitrator1.DoNRetranslocate(Grain.NDemand, Organ1s);

        bool IncludeRetranslocationInNDemand = !DoRetranslocationBeforeNDemand;
        foreach (Organ1 Organ in Organ1s)
            Organ.DoNDemand(IncludeRetranslocationInNDemand);

        foreach (Organ1 Organ in Organ1s)
            Organ.DoNSenescence();
        Arbitrator1.doNSenescedRetranslocation(Organ1s);

        foreach (Organ1 Organ in Organ1s)
            Organ.DoSoilNDemand();
        // PotNFix = _fixation->NFixPot();
        double PotNFix = 0;
        Root.DoNUptake(PotNFix);

        double n_fix_uptake = Arbitrator1.DoNPartition(n_fix_pot, Organ1s);

        // DoPPartition();
        if (!DoRetranslocationBeforeNDemand)
            Arbitrator1.DoNRetranslocate(Grain.NDemand2, Tops);

        // DoPRetranslocate();
        bool PlantIsDead = Population.PlantDeath();

        foreach (Organ1 Organ in Organ1s)
            Organ.DoDetachment();

        Update();

        CheckBounds();
        SWStress.DoPlantWaterStress(TopsSWDemand);
        NStress.DoPlantNStress();
        if (AverageStressMessage != null)
        {
            Console.WriteLine(Today.ToShortDateString() + " - " + Phenology.CurrentPhase.Start);
            double biomass = 0;
            foreach (Organ1 Organ in Organ1s)
                biomass += Organ.Green.Wt + Organ.Senesced.Wt;
            Console.WriteLine("                            LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)");
            Console.WriteLine("           Above Ground Biomass = " + biomass.ToString("f2") + " (g/m^2)");
        }
        Root.UpdateWaterBalance();
    }

    [EventHandler]
    private void OnPhaseChanged(PhaseChangedType Data)
    {
        if (SWStress != null && NStress != null)
        {
            AverageStressMessage = String.Format("    {0,20} to {1,20}", Data.OldPhaseName, Data.NewPhaseName);
            AverageStressMessage += String.Format("    {0,13}{1,13}{2,13}{3,13}",
                                                  SWStress.PhotoAverage, SWStress.ExpansionAverage,
                                                  NStress.PhotoAverage, NStress.GrainAverage);
            SWStress.ResetAverage();
            NStress.ResetAverage();
        }
    }

    private void CheckBounds()
    {
        //throw new NotImplementedException();
    }

    private void Update()
    {
        // send off detached roots before root structure is updated by plant death
        Root.DisposeDetachedMaterial();

        foreach (Organ1 Organ in Organ1s)
            Organ.Update();

        // now update new canopy covers
        PlantSpatial.Density = Population.Density;
        PlantSpatial.CanopyWidth = Stem.Width;

        foreach (Organ1 Organ in Organ1s)
            Organ.DoCover();

        // Update the plant stress observers
        SWStress.Update();
        NStress.Update();
        Population.Update();

        // PUBLISH New_Canopy event
        double cover_green = 0;
        double cover_sen = 0;
        foreach (Organ1 Organ in Organ1s)
        {
            cover_green += Organ.CoverGreen;
            cover_sen += Organ.CoverSen;
        }
        double cover_tot = (1.0 - (1.0 - cover_green) * (1.0 - cover_sen));
        NewCanopyType NewCanopy = new NewCanopyType();
        NewCanopy.height = (float)Stem.Height;
        NewCanopy.depth = (float)Stem.Height;
        NewCanopy.lai = (float)Leaf.LAI;
        NewCanopy.lai_tot = (float)(Leaf.LAI + Leaf.SLAI);
        NewCanopy.cover = (float)cover_green;
        NewCanopy.cover_tot = (float)cover_tot;
        NewCanopy.sender = Name;
        New_Canopy.Invoke(NewCanopy);
        Util.Debug("NewCanopy.height=%f", NewCanopy.height);
        Util.Debug("NewCanopy.depth=%f", NewCanopy.depth);
        Util.Debug("NewCanopy.lai=%f", NewCanopy.lai);
        Util.Debug("NewCanopy.lai_tot=%f", NewCanopy.lai_tot);
        Util.Debug("NewCanopy.cover=%f", NewCanopy.cover);
        Util.Debug("NewCanopy.cover_tot=%f", NewCanopy.cover_tot);

        foreach (Organ1 Organ in Organ1s)
            Organ.DoNConccentrationLimits();

        // PUBLISH BiomassRemoved event
        DoBiomassRemoved();
    }

    private void DoBiomassRemoved()
    {
        List<Biomass> Detaching = new List<Biomass>();
        List<string> OrganNames = new List<string>();
        foreach (Organ1 Organ in Organ1s)
        {
            if (!Organ.Detaching.IsEmpty)
            {
                Detaching.Add(Organ.Detaching);
                OrganNames.Add(Organ.Name);
            }
        }
        BiomassRemovedType chopped = new BiomassRemovedType();
        chopped.crop_type = CropType;
        chopped.dm_type = new string[Detaching.Count];
        chopped.dlt_crop_dm = new float[Detaching.Count];
        chopped.dlt_dm_n = new float[Detaching.Count];
        chopped.dlt_dm_p = new float[Detaching.Count];
        chopped.fraction_to_residue = new float[Detaching.Count];

        for (int i = 0; i < Detaching.Count; i++)
        {
            chopped.dm_type[i] = OrganNames[i];
            chopped.dlt_crop_dm[i] = (float)Detaching[i].Wt;
            chopped.dlt_dm_n[i] = (float)Detaching[i].N;
            //chopped.dlt_dm_p[i] = (float) Detaching[i].P;
            chopped.fraction_to_residue[i] = 1.0f;
        }
        BiomassRemoved.Invoke(chopped);
    }


    /// <summary>
    ///  Calculate an approximate nitrogen demand for today's growth.
    ///   The estimate basically = n to fill the plant up to maximum
    ///   nitrogen concentration.
    /// </summary>
    void DoNDemandEstimate()
    {
        // Assume that the distribution of plant
        // C will be similar after today and so N demand is that
        // required to raise all plant parts to max N conc.

        double dltDmPotRue = 0;
        foreach (Organ1 Organ in Organ1s)
            dltDmPotRue += Organ.dltDmPotRue;

        foreach (Organ1 Organ in Organ1s)
            Organ.DoNDemand1Pot(dltDmPotRue);

        ext_n_demand = 0;
        foreach (Organ1 Organ in Organ1s)
            ext_n_demand += Organ.NDemand;

        //nh  use zero growth value here so that estimated n fix is always <= actual;
        double n_fix_pot = NFixRate.Value * TopsGreen.Wt * SWStress.Fixation;

        if (NSupplyPreference == "active")
        {
            // Nothing extra to do here
        }
        else if (NSupplyPreference == "fixation")
        {
            // Remove potential fixation from demand term
            ext_n_demand = ext_n_demand - n_fix_pot;
            ext_n_demand = MathUtility.Constrain(ext_n_demand, 0.0, Double.MaxValue);
        }
        else
        {
            throw new Exception("bad n supply preference");
        }
        Util.Debug("Plant.ext_n_demand=%f", ext_n_demand);
    }

    #endregion

}
   