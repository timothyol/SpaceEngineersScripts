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

namespace SpaceEngineersScripting.BoreWalker2
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



        private const float ExtendingSpeed = .8f;
        private const float RetractingSpeed = -1.0f;

        private const int DelayTimeSeconds = 15;

        private int _state = 1;
        private bool _stop = false;

        private TimeSpan _elapsed = new TimeSpan(0);
        private StringBuilder _log;

        // private DateTime? delayTime = null;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private void ToggleGear(IMyLandingGear gear, bool state)
        {
            if (state)
            {
                gear.Lock();
                gear.AutoLock = true;
            }
            else
            {
                gear.Unlock();
                gear.AutoLock = false;
            }
        }

        //Special thanks to "pipakin" for these two 
        bool IsReadyToLock(IMyTerminalBlock block)
        {
            var builder = new StringBuilder();
            block.GetActionWithName("SwitchLock").WriteValue(block, builder);

            return builder.ToString() == "Ready To Lock";
        }
        bool IsLocked(IMyTerminalBlock block)
        {
            var builder = new StringBuilder();
            block.GetActionWithName("SwitchLock").WriteValue(block, builder);

            return builder.ToString() == "Locked";
        }

        bool AreLocked(IEnumerable<IMyTerminalBlock> blocks)
        {
            foreach (var block in blocks)
            {
                if (!IsLocked(block))
                    return false;
            }
            return true;
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
            Matrix mat;
            startBlk.Orientation.GetMatrix(out mat);
            Vector3I right1 = new Vector3I(mat.Right);
            Vector3I left1 = new Vector3I(mat.Left);
            Vector3I forward1 = new Vector3I(mat.Forward);
            Vector3I backward1 = new Vector3I(mat.Backward);
            Vector3I up1 = new Vector3I(mat.Up);
            Vector3I down1 = new Vector3I(mat.Down);

            // Rail section
            // Down is right, right is backward 
            // |startblk|
            // | merge  |connectr| armor   | armor    | merge    |
            // | tube   |junction| tube    | tube     | tube     | 
            // Type doesn't matter

            for (int i = 1; i < 3; ++i)
            {
                for(int j = 0; j < 5; ++ j)
                {
                    if(!CheckForBlock(startBlk, (right1 * i) + (backward1 * j), new string('r', i) + new string('b', j)))
                    {
                        _log.AppendLine($"Block at [{i},{j}] is not complete.");
                        return false;
                    }
                }
            }

            return true;
        }

        bool IsSecondConnectorComplete(IMyShipMergeBlock startBlk)
        {
            Matrix mat;
            startBlk.Orientation.GetMatrix(out mat);
            Vector3I right1 = new Vector3I(mat.Right);
            Vector3I left1 = new Vector3I(mat.Left);
            Vector3I forward1 = new Vector3I(mat.Forward);
            Vector3I backward1 = new Vector3I(mat.Backward);
            Vector3I up1 = new Vector3I(mat.Up);
            Vector3I down1 = new Vector3I(mat.Down);

            // Rail section
            // Down is right, right is backward (e.g., the junction block is "
            // |startblk|
            // | merge  |connectr| armor   | armor    | merge    |
            // | tube   |junction| tube    | tube     | tube     | 

            return CheckForBlock(startBlk, (right1 * 1) + (backward1 * 1), new string('r', 1) + new string('b', 1));
        }

        bool CheckForBlock(IMyShipMergeBlock startBlk, Vector3I direction, string description)
        {
            IMySlimBlock sb = startBlk.CubeGrid.GetCubeBlock(startBlk.Position + direction);
            if (sb == null)
            {
                if(startBlk.CubeGrid.CubeExists(startBlk.Position + direction))
                {
                    // If it's an armor block then sb will be null, but CubeExists() should return true
                    // no way to know how welded it is, but it doesn't really matter here
                    _log.AppendLine("Armor block? (" + description + ")");
                    return true;
                }
                _log.AppendLine("No block (" + description + ")");
            }
            else
            {
                var type = sb.FatBlock.GetType().Name;
                _log.AppendLine("Block found (" + description + "): " + type);


                var block = sb.FatBlock;
                if (block != null)
                {
                    _log.AppendLine("BuildIntegrity: " + sb.BuildIntegrity);
                    _log.AppendLine("CurrentDamage: " + sb.CurrentDamage);
                    _log.AppendLine("MaxIntegrity: " + sb.MaxIntegrity);

                    _log.AppendLine(block.IsFunctional ? "Is Functional" : "Not Functional");
                    return block.IsFunctional;
                }
            }
            return false;
        }

        void Main(string argument, UpdateType updateSource)
        {
            //States:
            // Extending (Gear locked, piston extending, merge block unmerged but enabled if piston > 2.5m)
            // Retracting (Gear unlocked, piston retracting, merge block locked)

            _log = new StringBuilder("");
            _log.AppendLine("State is: " + _state.ToString());

            var piston = GridTerminalSystem.GetBlockWithName("Piston Borewalker2") as IMyPistonBase;
            var panel = GridTerminalSystem.GetBlockWithName("LCD Status Borewalker2") as IMyTextPanel;
            var gear = GridTerminalSystem.GetBlockWithName("Landing Gear Borewalker2") as IMyLandingGear;
            var merge = GridTerminalSystem.GetBlockWithName("Merge Block Borewalker2") as IMyShipMergeBlock;
            var connector = GridTerminalSystem.GetBlockWithName("Connector Borewalker2") as IMyShipConnector;
            var connector2 = GridTerminalSystem.GetBlockWithName("Connector Borewalker2b") as IMyShipConnector;

            if (connector == null)
            {
                _log.AppendLine("Cannot find connector. :(");
                if (panel != null)
                {
                    panel.WriteText(_log.ToString(), false);
                }
                return;
            }
            if (connector2 == null)
            {
                _log.AppendLine("Cannot find connector. :(");
                if (panel != null)
                {
                    panel.WriteText(_log.ToString(), false);
                }
                return;
            }

            if (argument == "start")
            {
                _stop = false;
            }
            else if (argument == "stop")
            {
                _stop = true;
            }

            if (_stop)
            {
                ToggleGear(gear, true);
                piston.Velocity = 0f;
                return;
            }

            if (_state == 1)
            {
                if(piston.CurrentPosition == 10.0f && !IsMerged(merge))
                {
                    // Sometimes it doesn't merge, and moving back and forth will trigger it
                    piston.Velocity = RetractingSpeed;
                }
                else if(IsMerged(merge) && IsSecondConnectorComplete(merge))
                {
                    // Block merged, transition to state 2 once rail is complete
                    //if (delayTime == null)
                    //{
                    //    delayTime = DateTime.Now.AddSeconds(DelayTimeSeconds);
                    //    connector2.Enabled = true;
                    //    connector2.Connect();
                    //}
                    //else if (delayTime > DateTime.Now)
                    //{
                    //    var remaining = delayTime - DateTime.Now;
                    //    _log.AppendLine("Waiting. Time Left:" + remaining.Value.TotalSeconds + "s.");

                    //    connector2.Enabled = true;
                    //    connector2.Connect();
                    //}
                    //else if (delayTime < DateTime.Now)
                    //{
                    //delayTime = null;

                    _state = 2;
                    ToggleGear(gear, false);
                    connector.Enabled = false;
                    connector2.Enabled = true;
                    connector2.Connect();
                    piston.Velocity = RetractingSpeed;
                }
                else
                {
                    // Keep on extending piston
                    piston.Velocity = ExtendingSpeed;
                    ToggleGear(gear, true);
                    connector.Enabled = true;
                    connector.Connect();
                    connector2.Enabled = false;
                    if (IsLocked(gear))
                    {
                        if (piston.CurrentPosition > 2.25f)
                        {
                            merge.Enabled = true;
                        }
                        else
                        {
                            merge.Enabled = false;
                        }
                    }
                }
            }
            else if (_state == 2)
            {
                if (piston.CurrentPosition == 0f)
                {
                    if (IsSectionComplete(merge))
                    {
                        _state = 1;
                        ToggleGear(gear, true);
                        connector2.Enabled = false;
                        connector.Enabled = true;
                        connector.Connect();
                        piston.Velocity = ExtendingSpeed;
                        if (IsLocked(gear))
                        {
                            merge.Enabled = false;
                        }
                    }
                    else
                    {
                        connector.Enabled = true;
                        connector.Connect();
                        _log.AppendLine("Waiting for section to complete");
                    }
                    // Time to transition to state 1
                    //var currentTime = DateTime.Now;
                    //if(delayTime != null)
                    //{
                    //    connector2.Enabled = true;
                    //    connector2.Connect();
                    //    if (currentTime > delayTime)
                    //    {
                    //        delayTime = null;
                    //    }
                    //}
                    //else
                    //{
                    //}
                }
                else
                {
                    // Keep on retracting piston
                    merge.Enabled = true;
                    piston.Velocity = RetractingSpeed;
                    if (IsMerged(merge))
                    {
                        ToggleGear(gear, false);
                        connector.Enabled = false;
                        connector2.Enabled = true;
                        connector2.Connect();
                    }
                }
            }

            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
            }
        }


        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}