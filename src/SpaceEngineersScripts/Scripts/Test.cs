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

namespace SpaceEngineersScripting.Test
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

        private StringBuilder _log;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        bool IsMerged(IMyShipMergeBlock mrg1)
        {
            //Find direction that block merges to
            Matrix mat;
            mrg1.Orientation.GetMatrix(out mat);
            Vector3I right1 = new Vector3I(mat.Right);

            //Check if there is a block in front of merge face
            IMySlimBlock sb = mrg1.CubeGrid.GetCubeBlock(mrg1.Position + right1);
            if (sb == null) return false;

            //Check if the other block is actually a merge block
            IMyShipMergeBlock mrg2 = sb.FatBlock as IMyShipMergeBlock;
            if (mrg2 == null) return false;

            //Check that other block is correctly oriented
            mrg2.Orientation.GetMatrix(out mat);
            Vector3I right2 = new Vector3I(mat.Right);

            var result = right2 == -right1;
            _log.AppendLine("Block is " + (result ? "" : "NOT ") + "merged.");

            return result;
        }

        bool IsSectionComplete(IMyShipMergeBlock startBlk)
        {
            //Find direction that block merges to
            Matrix mat;
            startBlk.Orientation.GetMatrix(out mat);
            Vector3I right1 = new Vector3I(mat.Right);
            Vector3I left1 = new Vector3I(mat.Left);
            Vector3I forward1 = new Vector3I(mat.Forward);
            Vector3I backward1 = new Vector3I(mat.Backward);
            Vector3I up1 = new Vector3I(mat.Up);
            Vector3I down1 = new Vector3I(mat.Down);

            CheckForBlock(startBlk, right1, "Right");
            CheckForBlock(startBlk, left1, "Left");
            CheckForBlock(startBlk, forward1, "Forward");
            CheckForBlock(startBlk, backward1, "Backward");
            CheckForBlock(startBlk, up1, "Up");
            CheckForBlock(startBlk, down1, "Down");

            CheckForBlock(startBlk, right1 + up1, "ru");

            return false;
        }

        void CheckForBlock(IMyShipMergeBlock startBlk, Vector3I direction, string description)
        {
            IMySlimBlock sb = startBlk.CubeGrid.GetCubeBlock(startBlk.Position + direction);
            if (sb == null)
            {
                // _log.AppendLine("No block to the " + description + ".");
            }
            else
            {
                var type = sb.FatBlock.GetType().Name;
                _log.AppendLine("Block found (" + description + "): " + type);

                var block = sb.FatBlock;
                if(block != null)
                {
                    _log.AppendLine(block.IsFunctional ? "Is Functional" : "Not Functional");
                }

            }
        }

        void Main(string argument, UpdateType updateSource)
        {
            //States:
            // Extending (Gear locked, piston extending, merge block enabled if piston > 2.5m)
            // Retracting (Gear unlocked, piston retracting, merge block locked)

            _log = new StringBuilder("");

            //var piston = GridTerminalSystem.GetBlockWithName("Piston Borewalker2") as IMyPistonBase;
            var panel = GridTerminalSystem.GetBlockWithName("LCD Status Borewalker2") as IMyTextPanel;
            //var gear = GridTerminalSystem.GetBlockWithName("Landing Gear Borewalker2") as IMyLandingGear;
            var merge = GridTerminalSystem.GetBlockWithName("Merge Block Borewalker2") as IMyShipMergeBlock;
            //var connector = GridTerminalSystem.GetBlockWithName("Connector Borewalker2") as IMyShipConnector;
            //var connector2 = GridTerminalSystem.GetBlockWithName("Connector Borewalker2b") as IMyShipConnector;

            IsSectionComplete(merge);


            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
            }

        }


    /***************To above this comment into space engineers**********
    ********************************************************************/
    }
}