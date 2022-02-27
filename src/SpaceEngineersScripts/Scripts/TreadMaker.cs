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

        private int _lWrapState = -1;
        private int _rWrapState = -1;

        private const int MaxLinks = 28;

        private Dictionary<string, IMyMotorStator> _hinges = null;
        private Dictionary<string, int> _limpState = new Dictionary<string, int>();

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "start")
            {
                _lState = 0;
                _lNumMade = 0;
                _rState = 0;
                _rNumMade = 0;
                _lWrapState = 0;
                _rWrapState = 0;
                _hinges = null;
                _limpState["TMR"] = 0;
                _limpState["TML"] = 0;
            }
            else if (argument == "stop")
            {
                _lState = -1;
                _rState = -1;
                _lNumMade = 0;
                _rNumMade = 0;
                _lWrapState = -1;
                _rWrapState = -1;
                _hinges = null;
                // reset:
                ResetTM("TMR");
                ResetTM("TML");
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

            if(_rNumMade >= MaxLinks && _lNumMade >= MaxLinks)
            {
                // Reset the treadmaker machinery
                //ResetTM("TMR");
                //ResetTM("TML");

                // Get hinges.
                if (_hinges == null)
                {
                    GetHinges();
                }

                // start wrapping.
                try
                {
                    _log.AppendLine("Wrapping R");
                    _rWrapState = WrapTread("TMR", _rWrapState);
                    _log.AppendLine("Wrapping L");
                    _lWrapState = WrapTread("TML", _lWrapState);
                }
                catch(Exception e)
                {
                    _log.AppendLine("Exception wrapping: " + e.Message);
                    WriteLogs();
                    return;
                }
            }

            _log.AppendLine("Finished cleanly.");
            WriteLogs();
        }

        void GetHinges()
        {
            _hinges = new Dictionary<string, IMyMotorStator>();

            var allHinges = new List<IMyMotorStator>();

            GridTerminalSystem.GetBlocksOfType(allHinges);
            for (int i = 0; i < allHinges.Count; ++i)
            {
                var hinge = allHinges[i];
                if (hinge.CustomName.StartsWith("TMR") || hinge.CustomName.StartsWith("TML"))
                {
                    _hinges.Add(hinge.CustomName, hinge);
                }
            }
        }

        void ResetTM(string prefix)
        {
            var pistonGrab1 = GridTerminalSystem.GetBlockWithName(prefix + " Piston Grab 1") as IMyPistonBase;
            var pistonGrab2 = GridTerminalSystem.GetBlockWithName(prefix + " Piston Grab 2") as IMyPistonBase;
            var magPlate1 = GridTerminalSystem.GetBlockWithName(prefix + " Magnetic Plate Grab 1") as IMyLandingGear;
            var magPlate2 = GridTerminalSystem.GetBlockWithName(prefix + " Magnetic Plate Grab 2") as IMyLandingGear;
            var merge = GridTerminalSystem.GetBlockWithName(prefix + " Small Merge Block TreadMaker") as IMyShipMergeBlock;

            if (merge != null) merge.Enabled = false;
            if (magPlate1 != null) magPlate1.Unlock();
            if (magPlate2 != null) magPlate2.Unlock();
            if (pistonGrab1 != null) pistonGrab1.Velocity = -1.0f;
            if (pistonGrab2 != null) pistonGrab2.Velocity = -1.0f;
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
                        && (magPlate1.IsLocked || magPlate2.IsLocked)
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
                        _log.AppendLine(prefix + " " + "IsMerged: " + isMerged + ", isLocked: " + magPlate2.IsLocked + ", CurrentPos: " + pistonGrab2.CurrentPosition);

                        // keep trying to get there
                        magPlate1.AutoLock = false;
                        magPlate2.AutoLock = false;
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
                        if(!isMerged)
                        {
                            // Pistons can get in the way of blocks being placed, retract them until the merge block gets made
                            pistonGrab1.Velocity = -1;
                            pistonGrab2.Velocity = -1;
                        }
                        else
                        {
                            pistonGrab1.Velocity = 1;
                            pistonGrab2.Velocity = 1;
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

        int WrapTread(string prefix, int state)
        {
            // Detach 0, 27
            // Old: (subract 9)
            // 15 17 21 26 30 32
            // 15, 32: 90 degrees
            // 17, 30: 65 degrees
            // 21 26: 25 degrees
            // torque: 3000000

            //var hinges6 = GetHinges(6);
            //var hinges8 = GetHinges(8);
            //var hinges12 = GetHinges(12);
            //var hinges17 = GetHinges(17);
            //var hinges21 = GetHinges(21);
            //var hinges23 = GetHinges(23);

            //DetachHinges(10);
            //DetachHinges(28);


            var mergeTens1 = GridTerminalSystem.GetBlockWithName(prefix + " Merge Tensioner 1") as IMyShipMergeBlock;
            var mergeTens2 = GridTerminalSystem.GetBlockWithName(prefix + " Merge Tensioner 2") as IMyShipMergeBlock;
            var mergeRoot = GridTerminalSystem.GetBlockWithName(prefix + " Small Merge Block TreadMaker") as IMyShipMergeBlock;
            var pistTens = GridTerminalSystem.GetBlockWithName(prefix + " Piston Tensioner 2") as IMyPistonBase;
            var firstLinks = GetHinges(0, prefix);

            if (mergeTens1 == null) _log.AppendLine(prefix + " Merge Tensioner 1 not found");
            if (mergeTens2 == null) _log.AppendLine(prefix + " Merge Tensioner 2 not found");
            if (mergeRoot == null) _log.AppendLine(prefix + " Small Merge Block TreadMaker not found");
            if (pistTens == null) _log.AppendLine(prefix + " Piston Tensioner 2 not found");

            _log.AppendLine(prefix + " Wrap: " + state);

            switch(state)
            {
                case 0:
                    if(IsMerged(mergeTens1))
                    {
                        return 1;
                    }
                    else
                    {
                        mergeTens1.Enabled = true;
                        mergeTens2.Enabled = true;
                        pistTens.Velocity = -1.0f;
                        _log.AppendLine(prefix + " Wrap: 0");
                        SetHingesTo(GetHinges(5, prefix), 90.0f);
                        SetHingesTo(GetHinges(7, prefix), 65.0f);
                        SetHingesTo(GetHinges(11, prefix), 25.0f);
                        pistTens.Velocity = -1.0f;
                    }
                    break;
                case 1:
                    if (IsMerged(mergeTens2))
                    {
                        return 2;
                    }
                    else
                    {
                        mergeTens2.Enabled = true;
                        _log.AppendLine(prefix + " Wrap: 1");
                        mergeRoot.Enabled = false;
                        SetHingesTo(GetHinges(17, prefix), 25.0f);
                        SetHingesTo(GetHinges(21, prefix), 65.0f);
                        SetHingesTo(GetHinges(23, prefix), 90.0f);
                    }
                    break;
                case 2:
                    if (pistTens.CurrentPosition >= 1.9f)
                    {
                        return 3;
                    }
                    else
                    {
                        _log.AppendLine(prefix + " Wrap: 2");
                        // Go Limp, extend tensioner piston(s)
                        GoLimp(prefix);
                        pistTens.Velocity = 1.0f;
                    }
                    break;
                case 3:
                    if (firstLinks[0].IsAttached || firstLinks[1].IsAttached)
                    {
                        return 4;
                    }
                    else
                    {
                        _log.AppendLine(prefix + " Wrap: 3");
                        // Connect first link
                        firstLinks[0].Attach();
                        firstLinks[1].Attach();
                    }
                    break;
                case 4:
                    if (!mergeTens1.Enabled && !mergeTens2.Enabled)
                    {
                        // done
                        return -1;
                    }
                    else
                    {
                        _log.AppendLine(prefix + " Wrap: 4");
                        mergeTens1.Enabled = false;
                        mergeTens2.Enabled = false;
                    }
                    break;
                case -1:
                    _log.AppendLine(prefix + " Wrap: -1");
                    return -1;
            }
            return state;
        }

        void GoLimp(string prefix)
        {
            var panel = GridTerminalSystem.GetBlockWithName("LCD Panel GoLimp") as IMyTextPanel;
            panel.WriteText("\r\nGoing Limp " + prefix, true);

            var state = _limpState[prefix];

            var linkNum = state;
            var count = 0;
            var hinges = GetHinges(linkNum, prefix);
            // Max of 10 per tick.
            while(hinges != null && hinges.Length == 2 && count < 5)
            {
                panel.WriteText("\r\n" + prefix + " " + linkNum, true);
                GoLimp(hinges[0]);
                GoLimp(hinges[1]);

                linkNum++;
                count++;
                hinges = GetHinges(linkNum, prefix);
            }
            _limpState[prefix] = linkNum;
        }

        void GoLimp(IMyMotorStator hinge)
        {
            hinge.UpperLimitDeg = 90.0f;
            hinge.RotorLock = false;
            hinge.Enabled = false;
        }

        void DetachHinges(int num, string prefix)
        {
            var hinges = GetHinges(num, prefix);

            for (int i = 0; i < hinges.Length; ++i)
                if (hinges[i] != null)
                    hinges[i].Detach();
        }

        void SetHingesTo(IMyMotorStator[] hinges, float angle)
        {
            if (hinges == null) return;
            for (int i = 0; i < hinges.Length; ++i)
                SetHingeTo(hinges[i], angle);
        }

        void SetHingeTo(IMyMotorStator hinge, float angle)
        {
            if (hinge == null) return;

            hinge.UpperLimitDeg = angle;
            hinge.RotorLock = false;
            hinge.Torque = 3000000f;
            hinge.TargetVelocityRPM = 1;
        }

        IMyMotorStator[] GetHinges(int num, string prefix)
        {
            var hingeName = GetLinkHingeName(num, prefix);
            if (_hinges == null)
            {
                var hinge1 = GridTerminalSystem.GetBlockWithName(hingeName + "A") as IMyMotorStator;
                var hinge2 = GridTerminalSystem.GetBlockWithName(hingeName + "B") as IMyMotorStator;
                return new[] { hinge1, hinge2 };
            }
            else
            {
                var hinge1 = _hinges.ContainsKey(hingeName + "A") ? _hinges[hingeName + "A"] : null;
                var hinge2 = _hinges.ContainsKey(hingeName + "B") ? _hinges[hingeName + "B"] : null;
                return new[] { hinge1, hinge2 };
            }
        }

        string GetLinkHingeName(int num, string prefix)
        {
            return prefix + " " + "Hinge Link " + (num < 10 ? "0" : "") + num;
        }

        bool IsMerged(IMyShipMergeBlock mrg1)
        {
            if (mrg1 == null) return false;
            //_log.AppendLine("IsMerged: " + mrg1.CustomName);
            if (mrg1.IsConnected)
            {
                //_log.AppendLine("Block believes it is merged.");
            }

            //Find direction that block merges to
            Matrix mat;
            mrg1.Orientation.GetMatrix(out mat);
            Vector3I up1 = new Vector3I(mat.Up);

            //if(mrg1.CustomName.Contains("Tensioner 2"))
            //{
            //    var directions = new[] { "up", "down", "left", "right", "forward", "backward" };

            //    for (int i = 0; i < directions.Length; ++i)
            //    {
            //        if (CheckMerge(mrg1, directions[i]))
            //            return true;
            //    }
            //    return false;
            //}

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

        bool CheckMerge(IMyShipMergeBlock mrg1, string direction)
        {
            //Find direction that block merges to
            Matrix mat;
            mrg1.Orientation.GetMatrix(out mat);

            var dir = GetDirection(mat, direction);

            //Check if there is a block in front of merge face
            IMySlimBlock sb = mrg1.CubeGrid.GetCubeBlock(mrg1.Position + dir);
            if (sb == null)
            {
                //_log.AppendLine("CheckMerge: block is null (" + direction + ")");
                return false;
            }

            //Check if the other block is actually a merge block
            IMyShipMergeBlock mrg2 = sb.FatBlock as IMyShipMergeBlock;
            if (mrg2 == null)
            {
                //_log.AppendLine("CheckMerge: block is not merge block (" + direction + ")");
                return false;
            }

            //Check that other block is correctly oriented
            mrg2.Orientation.GetMatrix(out mat);
            Vector3I dir2 = GetDirection(mat, direction);

            var result = dir2 == -dir;
            //_log.AppendLine("Block is " + (result ? "" : "NOT ") + "merged. (" + direction + ")");

            return result;
        }

        Vector3I GetDirection(Matrix mat, string direction)
        {
            switch (direction)
            {
                case "up":
                    return new Vector3I(mat.Up);
                case "down":
                    return new Vector3I(mat.Down);
                case "left":
                    return new Vector3I(mat.Left);
                case "right":
                    return new Vector3I(mat.Right);
                case "forward":
                    return new Vector3I(mat.Forward);
                case "backward":
                    return new Vector3I(mat.Backward);
            }
            return new Vector3I(0);
        }

        void WriteLogs()
        {
            Echo(_log.ToString());
            var panel = GridTerminalSystem.GetBlockWithName("LCD Panel TreadMaker Status") as IMyTextPanel;
            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
                Echo(_log.ToString());
            }
        }
        
        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}