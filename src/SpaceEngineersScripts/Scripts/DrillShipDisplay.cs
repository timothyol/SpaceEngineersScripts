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

namespace SpaceEngineersScripting.DrillShipDisplay
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


        //Drill Ship Display

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private const string LcdName = "LCD Panel OreDisplay";
        private const string CockpitName = "Cockpit";
        private const int SurfaceId = 2;

        private List<IMyTerminalBlock> cargo_blocks = null;

        public void Main(string argument, UpdateType updateSource)
        {
            var sbLog = new StringBuilder();

            var conn_blocks = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(conn_blocks);

            bool isConnected = false;

            for (int i = 0; i < conn_blocks.Count; ++i)
            {
                var conn = conn_blocks[i];
                if (conn.Status == MyShipConnectorStatus.Connected)
                {
                    isConnected = true;
                    break;
                }
            }

            if (cargo_blocks == null && isConnected)
            {
                var undockmsg = "Undock to update display";
                Echo(undockmsg);
                PrintLcd(undockmsg);
                return;
            }

            if (cargo_blocks == null)
            {
                //Get inventories; cargo blocks, connectors, drills    
                cargo_blocks = new List<IMyTerminalBlock>();
                AddBlocks<IMyCargoContainer>(cargo_blocks);
                AddBlocks<IMyShipDrill>(cargo_blocks);
                AddBlocks<IMyShipConnector>(cargo_blocks);

                // O2 gens not included in "Total Space Available" calculation
                AddBlocks<IMyGasGenerator>(cargo_blocks);
            }

            // declare variables for calculating volume

            VRage.MyFixedPoint currentVolume = 0;
            VRage.MyFixedPoint maxVolume = 0;

            var itemCounts = new Dictionary<string, int>();
            for (int i = 0; i < cargo_blocks.Count; ++i)
            {
                var cb = cargo_blocks[i];
                var inv = cb.GetInventory(0);
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inv.GetItems(items);

                //Don't include O2 gen for space calculations
                if (!(cb is IMyGasGenerator))
                {
                    currentVolume += inv.CurrentVolume;
                    maxVolume += inv.MaxVolume;
                }

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

            double percentage = (double)100 - Math.Round(((double)currentVolume / (double)maxVolume) * 100, 2);

            // rounds the volume values for clean display
            double dbcurrentVolume = Math.Round((double)currentVolume, 2);
            double dbmaxVolume = Math.Round((double)maxVolume, 2);


            var msg = "Space Available: (" + percentage + "%)\r\n";

            var invDispWidth = 26;
            var iCnt = Math.Floor(percentage * ((double)invDispWidth / 100.0));

            for (int i = 0; i < (invDispWidth - iCnt); ++i)
                msg += "X";
            for (int i = 0; i < iCnt; ++i)
                msg += "-";
            msg += "\r\n";


            msg += "\r\nOres:\r\n";
            for (int i = 0; i < itemCounts.Count; ++i)
            {
                var key = itemCounts.Keys.ElementAt(i);
                var count = itemCounts[key];

                if (key.EndsWith("Ore") || key.EndsWith("Stone") || key.EndsWith("Ice"))
                {
                    msg += "\r\n" + key + ": " + count;
                }
                else
                {
                    Echo("Ignored item: " + key + ": " + count);
                }
            }

            Echo(msg);
            PrintLcd(msg);

        }

        private void AddBlocks<T>(List<IMyTerminalBlock> blockList) where T : class
        {
            var temp = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(temp);
            blockList.AddRange(temp);
        }

        private void PrintLcd(String msg)
        {
            var lcdBlks = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(lcdBlks);
            for (int i = 0; i < lcdBlks.Count; ++i)
            {
                var lcd = lcdBlks[i];
                if (lcd.CustomName == LcdName)
                    lcd.WriteText(msg, false);
            }

            var cockpit = GridTerminalSystem.GetBlockWithName(CockpitName) as IMyTextSurfaceProvider;
            if(cockpit != null)
            {
                var surface = cockpit.GetSurface(SurfaceId);
                surface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                surface.WriteText(msg, false);
            }
        }

        //Stolen from TheFinalFrontier.se
        //http://thefinalfrontier.se/inventory-display-and-management/
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
            if (name.Equals("AngleGrinderItem")) { return "Grinder"; }
            if (name.Equals("HandDrillItem")) { return "Hand Drill"; }
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


        /***************To above this comment*******************************
        ********************************************************************/
    }
}