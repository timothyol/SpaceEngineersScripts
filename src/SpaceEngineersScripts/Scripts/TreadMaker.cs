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
        private int _lState = -1;
        private int _lNumMade = 0;
        private int _rState = -1;
        private int _rNumMade = 0;
        private bool _paused = false;

        private const int MaxLinks = 30;

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "start")
            {
                _lState = 0;
                _lNumMade = 0;
                _rState = 0;
                _rNumMade = 0;
            }
            else if (argument == "stop")
            {
                _lState = -1;
                _rState = -1;
            }
            else if (argument == "pause")
            {
                _paused = !_paused;
            }

            _log = new StringBuilder("");
            _log.AppendLine("LState is: " + _lState.ToString());
            _log.AppendLine("RState is: " + _rState.ToString());

            if (_paused)
            {
                _log.AppendLine("Paused");
                WriteLogs();
                return;
            }

            if (_lNumMade < MaxLinks)
            {
                var oldState = _lState;
                _lState = MakeTread(_lState, "TML", _lNumMade);
                if (oldState != 0 && _lState == 0)
                    _lNumMade++;
                _log.AppendLine("L has made " + _lNumMade + " links");
            }
            else
            {
                _log.AppendLine("L has made enough links!");
            }

            if (_rNumMade < MaxLinks)
            {
                var oldState = _rState;
                _rState = MakeTread(_rState, "TMR", _rNumMade);
                if (oldState != 0 && _rState == 0)
                    _rNumMade++;
                _log.AppendLine("R has made " + _rNumMade + " links");
            }
            else
            {
                _log.AppendLine("R has made enough links!");
            }

            WriteLogs();
        }
        
        int MakeTread(int state, string prefix, int numMade)
        {
            //Sequence:
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

            var newState = state;

            var lastHingeName = prefix + " " + "Hinge Link " + (numMade < 10 ? "0" : "") + numMade;

            var pistonGrab1 = GridTerminalSystem.GetBlockWithName(prefix + " Piston Grab 1") as IMyPistonBase;
            var pistonGrab2 = GridTerminalSystem.GetBlockWithName(prefix + " Piston Grab 2") as IMyPistonBase;
            var magPlate1 = GridTerminalSystem.GetBlockWithName(prefix + " Magnetic Plate Grab 1") as IMyLandingGear;
            var magPlate2 = GridTerminalSystem.GetBlockWithName(prefix + " Magnetic Plate Grab 2") as IMyLandingGear;
            var merge = GridTerminalSystem.GetBlockWithName(prefix + " Small Merge Block TreadMaker") as IMyShipMergeBlock;
            var newHinge1 = GridTerminalSystem.GetBlockWithName(prefix + " Link Hinge 1") as IMyMotorStator;
            var newHinge2 = GridTerminalSystem.GetBlockWithName(prefix + " Link Hinge 2") as IMyMotorStator;
            var lastHinge1 = GridTerminalSystem.GetBlockWithName(lastHingeName + "A") as IMyMotorStator;
            var lastHinge2 = GridTerminalSystem.GetBlockWithName(lastHingeName + "B") as IMyMotorStator;

            if (newHinge1 == null || newHinge2 == null)
                _log.AppendLine("At least one new hinge not found.");

            if (pistonGrab1 == null) { _log.AppendLine(prefix + " Can't find pistonGrab1"); return -1; }
            if (pistonGrab2 == null) { _log.AppendLine(prefix + " Can't find pistonGrab2"); return -1; }
            if (magPlate1 == null) { _log.AppendLine(prefix + " Can't find magPlate1"); return -1; }
            if (magPlate2 == null) { _log.AppendLine(prefix + " Can't find magPlate2"); return -1; }
            if (merge == null) { _log.AppendLine(prefix + " Can't find merge"); return -1; }

            switch (state)
            {
                case -1:
                    // Waiting to start.
                    _log.AppendLine(prefix + " Waiting for start command.");
                    break;
                case 0:

                    // We initializing boys
                    if ( // next state when:
                        IsMerged(merge)
                        && magPlate1.IsLocked
                        && magPlate2.IsLocked
                        && pistonGrab1.CurrentPosition == 2.0f
                        && pistonGrab2.CurrentPosition == 2.0f
                        && (numMade == 0
                            ? (lastHinge1 != null && !lastHinge1.IsAttached
                               && lastHinge2 != null && !lastHinge2.IsAttached)
                            : true)
                        )
                    {
                        newState = 1;
                    }
                    else
                    {
                        var isMerged = IsMerged(merge);
                        _log.AppendLine(prefix + " " + "IsMerged: " + isMerged + ", isLocked: " + magPlate1.IsLocked + ", CurrentPos: " + pistonGrab1.CurrentPosition);
                        _log.AppendLine(prefix + " " + "IsMerged: " + isMerged + ", isLocked: " + magPlate1.IsLocked + ", CurrentPos: " + pistonGrab1.CurrentPosition);

                        // keep trying to get there
                        magPlate1.AutoLock = false;
                        magPlate2.AutoLock = false;
                        pistonGrab1.Velocity = 1;
                        pistonGrab2.Velocity = 1;
                        pistonGrab1.MaxLimit = 2;
                        pistonGrab2.MaxLimit = 2;
                        merge.Enabled = true;
                        if (newHinge1 != null) newHinge1.CustomName = lastHingeName + "A";
                        if (newHinge2 != null) newHinge2.CustomName = lastHingeName + "B";
                        if (pistonGrab1.CurrentPosition == 2.0f && pistonGrab2.CurrentPosition == 2.0f)
                        {
                            magPlate1.Lock();
                            magPlate2.Lock();
                        }
                        else
                        {
                            magPlate1.Unlock();
                            magPlate2.Unlock();
                        }
                        if (numMade == 0)
                        {
                            // start fresh. detach any existing trash connected to our first link
                            if (lastHinge1 != null) lastHinge1.Detach();
                            if (lastHinge2 != null) lastHinge2.Detach();
                        }
                    }
                    break;
                case 1:
                    // Turn off merge block, retract piston, if currentPos < 1, turn on merge block
                    if ( // next state when:
                        IsMerged(merge)
                        && pistonGrab1.CurrentPosition <= 0.5f
                        && pistonGrab2.CurrentPosition == 0.5f)
                    {
                        newState = 2;
                    }
                    else
                    {
                        pistonGrab1.Velocity = -1;
                        pistonGrab2.Velocity = -1;
                        if (pistonGrab1.CurrentPosition < 1.0f && pistonGrab2.CurrentPosition < 1.0f)
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
                    if (newHinge1 == null)
                    {
                        _log.AppendLine(prefix + " " + "newHinge1 not found.");
                    }
                    if (newHinge2 == null)
                    {
                        _log.AppendLine(prefix + " " + "newHinge2 not found.");
                    }
                    if (newHinge1 == null || newHinge2 == null)
                    {
                        break;
                    }

                    if (IsMerged(merge)
                        && pistonGrab1.CurrentPosition == 1.0f
                        && pistonGrab2.CurrentPosition == 1.0f
                        && newHinge1 != null && !newHinge1.IsAttached
                        && newHinge2 != null && !newHinge2.IsAttached)
                    {
                        newState = 3;
                    }
                    else
                    {
                        newHinge1.Detach();
                        newHinge2.Detach();

                        pistonGrab1.MaxLimit = 1.0f;
                        pistonGrab2.MaxLimit = 1.0f;
                        pistonGrab1.Velocity = 1;
                        pistonGrab2.Velocity = 1;
                    }
                    break;
                case 3:
                    // Attach hinges. 
                    if (newHinge1 != null && newHinge1.IsAttached && newHinge2 != null && newHinge2.IsAttached)
                    {
                        newState = 4;
                    }
                    else
                    {
                        newHinge1.Attach();
                        newHinge2.Attach();
                    }
                    break;
                case 4:
                    // Unlock mag plate and extend piston 2m.
                    if (pistonGrab1.CurrentPosition == 2.0f
                        && pistonGrab2.CurrentPosition == 2.0f
                        && !magPlate1.IsLocked
                        && !magPlate2.IsLocked)
                    {
                        newState = 0;
                    }
                    else
                    {
                        magPlate1.Unlock();
                        magPlate2.Unlock();
                        pistonGrab1.MaxLimit = 2.0f;
                        pistonGrab2.MaxLimit = 2.0f;
                        pistonGrab1.Velocity = 1;
                        pistonGrab2.Velocity = 1;
                    }
                    break;
                default:
                    _log.AppendLine(prefix + " " + "Invalid state. :(");
                    break;
            }

            return newState;
        }

        void WriteLogs()
        {
            var panel = GridTerminalSystem.GetBlockWithName("LCD Panel TreadMaker Status") as IMyTextPanel;
            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
                Echo(_log.ToString());
            }
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
                //_log.AppendLine("IsMerged: block is null");
                return false;
            }

            //Check if the other block is actually a merge block
            IMyShipMergeBlock mrg2 = sb.FatBlock as IMyShipMergeBlock;
            if (mrg2 == null)
            {
                //_log.AppendLine("IsMerged: block is not merge block");
                return false;
            }

            //Check that other block is correctly oriented
            mrg2.Orientation.GetMatrix(out mat);
            Vector3I up2 = new Vector3I(mat.Up);

            var result = up2 == -up1;
            //_log.AppendLine("Block is " + (result ? "" : "NOT ") + "merged.");

            return result;
        }

        
        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}