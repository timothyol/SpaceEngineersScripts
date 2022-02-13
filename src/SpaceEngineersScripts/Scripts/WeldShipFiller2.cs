#region Stuff - do not copy into space engineers
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace SpaceEngineersScripting.WeldShipFiller2
{
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

        //Weldship Filler, where you list your source inventories manually

        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private static StringBuilder Logger = null;
        private const string LcdName = "LCD FillInventory Status 2";

        private const string cargoNameA = "Weldship Cargo A2";
        private const string cargoNameB = "Weldship Cargo B2";
        private const string cargoNameC = "Weldship Cargo C2";

        private const string sourceContainerName = "Large Cargo Container Base";

        private readonly static string[] targetCargoNames = new[] { cargoNameA, cargoNameB, cargoNameC };

        private readonly static Dictionary<string, int> CargoAContents = new Dictionary<string, int>{
            {"Steel Plate", 3500},
            {"Interior Plate", 2000},
            {"Construction Component", 3500},
            {"Small Steel Tube", 1000},
            {"Computer", 4000}
        };

        private readonly static Dictionary<string, int> CargoBContents = new Dictionary<string, int>{
            {"Motor", 500},
            {"Metal Grid", 250},
            {"Bulletproof Glass", 100},
            {"Display", 300},
            {"Large Steel Tube", 400},
        };

        private readonly static Dictionary<string, int> CargoCContents = new Dictionary<string, int>{
            {"Reactor Component", 100},
            {"Radio Component", 100},
            {"Thruster Component", 100},
            {"Detector Component", 100},
            {"GravGen Component", 20},
            {"Medical Component", 50}
        };

        private readonly static Dictionary<string, Dictionary<string, int>> CargoTargets = new Dictionary<string, Dictionary<string, int>>
        {
            //{ cargoNameA, TestContents },
            { cargoNameA, CargoAContents },
            { cargoNameB, CargoBContents },
            { cargoNameC, CargoCContents }
        };

        private class ItemToBeFulfilled
        {
            public int Required;
            public int CurrentCount;
            public IMyInventory TargetInventory;
            public int? InventoryIndex;
        }

        private class IndexAndCount
        {
            public IndexAndCount(int? _index, int _count)
            {
                index = _index;
                count = _count;
            }
            public int? index;
            public int count;
        }

        public void Main()
        {
            Logger = new StringBuilder();

            //Get inventories
            var cargo_blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargo_blocks);

            var filtered_blocks = new List<IMyTerminalBlock>();
            for (int i = 0; i < cargo_blocks.Count; ++i)
            {
                if (cargo_blocks[i].CustomName.Contains(sourceContainerName))
                {
                    filtered_blocks.Add(cargo_blocks[i]);
                }
            }

            Echo("Cargo Blocks To Search: " + filtered_blocks.Count);

            //Figure out what we have and how much we need
            foreach(var key in CargoTargets.Keys)
            {
                Logger.AppendLine("Filling block '" + key + "'");
                var trgtBlock = GridTerminalSystem.GetBlockWithName(key);
                if(trgtBlock != null)
                {
                    StackAll(trgtBlock);
                    var requiredItems = CargoTargets[key];

                    var itemsToFill = GetTargetInvStatus(CargoTargets[key], trgtBlock);

                    var targetInventory = trgtBlock.GetInventory(0);

                    var tgtItems = new List<MyInventoryItem>();
                    targetInventory.GetItems(tgtItems);

                    //Loop through all items in the source blocks, and move them to target inv if needed
                    foreach (var block in filtered_blocks)
                    {
                        var srcInventory = block.GetInventory(0);

                        var srcItems = new List<MyInventoryItem>();
                        srcInventory.GetItems(srcItems);

                        //Go backwards so if we move an entire stack out it doesn't fuck with indices
                        for (int j = srcItems.Count - 1; j >= 0; j--)
                        {
                            var item = srcItems[j];
                            var name = decodeItemName(item.Type.SubtypeId.ToString(), item.Type.ToString());
                            if (itemsToFill.ContainsKey(name))
                            {
                                var requirements = itemsToFill[name];
                                if (requirements.CurrentCount < requirements.Required)
                                {
                                    // try to take
                                    var amountToMove = requirements.Required - requirements.CurrentCount;
                                    amountToMove = amountToMove <= item.Amount ? amountToMove : (int)item.Amount;

                                    Logger.AppendLine("Attempting to take " + amountToMove + " of " + name + " from " + block.CustomName);

                                    var result = srcInventory.TransferItemTo(requirements.TargetInventory, j, requirements.InventoryIndex, true, amountToMove);
                                    if (!result)
                                    {
                                        Logger.AppendLine("Transfer failed :(");
                                    }
                                    else
                                    {
                                        var newCountAndIndex = GetIndexAndCount(targetInventory, name);
                                        requirements.InventoryIndex = newCountAndIndex.index;
                                        requirements.CurrentCount = newCountAndIndex.count;
                                    }
                                }
                            }
                        }
                    }

                    //Log what we're still missing
                    foreach(var kvp in itemsToFill)
                    {
                        if(kvp.Value.CurrentCount < kvp.Value.Required)
                        {
                            Logger.AppendLine(kvp.Key + ": " + kvp.Value.CurrentCount + " / " + kvp.Value.Required);
                        }
                    }
                }
            }

            if (GridTerminalSystem.GetBlockWithName(LcdName) is IMyTextPanel lcd)
            {
                lcd.WriteText(Logger.ToString(), false);
            }
            Echo(Logger.ToString());
        }

        private IndexAndCount GetIndexAndCount(IMyInventory inventory, string tgtName)
        {
            var items = new List<MyInventoryItem>();
            inventory.GetItems(items);

            for(int i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                var name = decodeItemName(item.Type.SubtypeId.ToString(), item.Type.ToString());
                if(name == tgtName)
                {
                    return new IndexAndCount(i, (int)item.Amount);
                }
            }

            return new IndexAndCount(null, 0);
        }

        private Dictionary<string, ItemToBeFulfilled> GetTargetInvStatus(Dictionary<string, int> requiredItems, IMyTerminalBlock targetCargo)
        {
            var itemsToFill = new Dictionary<string, ItemToBeFulfilled>();

            var targetInventory = targetCargo.GetInventory(0);
            var tgtItems = new List<MyInventoryItem>();
            targetInventory.GetItems(tgtItems);

            foreach (var item in tgtItems)
            {
                var name = decodeItemName(item.Type.SubtypeId.ToString(), item.Type.ToString());

                if (requiredItems.ContainsKey(name))
                {
                    var requiredCount = requiredItems[name];
                    var currentCount = item.Amount;
                    var index = tgtItems.IndexOf(item);

                    if (!itemsToFill.ContainsKey(name))
                    {
                        itemsToFill.Add(name, new ItemToBeFulfilled
                        {
                            Required = requiredCount,
                            CurrentCount = (int)currentCount,
                            InventoryIndex = index,
                            TargetInventory = targetInventory
                        });
                    }
                    else
                    {
                        Echo("Item '" + name + "' defined multiple times for a single container. This currently is not supported");
                    }
                }
            }
            // Add entries for what we missed
            foreach (var itemName in requiredItems.Keys)
            {
                if (!itemsToFill.ContainsKey(itemName))
                {
                    itemsToFill.Add(itemName, new ItemToBeFulfilled
                    {
                        Required = requiredItems[itemName],
                        CurrentCount = 0,
                        InventoryIndex = null,
                        TargetInventory = targetInventory
                    });
                }
            }
            return itemsToFill;
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