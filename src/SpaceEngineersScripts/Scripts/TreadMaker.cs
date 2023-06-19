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

        private class TreadMakerState 
        { 
            public int State; 
            public int NumMade; 
            public string Prefix;
            public int MaxLinks;
        };

        private StringBuilder _log;
        private StringBuilder _errorLog;
        private const int MaxLinks = 22;
        private TreadMakerState _lState = new TreadMakerState { Prefix = "TML", State = -1, NumMade = 0, MaxLinks = MaxLinks };
        private TreadMakerState _rState = new TreadMakerState { Prefix = "TMR", State = -1, NumMade = 0, MaxLinks = MaxLinks };
        private bool _paused = false;
        //private bool _hasPaused = false;

        private int _lWrapState = -1;
        private int _rWrapState = -1;

        private Dictionary<string, IMyMotorStator> _hinges = null;
        private Dictionary<string, int> _limpState = new Dictionary<string, int>();

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "start")
            {
                _lState.State = 0;
                _lState.NumMade = 0;
                _rState.State = 0;
                _rState.NumMade = 0;
                _lWrapState = 0;
                _rWrapState = 0;
                _hinges = null;
                _limpState["TMR"] = 0;
                _limpState["TML"] = 0;
                //_hasPaused = false;
            }
            else if (argument == "stop")
            {
                _lState.State = -1;
                _rState.State = -1;
                _lState.NumMade = 0;
                _rState.NumMade = 0;
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

            _errorLog = new StringBuilder("");
            _log = new StringBuilder("");
            _log.AppendLine("LState is: " + _lState.State.ToString());
            _log.AppendLine("RState is: " + _rState.State.ToString());

            if (_paused)
            {
                _log.AppendLine("Paused");
                WriteLogs();
                return;
            }

            if (_lState.State != -2)
            {
                MakeTread(_lState);
                _log.AppendLine("L has made " + _lState.NumMade + " links");
            }
            else
            {
                _log.AppendLine("L has made enough links!");
            }

            if (_rState.State != -2)
            {
                MakeTread(_rState);             
                _log.AppendLine("R has made " + _rState.NumMade + " links");
            }
            else
            {
                _log.AppendLine("R has made enough links!");
            }

            if(_rState.State == -2 && _lState.State == -2)
            {
                // Pause between stages
                //if(!_hasPaused)
                //{
                //    _paused = true;
                //    _hasPaused = true;
                //    WriteLogs();
                //    return;
                //}

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
                catch (Exception e)
                {
                    _log.AppendLine("Exception wrapping: " + e.Message);
                    WriteLogs();
                    return;
                }
            }

            _log.AppendLine("End Tick");
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
            var merge = GridTerminalSystem.GetBlockWithName(prefix + " Merge TreadMaker") as IMyShipMergeBlock;

            if (merge != null) merge.Enabled = false;
            if (magPlate1 != null) magPlate1.Unlock();
            if (magPlate2 != null) magPlate2.Unlock();
            if (pistonGrab1 != null) pistonGrab1.Velocity = -1.0f;
            if (pistonGrab2 != null) pistonGrab2.Velocity = -1.0f;
        }

        void MakeTread(TreadMakerState state)
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

            var lastHingeName = state.Prefix + " " + "Hinge Link " + (state.NumMade < 10 ? "0" : "") + state.NumMade;

            var pistonGrab1 = GridTerminalSystem.GetBlockWithName(state.Prefix + " Piston Grab 1") as IMyPistonBase;
            var pistonGrab2 = GridTerminalSystem.GetBlockWithName(state.Prefix + " Piston Grab 2") as IMyPistonBase;
            var magPlate1 = GridTerminalSystem.GetBlockWithName(state.Prefix + " Magnetic Plate Grab 1") as IMyLandingGear;
            var magPlate2 = GridTerminalSystem.GetBlockWithName(state.Prefix + " Magnetic Plate Grab 2") as IMyLandingGear;
            var merge = GridTerminalSystem.GetBlockWithName(state.Prefix + " Merge TreadMaker") as IMyShipMergeBlock;
            var newHinge1 = GridTerminalSystem.GetBlockWithName(state.Prefix + " Link Hinge 1") as IMyMotorStator;
            var newHinge2 = GridTerminalSystem.GetBlockWithName(state.Prefix + " Link Hinge 2") as IMyMotorStator;
            var newMagPlate = GridTerminalSystem.GetBlockWithName(state.Prefix + " Link Magplate") as IMyLandingGear;
            var lastHinge1 = GridTerminalSystem.GetBlockWithName(lastHingeName + "A") as IMyMotorStator;
            var lastHinge2 = GridTerminalSystem.GetBlockWithName(lastHingeName + "B") as IMyMotorStator;
            var grind = GridTerminalSystem.GetBlockWithName(state.Prefix + " Grinder") as IMyShipGrinder;
            var projb = GridTerminalSystem.GetBlockWithName(state.Prefix + " Projector B") as IMyProjector;

            if (newHinge1 == null || newHinge2 == null)
                _log.AppendLine("At least one new hinge not found.");

            if (pistonGrab1 == null) { _log.AppendLine(state.Prefix + " Can't find pistonGrab1"); state.State = -1; }
            if (pistonGrab2 == null) { _log.AppendLine(state.Prefix + " Can't find pistonGrab2"); state.State = -1; }
            if (magPlate1 == null) { _log.AppendLine(state.Prefix + " Can't find magPlate1"); state.State = -1; }
            if (magPlate2 == null) { _log.AppendLine(state.Prefix + " Can't find magPlate2"); state.State = -1; }
            if (merge == null) { _log.AppendLine(state.Prefix + " Can't find merge"); state.State = -1; }
            if (grind == null) { _log.AppendLine(state.Prefix + " Can't find grind"); state.State = -1; }
            if (projb == null) { _log.AppendLine(state.Prefix + " Can't find projb"); state.State = -1; }

            if (state.State != -1)
            {
                grind.Enabled = state.NumMade > 2;
                projb.Enabled = (state.NumMade % 2 == 1) && state.NumMade != 2;
            }
            else
            {
                grind.Enabled = false;
                // Keep this on when not making treads as it makes it easier to place blueprint
                projb.Enabled = true;
            }

            switch (state.State)
            {
                case -1:
                    // Waiting to start.
                    _log.AppendLine(state.Prefix + " Waiting for start command.");
                    break;
                case 0:
                    // Build/grab new link. either is first link or old link already attached
                    if ( // next state when:
                        IsMerged(merge)
                        && (magPlate1.IsLocked || magPlate2.IsLocked)
                        && pistonGrab1.CurrentPosition == 2.0f
                        && pistonGrab2.CurrentPosition == 2.0f
                        // If this is the first link, detach initial hinges before continuing
                        && (state.NumMade == 0
                            ? (lastHinge1 != null && !lastHinge1.IsAttached
                               && lastHinge2 != null && !lastHinge2.IsAttached)
                            : true)
                        )
                    {
                        state.State = 1;
                    }
                    else
                    {
                        var isMerged = IsMerged(merge);

                        // keep trying to get there
                        magPlate1.AutoLock = false;
                        magPlate2.AutoLock = false;
                        pistonGrab1.MaxLimit = 2;
                        pistonGrab2.MaxLimit = 2;
                        merge.Enabled = true;
                        if (newHinge1 != null) newHinge1.CustomName = lastHingeName + "A";
                        if (newHinge2 != null) newHinge2.CustomName = lastHingeName + "B";
                        if (newMagPlate != null) newMagPlate.CustomName = state.Prefix + " " + "MagPlate " + (state.NumMade < 10 ? "0" : "") + state.NumMade;
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
                        if (state.NumMade == 0)
                        {
                            // start fresh. detach any existing trash connected to our first link
                            if (lastHinge1 != null) lastHinge1.Detach();
                            if (lastHinge2 != null) lastHinge2.Detach();
                        }
                        if(!isMerged || newHinge1 == null || newHinge2 == null)
                        {
                            // Pistons can get in the way of blocks being placed, retract them until the blocks are made
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
                    if (
                        IsMerged(merge)
                        && pistonGrab1.CurrentPosition <= 0.5f
                        && pistonGrab2.CurrentPosition == 0.5f)
                    {
                        state.State = 2;
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
                        _log.AppendLine(state.Prefix + " " + "newHinge1 not found.");
                    }
                    if (newHinge2 == null)
                    {
                        _log.AppendLine(state.Prefix + " " + "newHinge2 not found.");
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
                        state.State = 3;
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
                        state.State = 4;
                    }
                    else
                    {
                        newHinge1.Attach();
                        newHinge2.Attach();
                    }
                    break;
                case 4:
                    // Unlock mag plate and extend piston 2m. Raname new link objects as they are now part of the tread
                    if (pistonGrab1.CurrentPosition == 2.0f
                        && pistonGrab2.CurrentPosition == 2.0f
                        && !magPlate1.IsLocked
                        && !magPlate2.IsLocked)
                    {
                        state.NumMade += 1;
                        if (state.NumMade >= state.MaxLinks)
                        {
                            grind.Enabled = false;
                            state.State = -2;
                        }
                        else
                        {
                            state.State = 0;
                        }

                        if (newHinge1 != null) newHinge1.CustomName = state.Prefix + " " + "Hinge Link " + (state.NumMade < 10 ? "0" : "") + state.NumMade + "A";
                        if (newHinge2 != null) newHinge2.CustomName = state.Prefix + " " + "Hinge Link " + (state.NumMade < 10 ? "0" : "") + state.NumMade + "B";
                        if (newMagPlate != null) newMagPlate.CustomName = state.Prefix + " " + "MagPlate " + (state.NumMade < 10 ? "0" : "") + state.NumMade;
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
                    _log.AppendLine(state.Prefix + " " + "Invalid state. :(");
                    break;
            }
        }

        int WrapTread(string prefix, int state)
        {
            var mergeTens1 = GridTerminalSystem.GetBlockWithName(prefix + " Merge Tensioner 1") as IMyShipMergeBlock;
            var mergeTens2 = GridTerminalSystem.GetBlockWithName(prefix + " Merge Tensioner 2") as IMyShipMergeBlock;
            var mergeRoot = GridTerminalSystem.GetBlockWithName(prefix + " Merge TreadMaker") as IMyShipMergeBlock;
            var pistTens = GridTerminalSystem.GetBlockWithName(prefix + " Piston Tensioner 2") as IMyPistonBase;

            if (mergeTens1 == null) _log.AppendLine(prefix + " Merge Tensioner 1 not found");
            if (mergeTens2 == null) _log.AppendLine(prefix + " Merge Tensioner 2 not found");
            if (mergeRoot == null) _log.AppendLine(prefix + " Merge TreadMaker not found");
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
                        SetHingesTo(GetHinges(4, prefix), 20.0f);
                        SetHingesTo(GetHinges(5, prefix), 45.0f);
                        SetHingesTo(GetHinges(6, prefix), 45.0f);
                        SetHingesTo(GetHinges(7, prefix), 45.0f);
                        SetHingesTo(GetHinges(10, prefix), 25.0f);
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
                        mergeRoot.Enabled = false;
                        SetHingesTo(GetHinges(15, prefix), 25.0f);
                        SetHingesTo(GetHinges(17, prefix), 10.0f);
                        SetHingesTo(GetHinges(18, prefix), 45.0f);
                        SetHingesTo(GetHinges(19, prefix), 45.0f);
                        SetHingesTo(GetHinges(20, prefix), 40.0f);
                        SetHingesTo(GetHinges(21, prefix), 10.0f);
                    }
                    break;
                case 2:
                    if (pistTens.CurrentPosition >= 1.9f)
                    {
                        return 3;
                    }
                    else
                    {
                        // Go Limp, extend tensioner piston(s)
                        GoLimp(prefix);
                        pistTens.Velocity = 1.0f;
                    }
                    break;
                case 3:
                    var firstLinks = GetHinges(0, prefix);
                    if(firstLinks == null || firstLinks[0] == null || firstLinks[1] == null)
                    {
                        _errorLog.AppendLine("Cannot find first links when wrapping.");
                    }
                    if (firstLinks[0].IsAttached || firstLinks[1].IsAttached)
                    {
                        return 4;
                    }
                    else
                    {
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
                        mergeTens1.Enabled = false;
                        mergeTens2.Enabled = false;
                        pistTens.Velocity = -1.0f;
                    }
                    break;
                case -1:
                    _log.AppendLine("Wrap " + prefix + " is done!");
                    return -1;
            }
            return state;
        }

        void GoLimp(string prefix)
        {
            var state = _limpState[prefix];

            var linkNum = state;
            var count = 0;
            var hinges = GetHinges(linkNum, prefix);
            // Max of N per tick. (was a debugging thing, leaving it in because limiting actions per tick is just nice to do)
            while(hinges != null && hinges.Length == 2 && count < 5)
            {
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
            //if (mrg1.IsConnected)
            //{
            //    _log.AppendLine("Block believes it is merged.");
            //}

            //Find direction that block merges to
            Matrix mat;
            mrg1.Orientation.GetMatrix(out mat);
            // Small grid small merge blocks use 'up', everything else uses 'right'
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

        void WriteLogs()
        {
            Echo(_log.ToString());
            var panel = GridTerminalSystem.GetBlockWithName("LCD Panel TreadMaker Status") as IMyTextPanel;
            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
            }

            Echo(_errorLog.ToString());
            var errorPanel = GridTerminalSystem.GetBlockWithName("LCD Panel TreadMaker Errors") as IMyTextPanel;
            if (errorPanel != null)
            {
                errorPanel.WriteText(_errorLog.ToString(), false);
            }
        }
        
        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}