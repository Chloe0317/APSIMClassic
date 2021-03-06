﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModelFramework;

class RadiationPartitioning
{
    [Param]
    double FractIncidentRadn = 0.0;

    [Param]
    string[] RadiationPartitioningOrder = null;

    [Link]
    Component My = null;

    [Input]
    double Radn = 0;

    public void DoRadiationPartition()
    {
        double incomingSolarRadiation = Radn * FractIncidentRadn;

        foreach (string OrganName in RadiationPartitioningOrder)
            {
                Organ1 Organ = My.LinkByName(OrganName) as Organ1;

                // calc the total interception from this part - what is left is transmitted
                // to the other parts.
                incomingSolarRadiation -= Organ.interceptRadiation(incomingSolarRadiation);
            }
        Util.Debug("RadiationPartitioning.IncomingSolarRadiation=%f", incomingSolarRadiation);
    }

}

