@echo off
rem -------------------------------------------------------------
rem This batch file compiles the APSIM engine and modules
rem -------------------------------------------------------------

rem -------------------------------------------------------------
rem Need to process the datatypes.interface file and auto-
rem generate the datatypes.cpp, .h etc.
rem -------------------------------------------------------------
call MakeProject CSGeneral
call MakeProject CMPServices
call MakeProject ApsimFile
call MakeProject DataTypes

rem -------------------------------------------------------------
rem Need to compile all infrastructure projects before the main
rem bulk of projects
rem -------------------------------------------------------------
call MakeProject General
call MakeProject ApsimShared
call MakeProject Protocol
call MakeProject ComponentInterface
call MakeProject ComponentInterface2
call MakeProject DotNetComponentInterface
call MakeProject CSDotNetComponentInterface
call MakeProject FortranInfrastructure
call MakeProject FortranComponentInterface
call MakeProject FortranComponentInterface2
call MakeProject CropTemplate
call MakeProject VBMet
call MakeProject DotNetProxies

rem -------------------------------------------------------------
rem Now we can compile all the rest of the APSIM projects.
rem -------------------------------------------------------------
call MakeProject Accum
call MakeProject AgPasture
call MakeProject Apsim
call MakeProject ApsimToSim
call MakeProject Canopy
call MakeProject Clock
call MakeProject ConToApsim
call MakeProject ConToSim
call MakeProject CropMod
call MakeProject Eo
call MakeProject Erosion
call MakeProject Fertiliser
call MakeProject Grasp
call MakeProject Graz
call MakeProject Growth
call MakeProject Input
call MakeProject Irrigation
call MakeProject Log
call MakeProject Maize
call MakeProject Manager
call MakeProject Map
call MakeProject MicroMet
call MakeProject Millet
call MakeProject Operations
call MakeProject Oryza
call MakeProject Ozcot
call MakeProject Parasite
call MakeProject Pasture
call MakeProject PatchInput
call MakeProject Plant
call MakeProject Plant2
call MakeProject Pond
call MakeProject ProtocolManager
call MakeProject Report
call MakeProject Root
call MakeProject SiloInput
call MakeProject SOI
call MakeProject SoilErosion
call MakeProject SoilN
call MakeProject SoilNitrogen
call MakeProject SoilP
call MakeProject SoilTemp
call MakeProject SoilTemp2
call MakeProject SoilWat
call MakeProject Solute
call MakeProject Sorghum
call MakeProject Stock
call MakeProject Sugar
call MakeProject Supplement
call MakeProject Surface
call MakeProject SurfaceOM
call MakeProject SurfaceTemp
call MakeProject SWIM2
call MakeProject SWIM3
call MakeProject SysBal
call MakeProject TclLink
call MakeProject Tracker
call MakeProject Tree
call MakeProject UrinePatch
call MakeProject VenLink
call MakeProject WaterSupply
call MakeProject YieldProphet
call MakeProject DDRules
call MakeProject FarmSimGraze

call MakeProject SetXMLValue
call MakeProject Plant2Documentation
call MakeProject RunMacro
call MakeProject UpdateDotNetProxies
