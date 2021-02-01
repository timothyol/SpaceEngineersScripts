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

        private DateTime? delayTime = null;

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



        void Main(string argument, UpdateType updateSource)
        {
            //States:
            // Extending (Gear locked, piston extending, merge block enabled if piston > 2.5m)
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
                    piston.Velocity = RetractingSpeed;
                }
                else if(IsMerged(merge))
                {
                    if (delayTime == null)
                    {
                        delayTime = DateTime.Now.AddSeconds(DelayTimeSeconds);
                        connector2.Enabled = true;
                        connector2.Connect();
                    }
                    else if (delayTime > DateTime.Now)
                    {
                        var remaining = delayTime - DateTime.Now;
                        _log.AppendLine("Waiting. Time Left:" + remaining.Value.TotalSeconds + "s.");

                        connector2.Enabled = true;
                        connector2.Connect();
                    }
                    else if (delayTime < DateTime.Now)
                    {
                        delayTime = null;
                        _state = 2;
                        ToggleGear(gear, false);
                        connector.Enabled = false;
                        connector2.Enabled = true;
                        connector2.Connect();
                        piston.Velocity = RetractingSpeed;
                    }
                }
                else
                {
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
                    var currentTime = DateTime.Now;
                    if(delayTime != null)
                    {
                        connector2.Enabled = true;
                        connector2.Connect();
                        if (currentTime > delayTime)
                        {
                            delayTime = null;
                        }
                    }
                    else
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
                }
                else
                {
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
}s