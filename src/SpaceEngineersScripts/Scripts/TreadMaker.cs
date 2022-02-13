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

namespace SpaceEngineersScripting.TreadMaker
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

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }


        private StringBuilder _log;
        private int _state = -1;


        public void Main(string argument, UpdateType updateSource)
        {
            //States:
            // 0: Piston Grab fully extended, link is made and connected to old tread
            // 1: Connect Mag plate
            // 2: Turn off merge block
            // 3: retract piston grab all the way
            // 4: Turn ON merge block, New link is made
            // 5: Detach hinge parts
            // 6: extend piston grab 1m
            // 7: attach hingest to old link
            // 8: unlock mag plate
            // 9: extend piston grab 2m
            // 10: goto 0
            // 


            // Retracting (Merge Unmerged, Back connector unlocked, front locked, piston retracting)

            if (argument == "start")
            {
                _state = 0;
            }
            else if (argument == "stop")
            {
                _state = -1;
            }

            _log = new StringBuilder("");
            _log.AppendLine("State is: " + _state.ToString());

            var pistonGrab = GridTerminalSystem.GetBlockWithName("Piston Grab") as IMyPistonBase;
            var merge = GridTerminalSystem.GetBlockWithName("Small Merge Block TreadMaker") as IMyShipMergeBlock;
            var magPlate = GridTerminalSystem.GetBlockWithName("Magnetic Plate Grab") as IMyLandingGear;
            var newHinge1 = GridTerminalSystem.GetBlockWithName("Hinge 1") as IMyMotorStator;
            var newHinge2 = GridTerminalSystem.GetBlockWithName("Hinge 2") as IMyMotorStator;
            var panel = GridTerminalSystem.GetBlockWithName("LCD Panel TreadMaker Status") as IMyTextPanel;

            if (newHinge1 == null || newHinge2 == null)
                _log.AppendLine("At least one new hinge not found.");

            switch (_state)
            {
                case -1:
                    // Waiting to start.
                    _log.AppendLine("Waiting for start command.");
                    break;
                case 0:
                    // We initializing boys
                    if( // next state when:
                        IsMerged(merge)
                        && magPlate.IsLocked
                        && pistonGrab.CurrentPosition == 2.0f)
                    {
                        _state = 1;
                    } else
                    {
                        var isMerged = IsMerged(merge);
                        _log.AppendLine("IsMerged: " + isMerged + ", isLocked: " + magPlate.IsLocked + ", CurrentPos: " + pistonGrab.CurrentPosition);

                        // keep trying to get there
                        magPlate.AutoLock = false;
                        pistonGrab.Velocity = 1;
                        pistonGrab.MaxLimit = 2;
                        merge.Enabled = true;
                        if(pistonGrab.CurrentPosition == 2.0f)
                        {
                            magPlate.Lock();
                        }
                        else
                        {
                            magPlate.Unlock();
                        }
                    }
                    break;
                case 1:
                    // Turn off merge block, retract piston, if currentPos < 1, turn on merge block
                    if ( // next state when:
                        IsMerged(merge)
                        && pistonGrab.CurrentPosition == 0.0f)
                    {
                        _state = 2;
                    }
                    else
                    {
                        pistonGrab.Velocity = -1;
                        if (pistonGrab.CurrentPosition < 1.0f)
                        {
                            merge.Enabled = true;
                        }
                        else
                        {
                            merge.Enabled = false;
                        }
                    }
                    break;
                case 2:
                    // detach new hinges. extend piston 1m. 
                    if(newHinge1 == null)
                    {
                        _log.AppendLine("newHinge1 not found.");
                    }
                    if(newHinge2 == null)
                    {
                        _log.AppendLine("newHinge2 not found.");
                    }
                    if(newHinge1 == null || newHinge2 == null)
                    {
                        break;
                    }


                    if (IsMerged(merge)
                        && pistonGrab.CurrentPosition == 1.0f
                        && newHinge1 != null && !newHinge1.IsAttached
                        && newHinge2 != null && !newHinge2.IsAttached)
                    {
                        _state = 3;
                    }
                    else
                    {
                        newHinge1.Detach();
                        newHinge2.Detach();

                        pistonGrab.MaxLimit = 1.0f;
                        pistonGrab.Velocity = 1;
                    }
                    break;
                case 3:
                    // Attach hinges. 
                    if (newHinge1 != null && newHinge1.IsAttached && newHinge2 != null && newHinge2.IsAttached)
                    {
                        _state = 4;
                        newHinge1.CustomName = "Hinge Link 1";
                        newHinge2.CustomName = "Hinge Link 2";
                    }                        
                    else
                    {
                        newHinge1.Attach();
                        newHinge2.Attach();
                    }
                    break;
                case 4:
                    // Unlock mag plate and extend piston 2m.
                    if (pistonGrab.CurrentPosition == 2.0f
                        && !magPlate.IsLocked)
                    {
                        _state = 0;
                    }
                    else
                    {
                        magPlate.Unlock();
                        pistonGrab.MaxLimit = 2.0f;
                        pistonGrab.Velocity = 1;
                    }
                    break;
                default:
                    _log.AppendLine("Invalid state. :(");
                    break;
            }

            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
                Echo(_log.ToString());
            }


            //var piston = GridTerminalSystem.GetBlockWithName("Piston Railwalker") as IMyPistonBase;
            //var panel = GridTerminalSystem.GetBlockWithName("LCD Panel Railwalker Status") as IMyTextPanel;
            //var merge = GridTerminalSystem.GetBlockWithName("Merge Block Railwalker") as IMyShipMergeBlock;
            //var connectorBack = GridTerminalSystem.GetBlockWithName("Connector Railwalker Back") as IMyShipConnector;
            //var connectorFront = GridTerminalSystem.GetBlockWithName("Connector Railwalker Front") as IMyShipConnector;



        }

        bool IsMerged(IMyShipMergeBlock mrg1)
        {
            //Find direction that block merges to
            Matrix mat;
            mrg1.Orientation.GetMatrix(out mat);
            Vector3I up1 = new Vector3I(mat.Up);

            //Check if there is a block in front of merge face
            IMySlimBlock sb = mrg1.CubeGrid.GetCubeBlock(mrg1.Position + up1);
            if (sb == null)
            {
                _log.AppendLine("IsMerged: block is null");
                return false;
            }

            //Check if the other block is actually a merge block
            IMyShipMergeBlock mrg2 = sb.FatBlock as IMyShipMergeBlock;
            if (mrg2 == null)
            {
                _log.AppendLine("IsMerged: block is not merge block");
                return false;
            }

            //Check that other block is correctly oriented
            mrg2.Orientation.GetMatrix(out mat);
            Vector3I up2 = new Vector3I(mat.Up);

            var result = up2 == -up1;
            _log.AppendLine("Block is " + (result ? "" : "NOT ") + "merged.");

            return result;
        }




        
        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}