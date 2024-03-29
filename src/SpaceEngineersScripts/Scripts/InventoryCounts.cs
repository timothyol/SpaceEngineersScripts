﻿#region Stuff

using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace SpaceEngineersScripting.InventoryCounts
{
    //public interface IMyShipMergeBlock
    //{
    //    void SetValueBoolean(String foo, bool bar);
    //}

    public static class ElementAtExtensionyBoi
    {
        public static T ElementAt<T, T2>(this Dictionary<T, T2>.KeyCollection kc, int i)
        {
            return default(T);
        }
    }

    class Program
    {
        IMyTerminalBlock Me;
        void Echo(string line)
        {
            Console.WriteLine(line);
        }
        IMyGridTerminalSystem GridTerminalSystem = null;
        private class Runtime
        {
            public static UpdateFrequency UpdateFrequency;
        }

        #endregion
        /*******************************************************************
        ****************Copy from below this comment************************/

        //Inventory Counts
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private class BlueprintNames
        {
            public const string BulletproofGlass = "MyObjectBuilder_BlueprintDefinition/BulletproofGlass";
            public const string ComputerComponent = "MyObjectBuilder_BlueprintDefinition/ComputerComponent";
            public const string ConstructionComponent = "MyObjectBuilder_BlueprintDefinition/ConstructionComponent";
            public const string DetectorComponent = "MyObjectBuilder_BlueprintDefinition/DetectorComponent";
            public const string Display = "MyObjectBuilder_BlueprintDefinition/Display";
            public const string ExplosivesComponent = "MyObjectBuilder_BlueprintDefinition/ExplosivesComponent";
            public const string GirderComponent = "MyObjectBuilder_BlueprintDefinition/GirderComponent";
            public const string GravityGeneratorComponent = "MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent";
            public const string InteriorPlate = "MyObjectBuilder_BlueprintDefinition/InteriorPlate";
            public const string LargeTube = "MyObjectBuilder_BlueprintDefinition/LargeTube";
            public const string MedicalComponent = "MyObjectBuilder_BlueprintDefinition/MedicalComponent";
            public const string MetalGrid = "MyObjectBuilder_BlueprintDefinition/MetalGrid";
            public const string Missile200mm = "MyObjectBuilder_BlueprintDefinition/Missile200mm";
            public const string MotorComponent = "MyObjectBuilder_BlueprintDefinition/MotorComponent";
            public const string NATO_25x184mmMagazine = "MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine";
            public const string NATO_5p56x45mmMagazine = "MyObjectBuilder_BlueprintDefinition/NATO_5p56x45mmMagazine";
            public const string PowerCell = "MyObjectBuilder_BlueprintDefinition/PowerCell";
            public const string RadioCommunicationComponent = "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent";
            public const string ReactorComponent = "MyObjectBuilder_BlueprintDefinition/ReactorComponent";
            public const string SmallTube = "MyObjectBuilder_BlueprintDefinition/SmallTube";
            public const string SolarCell = "MyObjectBuilder_BlueprintDefinition/SolarCell";
            public const string SteelPlate = "MyObjectBuilder_BlueprintDefinition/SteelPlate";
            public const string Superconductor = "MyObjectBuilder_BlueprintDefinition/Superconductor";
            public const string ThrustComponent = "MyObjectBuilder_BlueprintDefinition/ThrustComponent";
            public const string AngleGrinder = "MyObjectBuilder_BlueprintDefinition/AngleGrinder";
            public const string AngleGrinder2 = "MyObjectBuilder_BlueprintDefinition/AngleGrinder2";
            public const string AngleGrinder3 = "MyObjectBuilder_BlueprintDefinition/AngleGrinder3";
            public const string AngleGrinder4 = "MyObjectBuilder_BlueprintDefinition/AngleGrinder4";
            public const string HandDrill = "MyObjectBuilder_BlueprintDefinition/HandDrill";
            public const string HandDrill2 = "MyObjectBuilder_BlueprintDefinition/HandDrill2";
            public const string HandDrill3 = "MyObjectBuilder_BlueprintDefinition/HandDrill3";
            public const string HandDrill4 = "MyObjectBuilder_BlueprintDefinition/HandDrill4";
            public const string Welder = "MyObjectBuilder_BlueprintDefinition/Welder";
            public const string Welder2 = "MyObjectBuilder_BlueprintDefinition/Welder2";
            public const string Welder3 = "MyObjectBuilder_BlueprintDefinition/Welder3";
            public const string Welder4 = "MyObjectBuilder_BlueprintDefinition/Welder4";
            public const string AutomaticRifle = "MyObjectBuilder_BlueprintDefinition/AutomaticRifle";
            public const string PreciseAutomaticRifle = "MyObjectBuilder_BlueprintDefinition/PreciseAutomaticRifle";
            public const string RapidFireAutomaticRifle = "MyObjectBuilder_BlueprintDefinition/RapidFireAutomaticRifle";
            public const string UltimateAutomaticRifle = "MyObjectBuilder_BlueprintDefinition/UltimateAutomaticRifle";
            public const string HydrogenBottle = "MyObjectBuilder_BlueprintDefinition/HydrogenBottle";
            public const string OxygenBottle = "MyObjectBuilder_BlueprintDefinition/OxygenBottle";
            public const string Canvas = "MyObjectBuilder_BlueprintDefinition/Canvas";
        }

        //Name of lcd panel to a list of names of items to display on that panel
        private readonly Dictionary<string, List<string>> ItemNamesFilter = new Dictionary<string, List<string>>() {
            { "LCD Panel Common Comps", new List<string> {
                "Steel Plate", "Construction Component", "Interior Plate", "Small Steel Tube", "Large Steel Tube", "Metal Grid", "Bulletproof Glass", "Display", "Girder", "Superconductor", "Canvas", "Computer", "Explosives", "Motor"}},
            { "LCD Panel Spec Comps", new List<string> {"Detector Component", "Medical Component", "Reactor Component", "Radio Component", "Solar Cell", "Power Cell", "GravGen Component", "Thruster Component"}},
            { "LCD Panel Ingots", new List<string> { "Magnesium Powder", "Gravel", "Silicon Wafer", "Nickel Ingot", "Iron Ingot", "Ice", "Platinum Ingot", "Uranium Ingot", "Gold Ingot", "Silver Ingot", "Cobalt Ingot" } },
            { "LCD Panel Ores", new List<string> {"Ice", "Iron Ore", "Nickel Ore", "Silicon Ore", "Gold Ore", "Silver Ore", "Uranium Ore", "Stone", "Magnesium Ore", "Cobalt Ore", "Platinum Ore"} },
            { "LCD Panel Debug", new List<string> {} }
        };

        private const String PrimaryAssName = "Assembler 6 primary";

        private class ItemMinInfo
        {
            public string Name;
            public int MinCount;
            public string BlueprintName;

            public ItemMinInfo(string name, int minCount, string blueprintName)
            {
                Name = name;
                MinCount = minCount;
                BlueprintName = blueprintName;
            }
        }

        private readonly List<ItemMinInfo> ItemMinimums = new List<ItemMinInfo>()
        {
            new ItemMinInfo("Steel Plate", 30000, BlueprintNames.SteelPlate ),
            new ItemMinInfo("Construction Component", 20000, BlueprintNames.ConstructionComponent ),
            new ItemMinInfo("Interior Plate", 10000, BlueprintNames.InteriorPlate ),
            new ItemMinInfo("Small Steel Tube", 10000, BlueprintNames.SmallTube ),
            new ItemMinInfo("Large Steel Tube", 10000, BlueprintNames.LargeTube ),
            new ItemMinInfo("Metal Grid", 10000, BlueprintNames.MetalGrid ),
            new ItemMinInfo("Bulletproof Glass", 10000, BlueprintNames.BulletproofGlass ),
            new ItemMinInfo("Display", 10000, BlueprintNames.Display ),
            new ItemMinInfo("Girder", 10000, BlueprintNames.GirderComponent ),
            new ItemMinInfo("Superconductor", 1000, BlueprintNames.Superconductor ),
            //new ItemMinInfo("Canvas", 1000, BlueprintNames.Canvas ),
            new ItemMinInfo("Computer", 20000, BlueprintNames.ComputerComponent ),
            new ItemMinInfo("Explosives", 0, BlueprintNames.ExplosivesComponent ),
            new ItemMinInfo("Motor", 10000, BlueprintNames.MotorComponent ),
            new ItemMinInfo("Detector Component", 1000, BlueprintNames.DetectorComponent ),
            new ItemMinInfo("Medical Component", 1000, BlueprintNames.MedicalComponent ),
            new ItemMinInfo("Reactor Component", 1000, BlueprintNames.ReactorComponent ),
            new ItemMinInfo("Radio Component", 1000, BlueprintNames.RadioCommunicationComponent ),
            new ItemMinInfo("Solar Cell", 1000, BlueprintNames.SolarCell ),
            new ItemMinInfo("Power Cell", 1000, BlueprintNames.PowerCell ),
            new ItemMinInfo("GravGen Component", 1000, BlueprintNames.GravityGeneratorComponent ),
            new ItemMinInfo("Thruster Component",  1000, BlueprintNames.ThrustComponent )
        };

        private const bool ShouldQueueItems = false;

        public void Main(string argument, UpdateType updateSource)
        {
            //Get inventories; cargo blocks, connectors, drills, etc  
            var cargo_blocks = new List<IMyTerminalBlock>();

            AddBlocks<IMyCargoContainer>(cargo_blocks);
            AddBlocks<IMyShipConnector>(cargo_blocks);
            AddBlocks<IMyAssembler>(cargo_blocks);
            AddBlocks<IMyRefinery>(cargo_blocks);
            AddBlocks<IMyReactor>(cargo_blocks);
            AddBlocks<IMyShipWelder>(cargo_blocks);
            AddBlocks<IMyShipDrill>(cargo_blocks);
            AddBlocks<IMyShipGrinder>(cargo_blocks);
            AddBlocks<IMyGasGenerator>(cargo_blocks);


            var itemCounts = new Dictionary<string, int>();
            for (int i = 0; i < cargo_blocks.Count; ++i)
            {
                var cb = cargo_blocks[i];

                //var inventories = GetInvIndices(cb);
                var inventories = cb.InventoryCount;

                for (int k = 0; k < inventories; ++k)
                {
                    var inv = cb.GetInventory(k);
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    inv.GetItems(items);

                    for (int j = 0; j < items.Count; j++)
                    {
                        var itemName = decodeItemName(items[j].Type.SubtypeId.ToString(), items[j].Type.TypeId.ToString());
                        var currentAmount = (int)items[j].Amount;
                        if (itemCounts.ContainsKey(itemName))
                        {
                            var oldCnt = itemCounts[itemName];
                            var newCnt = oldCnt + currentAmount;
                            itemCounts[itemName] = newCnt;
                        }
                        else
                        {
                            itemCounts.Add(itemName, currentAmount);
                        }
                    }
                }
            }

            //Print item counts to whatever LCD screen
            for (int j = 0; j < ItemNamesFilter.Keys.Count; ++j)
            {
                var lcdPanel = ItemNamesFilter.Keys.ElementAt(j);
                var itemNames = ItemNamesFilter[lcdPanel];

                if (itemNames.Count > 0)
                {
                    for (int i = 0; i < itemNames.Count; ++i)
                    {
                        var name = itemNames[i];
                        if (!itemCounts.ContainsKey(name))
                        {
                            itemCounts.Add(name, 0);
                        }
                    }
                }

                var msg = "";
                for (int i = 0; i < itemCounts.Count; ++i)
                {
                    var key = itemCounts.Keys.ElementAt(i);
                    var count = itemCounts[key];

                    if (itemNames.Count > 0 && !itemNames.Contains(key))
                    {
                        continue;
                    }

                    msg += key + ": " + FormatNumber(count) + "\r\n";
                }

                Echo(msg);
                PrintLcd(lcdPanel, msg);
            }

            if(ShouldQueueItems)
            {
                //compare item counts 
                var primeAss = GridTerminalSystem.GetBlockWithName(PrimaryAssName) as IMyAssembler;
                if (primeAss == null)
                {
                    Echo("Primary assembler not found.");
                    return;
                }

                //
                primeAss.ClearQueue();

                //Get queued item counts
                var asses = new List<IMyAssembler>();
                GridTerminalSystem.GetBlocksOfType<IMyAssembler>(asses);

                var queuedCounts = new Dictionary<String, int>();
                for (int i = 0; i < asses.Count; ++i)
                {
                    var ass = asses[i];
                    var queue = new List<MyProductionItem>();
                    ass.GetQueue(queue);

                    for (int j = 0; j < queue.Count; ++j)
                    {
                        var qi = queue[j];
                        var decodedName = DecodeBlueprintName(qi.BlueprintId.ToString());
                        if (String.IsNullOrEmpty(decodedName)) continue;
                        if (queuedCounts.ContainsKey(decodedName))
                        {
                            var count = queuedCounts[decodedName];
                            count += (int)qi.Amount;
                            queuedCounts[decodedName] = count;
                        }
                        else
                        {
                            var count = (int)qi.Amount;
                            queuedCounts.Add(decodedName, count);
                        }
                    }
                }

                for (int i = 0; i < ItemMinimums.Count; ++i)
                {
                    var name = ItemMinimums[i].Name;
                    var minCount = ItemMinimums[i].MinCount;
                    var bpName = ItemMinimums[i].BlueprintName;

                    var haveCount = itemCounts.ContainsKey(name) ? itemCounts[name] : 0;
                    var queuedCount = queuedCounts.ContainsKey(name) ? queuedCounts[name] : 0;

                    var needCount = minCount - (haveCount + queuedCount);

                    if (needCount > 0)
                    {
                        Echo("Item: " + name);
                        Echo(String.Format("Have: {0}; Queued: {1}; Need: {2}", haveCount, queuedCount, minCount));

                        var bp = GetBlueprint(bpName);

                        if (primeAss != null)
                        {
                            primeAss.AddQueueItem(bp, (VRage.MyFixedPoint)needCount);
                        }
                    }
                }
            }
        }

        private int[] GetInvIndices(IMyTerminalBlock block)
        {
            if (block is IMyAssembler) return new[] { 0, 1 };
            if (block is IMyRefinery) return new[] { 0, 1 };

            return new[] { 0 };
        }

        private void AddBlocks<T>(List<IMyTerminalBlock> blockList) where T : class
        {
            var temp = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(temp);
            blockList.AddRange(temp);
        }

        private void PrintLcd(String lcdName, String msg)
        {
            var lcds = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcds);

            if (lcds.Count == 0)
            {
                Echo("Lcd Block " + lcdName + " not found.");
                return;
            }

            for (int i = 0; i < lcds.Count; ++i)
            {
                var lcd = lcds[i];

                if (lcd.CustomName != lcdName) continue;

                if (lcd != null)
                    lcd.WriteText(msg, false);
            }
        }

        private string FormatNumber(long num)
        {
            if (num > 10000000000)
                return (num / 1000000000) + "G";
            else if (num > 1000000000)
                return String.Format("{0:0.00}", (double)num / 1000000000) + "G";
            else if (num > 100000000)
                return (num / 1000000) + "M";
            else if (num > 10000000)
                return String.Format("{00:0.0}", (float)num / 1000000f) + "M";
            else if (num > 1000000)
                return String.Format("{0:0.00}", (float)num / 1000000f) + "M";
            else if (num > 100000)
                return (num / 1000) + "K";
            else if (num > 10000)
                return String.Format("{0:00.0}", (float)num / 1000f) + "K";
            else if (num > 1000)
                return String.Format("{0:0.00}", (float)num / 1000f) + "K";
            else
                return num.ToString();
        }

        private MyDefinitionId GetBlueprint(String name)
        {
            return MyDefinitionId.Parse(name);
        }

        private string DecodeBlueprintName(String name)
        {
            if (name.Equals(BlueprintNames.BulletproofGlass)) return "Bulletproof Glass";
            if (name.Equals(BlueprintNames.ComputerComponent)) return "Computer";
            if (name.Equals(BlueprintNames.ConstructionComponent)) return "Construction Component";
            if (name.Equals(BlueprintNames.DetectorComponent)) return "Detector Component";
            if (name.Equals(BlueprintNames.Display)) return "Display";
            if (name.Equals(BlueprintNames.ExplosivesComponent)) return "Explosives";
            if (name.Equals(BlueprintNames.GirderComponent)) return "Girder";
            if (name.Equals(BlueprintNames.GravityGeneratorComponent)) return "GravGen Component";
            if (name.Equals(BlueprintNames.InteriorPlate)) return "Interior Plate";
            if (name.Equals(BlueprintNames.LargeTube)) return "Large Steel Tube";
            if (name.Equals(BlueprintNames.MedicalComponent)) return "Medical Component";
            if (name.Equals(BlueprintNames.MetalGrid)) return "Metal Grid";
            if (name.Equals(BlueprintNames.Missile200mm)) return "Missile";
            if (name.Equals(BlueprintNames.MotorComponent)) return "Motor";
            if (name.Equals(BlueprintNames.NATO_25x184mmMagazine)) return "";
            if (name.Equals(BlueprintNames.NATO_5p56x45mmMagazine)) return "";
            if (name.Equals(BlueprintNames.PowerCell)) return "Power Cell";
            if (name.Equals(BlueprintNames.RadioCommunicationComponent)) return "Radio Component";
            if (name.Equals(BlueprintNames.ReactorComponent)) return "Reactor Component";
            if (name.Equals(BlueprintNames.SmallTube)) return "Small Steel Tube";
            if (name.Equals(BlueprintNames.SolarCell)) return "Solar Cell";
            if (name.Equals(BlueprintNames.SteelPlate)) return "Steel Plate";
            if (name.Equals(BlueprintNames.Superconductor)) return "Superconductor";
            if (name.Equals(BlueprintNames.ThrustComponent)) return "Thruster Component";
            if (name.Equals(BlueprintNames.AngleGrinder)) return "Grinder";
            if (name.Equals(BlueprintNames.AngleGrinder2)) return "";
            if (name.Equals(BlueprintNames.AngleGrinder3)) return "";
            if (name.Equals(BlueprintNames.AngleGrinder4)) return "";
            if (name.Equals(BlueprintNames.HandDrill)) return "Hand Drill";
            if (name.Equals(BlueprintNames.HandDrill2)) return "";
            if (name.Equals(BlueprintNames.HandDrill3)) return "";
            if (name.Equals(BlueprintNames.HandDrill4)) return "";
            if (name.Equals(BlueprintNames.Welder)) return "Welder";
            if (name.Equals(BlueprintNames.Welder2)) return "";
            if (name.Equals(BlueprintNames.Welder3)) return "";
            if (name.Equals(BlueprintNames.Welder4)) return "";
            if (name.Equals(BlueprintNames.AutomaticRifle)) return "Rifle";
            if (name.Equals(BlueprintNames.PreciseAutomaticRifle)) return "";
            if (name.Equals(BlueprintNames.RapidFireAutomaticRifle)) return "";
            if (name.Equals(BlueprintNames.UltimateAutomaticRifle)) return "";
            if (name.Equals(BlueprintNames.HydrogenBottle)) return "";
            if (name.Equals(BlueprintNames.OxygenBottle)) return "";

            return "";
        }

        //Stolen from TheFinalFrontier.se
        //http://thefinalfrontier.se/inventory-display-and-management/
        //Updated a little
        String decodeItemName(String name, String typeId)
        {
            if (name.Equals("Construction")) { return "Construction Component"; }
            if (name.Equals("MetalGrid")) { return "Metal Grid"; }
            if (name.Equals("InteriorPlate")) { return "Interior Plate"; }
            if (name.Equals("SteelPlate")) { return "Steel Plate"; }
            if (name.Equals("SmallTube")) { return "Small Steel Tube"; }
            if (name.Equals("LargeTube")) { return "Large Steel Tube"; }
            if (name.Equals("BulletproofGlass")) { return "Bulletproof Glass"; }
            if (name.Equals("Reactor")) { return "Reactor Component"; }
            if (name.Equals("Thrust")) { return "Thruster Component"; }
            if (name.Equals("GravityGenerator")) { return "GravGen Component"; }
            if (name.Equals("Medical")) { return "Medical Component"; }
            if (name.Equals("RadioCommunication")) { return "Radio Component"; }
            if (name.Equals("Detector")) { return "Detector Component"; }
            if (name.Equals("SolarCell")) { return "Solar Cell"; }
            if (name.Equals("PowerCell")) { return "Power Cell"; }
            if (name.Equals("AutomaticRifleItem")) { return "Rifle"; }
            if (name.Equals("AutomaticRocketLauncher")) { return "Rocket Launcher"; }
            if (name.Equals("WelderItem")) { return "Welder"; }
            if (name.Equals("Welder2Item")) { return "Welder Lvl 2"; }
            if (name.Equals("Welder3Item")) { return "Welder Lvl 3"; }
            if (name.Equals("Welder4Item")) { return "Welder Lvl 4"; }
            if (name.Equals("Welder5Item")) { return "Welder Lvl 5"; }
            if (name.Equals("AngleGrinderItem")) { return "Grinder"; }
            if (name.Equals("AngleGrinder2Item")) { return "Grinder Lvl 2"; }
            if (name.Equals("AngleGrinder3Item")) { return "Grinder Lvl 3"; }
            if (name.Equals("AngleGrinder4Item")) { return "Grinder Lvl 4"; }
            if (name.Equals("AngleGrinder5Item")) { return "Grinder Lvl 5"; }
            if (name.Equals("HandDrillItem")) { return "Hand Drill"; }
            if (name.Equals("HandDrill2Item")) { return "Hand Drill Lvl 2"; }
            if (name.Equals("HandDrill3Item")) { return "Hand Drill Lvl 3"; }
            if (name.Equals("HandDrill4Item")) { return "Hand Drill Lvl 4"; }
            if (name.Equals("HandDrill5Item")) { return "Hand Drill Lvl 5"; }
            if (typeId.EndsWith("_Ore"))
            {
                if (name.Equals("Stone"))
                {
                    return name;
                }
                else if (name.Equals("Ice"))
                {
                    return name;
                }
                return name + " Ore";
            }
            if (typeId.EndsWith("_Ingot"))
            {
                if (name.Equals("Stone"))
                {
                    return "Gravel";
                }
                if (name.Equals("Magnesium"))
                {
                    return name + " Powder";
                }
                if (name.Equals("Silicon"))
                {
                    return name + " Wafer";
                }
                return name + " Ingot";
            }
            return name;
        }




        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}