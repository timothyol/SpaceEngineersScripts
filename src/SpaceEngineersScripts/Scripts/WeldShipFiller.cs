#region Stuff

using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace SpaceEngineersScripting.WeldShipFiller
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
        
        //Weldship Filler

        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private static StringBuilder Logger = null;
        private const string LcdName = "LCD FillInventory Status";

        private const string cargoNameA = "Weldship Cargo A";
        private const string cargoNameB = "Weldship Cargo B";
        private const string cargoNameC = "Weldship Cargo C";

        private readonly static string[] targetCargoNames = new[] { cargoNameA, cargoNameB, cargoNameC };

        private readonly static Dictionary<string, int> CargoAContents = new Dictionary<string, int>{
    {"Steel Plate", 3500},
    {"Interior Plate", 2000},
    {"Construction Component", 3500},
    {"Small Steel Tube", 1000},
    {"Computer", 4000}
};

        private readonly static Dictionary<string, int> CargoBContents = new Dictionary<string, int>{
    {"Motor", 1000},
    {"Metal Grid", 750},
    {"Bulletproof Glass", 300},
    {"Display", 500},
    {"Large Steel Tube", 200}
};

        private readonly static Dictionary<string, int> CargoCContents = new Dictionary<string, int>{
    {"Reactor Component", 100},
    {"Radio Component", 100},
    {"Thruster Component", 100},
    {"Detector Component", 100},
    {"GravGen Component", 100},
    {"Medical Component", 50}
};

        private readonly static Dictionary<string, Dictionary<string, int>> CargoTargets = new Dictionary<string, Dictionary<string, int>>
        {
            { cargoNameA, CargoAContents },
            { cargoNameB, CargoBContents },
            { cargoNameC, CargoCContents }
        };

        public void Main(string argument, UpdateType updateSource)
        {
            Logger = new StringBuilder();
            Logger.AppendLine("Missing Components: ");
            Logger.AppendLine();

            //Get inventories; cargo blocks, connectors, assemblers    
            var cargo_blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargo_blocks);

            var ass_blocks = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(ass_blocks);

            var conn_blocks = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(conn_blocks);

            cargo_blocks.InsertRange(0, ass_blocks);
            cargo_blocks.InsertRange(0, conn_blocks);

            for (int i = 0; i < CargoTargets.Count; ++i)
            {
                var key = CargoTargets.Keys.ElementAt(i);
                var val = CargoTargets[key];
                FillCargo(key, val, cargo_blocks);
            }

            var lcd = GridTerminalSystem.GetBlockWithName(LcdName) as IMyTextPanel;

            //lcd.WritePublicText(Logger.ToString(), false);
            lcd.WriteText(Logger.ToString(), false);
            Echo(Logger.ToString());
        }


        private void FillCargo(string cargoName, Dictionary<string, int> contents, List<IMyTerminalBlock> allCargoBlocks)
        {
            var tgt = GridTerminalSystem.GetBlockWithName(cargoName);
            StackAll(tgt);

            var tgtInventory = tgt.GetInventory(0);
            List<MyInventoryItem> tgtItems = new List<MyInventoryItem>();
            tgtInventory.GetItems(tgtItems);

            var keys = new List<string>(contents.Keys);

            for (int i = 0; i < keys.Count; ++i)
            {
                var requiredItemName = keys[i];
                var requestedAmount = contents[requiredItemName];

                var currentAmount = 0;
                var targetIndex = tgtItems.Count;

                //find this item if we have it and the amount we have
                for (int j = 0; j < tgtItems.Count; ++j)
                {
                    string itemName = decodeItemName(tgtItems[j].Type.ToString(),
                        tgtItems[j].Type.ToString());
                    if (itemName != requiredItemName) continue;

                    currentAmount = (int)tgtItems[j].Amount;
                    targetIndex = j;
                    break;
                }

                //Have enough of this item, move on
                if (currentAmount >= requestedAmount) continue;

                for (int j = 0; j < allCargoBlocks.Count; ++j)
                {
                    var srcBlock = allCargoBlocks[j];

                    //don't move from the blocks we're trying to fill
                    if (targetCargoNames.Contains(srcBlock.CustomName))
                        continue;

                    var srcInventory = srcBlock.GetInventory(0);
                    if (srcBlock is IMyAssembler)
                        srcInventory = srcBlock.GetInventory(1);

                    var srcItems = new List<MyInventoryItem>();
                    srcInventory.GetItems(srcItems);

                    var srcIndex = -1;
                    var srcAmount = 0;

                    for (int k = 0; k < srcItems.Count; ++k)
                    {
                        var srcItem = srcItems[k];
                        var srcItemName = decodeItemName(srcItem.Type.ToString(),
                        srcItem.Type.ToString());

                        if (srcItemName != requiredItemName) continue;

                        srcIndex = k;
                        srcAmount = (int)srcItem.Amount;
                        break;
                    }
                    //not found, next inventory
                    if (srcIndex == -1) continue;

                    var numMissing = requestedAmount - currentAmount;
                    var numToTransfer = srcAmount >= numMissing ? numMissing : srcAmount;

                    srcInventory.TransferItemTo(tgtInventory, srcIndex, targetIndex, true, numToTransfer);

                    currentAmount += numToTransfer;

                    if (currentAmount >= requestedAmount) break;
                }

                if (currentAmount < requestedAmount)
                {
                    var delta = requestedAmount - currentAmount;
                    Logger.AppendLine(requiredItemName + "(" + delta + ")");
                    //Echo(requiredItemName + "(" + delta + ")");
                }
            }
        }


        private void StackAll(IMyTerminalBlock cargo)
        {
            var reset = true;

            while (reset)
            {
                var firstSlots = new Dictionary<string, int>();
                var inv = cargo.GetInventory(0);
                var items = new List<MyInventoryItem>();
                inv.GetItems(items);

                reset = false;
                for (int i = 0; i < items.Count; ++i)
                {
                    var item = items[i];
                    var itemName = decodeItemName(item.Type.ToString(), item.Type.ToString());
                    if (firstSlots.ContainsKey(itemName))
                    {
                        var targetIndex = firstSlots[itemName];
                        var amount = item.Amount;
                        inv.TransferItemTo(inv, i, targetIndex, true, amount);

                        reset = true;
                        break;
                    }
                    else
                    {
                        firstSlots.Add(itemName, i);
                    }
                }
            }
        }



        //Originally Stolen from TheFinalFrontier.se
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