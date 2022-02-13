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
            // Runtime.UpdateFrequency = UpdateFrequency.Update100;
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
            {"Large Steel Tube", 200},
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

        public void Main()
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

            var filtered_blocks = new List<IMyTerminalBlock>();
            for (int i = 0; i < cargo_blocks.Count; ++i)
            {
                if(!CargoTargets.ContainsKey(cargo_blocks[i].CustomName))
                {
                    filtered_blocks.Add(cargo_blocks[i]);
                }
            }

            for (int i = 0; i < CargoTargets.Count; ++i)
            {
                var key = CargoTargets.Keys.ElementAt(i);
                var val = CargoTargets[key];
                FillCargo(key, val, filtered_blocks);
            }

            var lcd = GridTerminalSystem.GetBlockWithName(LcdName) as IMyTextPanel;

            lcd.WriteText(Logger.ToString(), false);
            Echo(Logger.ToString());
        }


        private void FillCargo(string cargoName, Dictionary<string, int> contents, List<IMyTerminalBlock> allCargoBlocks)
        {
            Echo("Cargo Blocks Searched: " + allCargoBlocks.Count);
            //Logger.AppendLine("Cargo Blocks Searched: " + allCargoBlocks.Count);
            Echo("Filling Cargo: " + cargoName);
            //Logger.AppendLine("Filling Cargo: " + cargoName);

            var tgt = GridTerminalSystem.GetBlockWithName(cargoName);

            Echo("Target cargo: " + tgt.CustomName);
            Logger.AppendLine("Target cargo: " + tgt.CustomName);
            StackAll(tgt);

            var tgtInventory = tgt.GetInventory(0);
            List<MyInventoryItem> tgtItems = new List<MyInventoryItem>();
            tgtInventory.GetItems(tgtItems);

            var keys = new List<string>(contents.Keys);

            for (int i = 0; i < keys.Count; ++i)
            {
                var thisItemLogger = new StringBuilder("");

                var requiredItemName = keys[i];
                Echo("Checking " + requiredItemName);
                //thisItemLogger.AppendLine("Checking " + requiredItemName);
                var requestedAmount = contents[requiredItemName];

                var currentAmount = 0;
                var targetIndex = tgtItems.Count;

                //find this item if we have it and the amount we have
                for (int j = 0; j < tgtItems.Count; ++j)
                {
                    string itemName = decodeItemName(tgtItems[j].Type.SubtypeId.ToString(),
                        tgtItems[j].Type.ToString());
                    if (itemName != requiredItemName) continue;

                    currentAmount = (int)tgtItems[j].Amount;
                    targetIndex = j;
                    break;
                }

                Echo("Currently have " + currentAmount + " of " + requiredItemName);
                //thisItemLogger.AppendLine("Currently have " + currentAmount + " of " + requiredItemName);

                //Have enough of this item, move on
                if (currentAmount >= requestedAmount)
                {
                    Echo("Have enough " + requiredItemName);
                    //thisItemLogger.AppendLine("Have enough " + requiredItemName);
                    continue;
                }

                var srcInventoryFound = false;
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
                        var srcItemName = decodeItemName(srcItem.Type.SubtypeId.ToString(),
                        srcItem.Type.ToString());

                        if (srcItemName != requiredItemName) continue;

                        srcIndex = k;
                        srcAmount = (int)srcItem.Amount;
                        break;
                    }
                    //not found, next inventory
                    if (srcIndex == -1) continue;

                    srcInventoryFound = true;

                    var numMissing = requestedAmount - currentAmount;
                    var numToTransfer = srcAmount >= numMissing ? numMissing : srcAmount;

                    var result = srcInventory.TransferItemTo(tgtInventory, srcIndex, targetIndex, true, numToTransfer);
                    if(!result)
                    {
                        Echo("Inventory Transfer failed :(");
                        //thisItemLogger.AppendLine("Inventory Transfer failed :(");
                    }

                    Echo("Attempted to transfer " + numToTransfer + " " + requiredItemName + " to cargo.");
                    //thisItemLogger.AppendLine("Attempted to transfer " + numToTransfer + " " + requiredItemName + " from " + srcBlock.CustomName + " to " + cargoName + ".");
                    tgtInventory.GetItems(tgtItems);
                    if(tgtItems.Count > targetIndex)
                    {
                        currentAmount = (int)tgtItems[targetIndex].Amount;
                        Echo("New count of " + currentAmount + " for " + tgtItems[targetIndex].Type.SubtypeId);
                        //thisItemLogger.AppendLine("New count of " + currentAmount + " for " + tgtItems[targetIndex].Type.SubtypeId);
                    }

                    if (currentAmount >= requestedAmount) break;
                }

                if(!srcInventoryFound)
                {
                    //thisItemLogger.AppendLine("Could not find any " + requiredItemName);
                }

                if (currentAmount < requestedAmount)
                {
                    thisItemLogger.AppendLine(requiredItemName + "(" + currentAmount + " / " + requestedAmount + ")");
                    //Echo(requiredItemName + "(" + delta + ")");
                }
                Echo(requiredItemName + " should have " + currentAmount + " in the cargo");
                //thisItemLogger.AppendLine(requiredItemName + " should have " + currentAmount + " in the cargo");
                Logger.Append(thisItemLogger.ToString());
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



        private static readonly Dictionary<string, string> ItemNameMap = new Dictionary<string, string>
        {
            {"Construction", "Construction Component"},
            {"MetalGrid", "Metal Grid"},
            {"InteriorPlate", "Interior Plate"},
            {"SteelPlate", "Steel Plate"},
            {"SmallTube", "Small Steel Tube"},
            {"LargeTube", "Large Steel Tube"},
            {"BulletproofGlass", "Bulletproof Glass"},
            {"Reactor", "Reactor Component"},
            {"Thrust", "Thruster Component"},
            {"GravityGenerator", "GravGen Component"},
            {"Medical", "Medical Component"},
            {"RadioCommunication", "Radio Component"},
            {"Detector", "Detector Component"},
            {"SolarCell", "Solar Cell"},
            {"PowerCell", "Power Cell"},
            {"AutomaticRifleItem", "Rifle"},
            {"AutomaticRocketLauncher", "Rocket Launcher"},
            {"WelderItem", "Welder"},
            {"Welder2Item", "Welder Lvl 2"},
            {"Welder3Item", "Welder Lvl 3"},
            {"Welder4Item", "Welder Lvl 4"},
            {"Welder5Item", "Welder Lvl 5"},
            {"AngleGrinderItem", "Grinder"},
            {"AngleGrinder2Item", "Grinder Lvl 2"},
            {"AngleGrinder3Item", "Grinder Lvl 3"},
            {"AngleGrinder4Item", "Grinder Lvl 4"},
            {"AngleGrinder5Item", "Grinder Lvl 5"},
            {"HandDrillItem", "Hand Drill"},
            {"HandDrill2Item", "Hand Drill Lvl 2"},
            {"HandDrill3Item", "Hand Drill Lvl 3"},
            {"HandDrill4Item", "Hand Drill Lvl 4"},
            {"HandDrill5Item", "Hand Drill Lvl 5"},
        };


        //Originally Stolen from TheFinalFrontier.se
        //http://thefinalfrontier.se/inventory-display-and-management/
        //Updated a little
        String decodeItemName(String name, String typeId)
        {
            if (ItemNameMap.ContainsKey(name))
            {
                return ItemNameMap[name];
            }

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