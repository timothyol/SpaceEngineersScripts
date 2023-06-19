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

namespace SpaceEngineersScripting.MobileBorer
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

        private const float Velocity = 0.1f;
        // A is lighter and should be able to move faster
        private const float AChassisCoefficient = 10f;
        private const float ReverseCoefficient = 3f;
        private const float RadianRatio = 57.2958f;
        private const float ProgressCompleted = .995f;

        private const string ArgPause = "Pause";
        private const string ArgUnpause = "Unpause";
        private const string ArgDeploy = "Deploy";
        private const string ArgUndeploy = "Undeploy";
        private const string ArgEngage = "Engage";
        private const string ArgBackToDock = "Back to Dock";
        private const string ArgReverse = "Reverse";

        // Restrict hinges if deviance exceeds this value
        private const float RestrictionThresholdPct = .05f;

        private StringBuilder _log;
        private const string LogPanelName = "LCD Panel MobileBorerLogs";
        private const string ControlPanelName = "LCD Panel MobileBorerDisplay";

        private bool _hasHadFirstTick = false;

        public enum RigState
        {
            Unknown,
            Loaded,
            Deploying,
            Undeploying,
            Docked,
            ExtendingA,
            ExtendingB,
            RetractingA,
            RetractingB
        }

        public class RotorInfo
        {
            public string Name;
            public float? Coefficient = null;
            public IMyMotorStator Rotor;
            public float Min;
            public float Max;
            private StringBuilder _log = null;

            public void Initialize(IMyGridTerminalSystem term, StringBuilder log)
            {
                _log = log;
                Rotor = term.GetBlockWithName(Name) as IMyMotorStator;
            }

            // returns 0.0 - 1.0
            public float GetPercentage()
            {
                var angle = Rotor.Angle * RadianRatio;
                return (angle - Min) / (Max - Min);
            }

            public void SetVelocity(float targetVelocityRpm)
            {
                Rotor.TargetVelocityRPM = targetVelocityRpm * Coefficient.Value;
            }

            public void Restrict(float minPct, float maxPct, float targetVelocityRpm, float halfDiff)
            {
                var rotorPct = GetPercentage();
                var display = Rotor.CustomName + "(" + rotorPct * 100f + "%)";

                if (targetVelocityRpm > 0f)
                {
                    if (rotorPct > maxPct - halfDiff)
                    {
                        Rotor.TargetVelocityRPM = 0;
                        _log.AppendLine("Off: " + display);
                    }
                    else
                    {
                        Rotor.TargetVelocityRPM = targetVelocityRpm * Coefficient.Value;
                        _log.AppendLine("On: " + display);
                    }
                }
                else if (targetVelocityRpm < 0f)
                {
                    if (rotorPct < minPct + halfDiff)
                    {
                        Rotor.TargetVelocityRPM = 0;
                        _log.AppendLine("Off: " + display);
                    }
                    else
                    {
                        Rotor.TargetVelocityRPM = targetVelocityRpm * Coefficient.Value;
                        _log.AppendLine("On: " + display);
                    }
                }
            }
        }

        public class RotorGroup
        {
            public string Name = "";
            public List<RotorInfo> Rotors = null;

            private StringBuilder _log;

            public void Initialize(IMyGridTerminalSystem term, StringBuilder log)
            {
                _log = log;
                float? baseDegreeRange = null;
                for (int i = 0; i < Rotors.Count; ++i)
                {
                    Rotors[i].Initialize(term, log);

                    if (Rotors[i].Coefficient == null)
                    {
                        if (baseDegreeRange == null)
                        {
                            baseDegreeRange = Rotors[i].Max - Rotors[i].Min;
                            Rotors[i].Coefficient = 1f;
                        }
                        else
                        {
                            var degreeRange = Rotors[i].Max - Rotors[i].Min;
                            Rotors[i].Coefficient = degreeRange / baseDegreeRange.Value;
                        }
                    }
                }
            }

            public float Move(float targetVelocityRpm)
            {
                if (targetVelocityRpm == 0f)
                {
                    StopAll();
                    return 0;
                }

                var minPct = GetMinPct();
                var maxPct = GetMaxPct();
                _log.AppendLine(Name + " Moving:");
                _log.AppendLine("Min Percentage: " + minPct + "%");
                _log.AppendLine("Max Percentage: " + maxPct + "%");

                RestrictRotors(minPct, maxPct, targetVelocityRpm);

                return targetVelocityRpm > 0 ? minPct : maxPct;
            }

            public void RestrictRotors(float minPct, float maxPct, float targetVelocityRpm)
            {
                if (maxPct - minPct > RestrictionThresholdPct)
                {
                    var halfDiff = (maxPct - minPct) / 2.0f;
                    for (int i = 0; i < Rotors.Count; ++i)
                    {
                        Rotors[i].Restrict(minPct, maxPct, targetVelocityRpm, halfDiff);
                    }
                }
                else
                {
                    for (int i = 0; i < Rotors.Count; ++i)
                    {
                        Rotors[i].SetVelocity(targetVelocityRpm);
                    }
                }
            }

            public float GetMaxPct()
            {
                if (Rotors.Count == 0) return 0;

                var maxPct = Rotors[0].GetPercentage();

                for (int rotorNum = 1; rotorNum < Rotors.Count; ++rotorNum)
                {
                    maxPct = Math.Max(maxPct, Rotors[rotorNum].GetPercentage());
                    rotorNum++;
                }

                return maxPct;
            }

            public float GetMinPct()
            {
                if (Rotors.Count == 0) return 0;

                var minPct = Rotors[0].GetPercentage();

                for (int rotorNum = 1; rotorNum < Rotors.Count; ++rotorNum)
                {
                    minPct = Math.Min(minPct, Rotors[rotorNum].GetPercentage());
                    rotorNum++;
                }

                return minPct;
            }

            public void StopAll()
            {
                for (int i = 0; i < Rotors.Count; ++i)
                {
                    Rotors[i].SetVelocity(0);
                }
            }
        }

        public class MiningRigExecuteState
        {
            public RigState State = RigState.Unknown;
            public List<string> Options = new List<string> { ArgDeploy };
            public bool Pause = false;
            public string StatusText = "Not Started";
            public DateTime? StateStarted;
            public RigState? StateStartedState;
        }

        private readonly MiningRigExecuteState _rigState = new MiningRigExecuteState();

        // root hinges: 3.25 - 63.5
        // elbow hinges: -86.75 - -26.5000
        private readonly RotorGroup _walkerLegs = new RotorGroup
        {
            Name = "Walker Legs",
            Rotors = new List<RotorInfo>
            {
                new RotorInfo { Name = "Hinge 3x3 E1A", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 E1B", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 R1A", Min = 3.25f, Max = 63.5f },
                new RotorInfo { Name = "Hinge 3x3 R1B", Min = 3.25f, Max = 63.5f },
                new RotorInfo { Name = "Hinge 3x3 E2A", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 E2B", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 R2A", Min = 3.25f, Max = 63.5f },
                new RotorInfo { Name = "Hinge 3x3 R2B", Min = 3.25f, Max = 63.5f },
                new RotorInfo { Name = "Hinge 3x3 E3A", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 E3B", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 R3A", Min = 3.25f, Max = 63.5f },
                new RotorInfo { Name = "Hinge 3x3 R3B", Min = 3.25f, Max = 63.5f },
                new RotorInfo { Name = "Hinge 3x3 E4A", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 E4B", Min = -86.75f, Max = -26.5f },
                new RotorInfo { Name = "Hinge 3x3 R4A", Min = 3.25f, Max = 63.5f },
                new RotorInfo { Name = "Hinge 3x3 R4B", Min = 3.25f, Max = 63.5f },
            }
        };

        private readonly RotorGroup _deployHinges = new RotorGroup
        {
            Name = "Deploy Hinges",
            Rotors = new List<RotorInfo>
            { 
                new RotorInfo {Name = "Hinge 3x3 Mining Rig Base", Min = 0f, Max = 90f },
                new RotorInfo {Name = "Hinge StoneDropper 1", Min = 0f, Max = 75f },
                new RotorInfo {Name = "Hinge StoneDropper 2", Min = 0f, Max = 75f },
                new RotorInfo {Name = "Hinge StoneDropper 3", Min = 0f, Max = 75f },
                new RotorInfo {Name = "Hinge StoneDropper 4", Min = 0f, Max = 75f },
                new RotorInfo {Name = "Hinge StoneDropper 5", Min = 0f, Max = 75f },
                new RotorInfo {Name = "Hinge StoneDropper 6", Min = 0f, Max = 75f },
                new RotorInfo {Name = "Hinge SD E 1", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 2", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 3", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 4", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 5", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 6", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 7", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 8", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 9", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 10", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 11", Min = -90f, Max = -7.5f },
                new RotorInfo {Name = "Hinge SD E 12", Min = -90f, Max = -7.5f }
            }
        };

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        void Main(string argument, UpdateType updateSource)
        {
            if (_log != null)
                _log.Clear();
            else
                _log = new StringBuilder("");

            if(!_hasHadFirstTick)
            {
                // Waking up. init rotor groups
                _walkerLegs.Initialize(GridTerminalSystem, _log);
                _deployHinges.Initialize(GridTerminalSystem, _log);
                _hasHadFirstTick = true;
            }

            int? argNum = null;

            if (!string.IsNullOrEmpty(argument))
            {
                try
                {
                    argNum = int.Parse(argument);
                }
                catch
                {
                    _log.AppendLine("Argument could not be converted to int. Arg: " + argument);
                }
            }

            string parsedArgument = null;

            if(argNum.HasValue)
            {
                parsedArgument = _rigState.Options[argNum.Value - 1];
            }

            _log.AppendLine("Argument: " + parsedArgument);

            DrillRigExecute(_rigState, parsedArgument);

            WriteDisplay(_rigState);

            WriteLogs();
        }

        void WriteDisplay(MiningRigExecuteState state)
        {
            var output = new StringBuilder("");

            output.AppendLine("Mobile Mining Rig");
            output.AppendLine("Status: " + state.StatusText);
            output.AppendLine("Options:");
            for(int i = 0; i < state.Options.Count; ++i)
            {
                output.AppendLine((i + 1) + ": " + state.Options[i]);
            }

            var panel = GridTerminalSystem.GetBlockWithName(ControlPanelName) as IMyTextPanel;
            if (panel != null)
            {
                panel.WriteText(output.ToString(), false);
            }
        }

        void DrillRigExecute(MiningRigExecuteState state, string argument)
        {
            //States:
            // Loaded - Walker is docked and base hinge retracted, ready for movement
            // Undeploying - Walker docked, base hinge retracting
            // Deploying - Walker docked, base hinge extending

            // Docked - Walker is fully retracted to the first node
            // Extending - Walker is moving down the rail
            // Retracting - Walker is moving back to dock
            // Extending/Retracting substates
            //  initial: both merges merged, legs fully retracted
            //  Chassis B moving: A merges merged, legs extending
            //  halfway transition: both merges merged, legs fully extended
            //  Chassis A moving: B Merges merged, legs retracting

            //TODO:
            // Track depth

            state.Options.Clear();

            if(!string.IsNullOrEmpty(argument))
            {
                state.StateStarted = null;
                state.StateStartedState = null;
                switch (argument)
                {
                    case ArgPause:
                        state.Pause = true;
                        break;
                    case ArgUnpause:
                        state.Pause = false;
                        break;
                    case ArgDeploy:
                        state.State = RigState.Deploying;
                        break;
                    case ArgUndeploy:
                        state.State = RigState.Undeploying;
                        break;
                    case ArgEngage:
                        state.State = RigState.ExtendingB;
                        break;
                    case ArgBackToDock:
                        if (state.State == RigState.ExtendingA) state.State = RigState.RetractingA;
                        if (state.State == RigState.ExtendingB) state.State = RigState.RetractingB;
                        break;
                    case ArgReverse:
                        if (state.State == RigState.RetractingA) state.State = RigState.ExtendingA;
                        if (state.State == RigState.RetractingB) state.State = RigState.ExtendingB;
                        break;
                }
            }

            if (state.Pause)
            {
                _walkerLegs.Move(0);
                _deployHinges.Move(0);
                state.Options.Add(ArgUnpause);
                return;
            }
            else
            {
                state.Options.Add(ArgPause);
            }

            var mergeA1 = GridTerminalSystem.GetBlockWithName("Merge Block A1") as IMyShipMergeBlock;
            var mergeA2 = GridTerminalSystem.GetBlockWithName("Merge Block A2") as IMyShipMergeBlock;
            var mergeB1 = GridTerminalSystem.GetBlockWithName("Merge Block B1") as IMyShipMergeBlock;
            var mergeB2 = GridTerminalSystem.GetBlockWithName("Merge Block B2") as IMyShipMergeBlock;
            var connW = GridTerminalSystem.GetBlockWithName("Connector Walker") as IMyShipConnector;
            var connOrigin = GridTerminalSystem.GetBlockWithName("Connector Origin") as IMyShipConnector;
            var tStandPiston1 = GridTerminalSystem.GetBlockWithName("Piston Trailer Stand 1") as IMyPistonBase;
            var tStandPiston2 = GridTerminalSystem.GetBlockWithName("Piston Trailer Stand 2") as IMyPistonBase;
            var tStandMagPlate1 = GridTerminalSystem.GetBlockWithName("Magnetic Plate Trailer Stand 1") as IMyLandingGear;
            var tStandMagPlate2 = GridTerminalSystem.GetBlockWithName("Magnetic Plate Trailer Stand 2") as IMyLandingGear;
            var anchorPiston1 = GridTerminalSystem.GetBlockWithName("Piston Anchor 1") as IMyPistonBase;
            var anchorPiston2 = GridTerminalSystem.GetBlockWithName("Piston Anchor 2") as IMyPistonBase;
            var anchor1 = GridTerminalSystem.GetBlockWithName("Large MagPlate Anchor 1") as IMyLandingGear;
            var anchor2 = GridTerminalSystem.GetBlockWithName("Large MagPlate Anchor 2") as IMyLandingGear;
            var proj = GridTerminalSystem.GetBlockWithName("Projector Mining Rig") as IMyProjector;
            var welders = GridTerminalSystem.GetBlockGroupWithName("Welders");
            var grinders = GridTerminalSystem.GetBlockGroupWithName("Grinders");
            var drills = GridTerminalSystem.GetBlockGroupWithName("Drills");
            var stoneDropSorters = GridTerminalSystem.GetBlockGroupWithName("StoneDrop Sorters");
            var parkingGears = GridTerminalSystem.GetBlockGroupWithName("Parking Gears");
            var stoneDropGears = GridTerminalSystem.GetBlockGroupWithName("StoneDrop Gears");

            var timeInState = state.StateStarted == null ? 0 : (DateTime.Now - state.StateStarted.Value).TotalMilliseconds;

            switch (state.State)
            {
                case RigState.Unknown:
                    var basePos = _deployHinges.GetMaxPct();
                    var walkerPos = _walkerLegs.GetMaxPct();
                    var baseSpeed = _deployHinges.Rotors[0].Rotor.TargetVelocityRPM;
                    var walkerSpeed = _walkerLegs.Rotors[0].Rotor.TargetVelocityRPM;

                    // Waking up. try to figure out state.
                    if (basePos < .01f)
                    {
                        // Probably loaded?
                        state.State = RigState.Loaded;
                    }
                    else if (basePos >= .01f && basePos <= .99f)
                    {
                        // deploying or undeploying
                        if (baseSpeed <= 0f)
                        {
                            // Call this undeploying
                            state.State = RigState.Undeploying;
                        }
                        else
                        {
                            state.State = RigState.Deploying;
                        }
                    }
                    else if (IsMerged(mergeA1))
                    {
                        if (IsMerged(mergeB1))
                        {
                            if (walkerPos < .01f)
                            {
                                if (connOrigin.Status == MyShipConnectorStatus.Connected)
                                {
                                    // Docked
                                    state.State = RigState.Docked;
                                }
                                else
                                {
                                    // Both connect and legs retracted, decide based on which direction they're going
                                    if (walkerSpeed >= 0f)
                                    {
                                        state.State = RigState.ExtendingB;
                                    }
                                    else
                                    {
                                        state.State = RigState.RetractingA;
                                    }
                                }
                            }
                            else
                            {
                                // Both connect and legs extended, decide based on which direction they're going
                                if (walkerSpeed >= 0f)
                                {
                                    state.State = RigState.ExtendingA;
                                }
                                else
                                {
                                    state.State = RigState.RetractingB;
                                }
                            }
                        }
                        else
                        {
                            if (walkerSpeed >= 0f)
                            {
                                state.State = RigState.ExtendingB;
                            }
                            else
                            {
                                state.State = RigState.RetractingB;
                            }
                        }
                    }
                    else if (IsMerged(mergeB1))
                    {
                        // We already covered states where both are merged, so assume A is not merged
                        if (walkerSpeed >= 0f)
                        {
                            state.State = RigState.ExtendingA;
                        }
                        else
                        {
                            state.State = RigState.RetractingA;
                        }
                    }
                    else 
                    {
                        // Cannot determine state. 
                        state.StatusText = "Cannot determine state. basePos=" + basePos.ToString(".##")
                            + ", baseSpeed=" + baseSpeed.ToString(".##")
                            + ", walkerPos=" + walkerPos.ToString(".##")
                            + ", walkerSpeed=" + walkerSpeed.ToString(".##");
                    }
                    break;
                case RigState.Loaded:
                    // Loaded. waiting for command to deploy
                    // Available commands:
                    // 1: Deploy
                    SetEnabled(false, welders, grinders, drills, stoneDropSorters);
                    state.Options.Insert(0, ArgDeploy);
                    state.StatusText = "Loaded";
                    if (timeInState > 2000)
                    {
                        SetGearsLocked(parkingGears, true);
                        SetGearsLocked(stoneDropGears, true);
                        tStandPiston1.Velocity = -1.0f;
                        tStandPiston2.Velocity = -1.0f;
                        tStandMagPlate1.Unlock();
                        tStandMagPlate2.Unlock();
                    }
                    break;
                case RigState.Deploying:
                    // Deploying. extend base hinge. 
                    // Available commands:
                    // 1: Undeploy
                    SetEnabled(false, welders, grinders, drills, stoneDropSorters);
                    SetGearsLocked(parkingGears, false);
                    SetGearsLocked(stoneDropGears, false);
                    tStandPiston1.Velocity = 1.0f;
                    tStandPiston2.Velocity = 1.0f;
                    tStandMagPlate1.Lock();
                    tStandMagPlate2.Lock();
                    anchorPiston1.Retract();
                    anchorPiston2.Retract();
                    anchor1.Unlock();
                    anchor2.Unlock();
                    state.Options.Insert(0, ArgUndeploy);
                    if (tStandMagPlate1.IsLocked && tStandMagPlate2.IsLocked)
                    {
                        var deployProgress = _deployHinges.Move(0.5f);
                        state.StatusText = "Deploying (" + (deployProgress * 100f).ToString("##.#") + "%)";
                        if (deployProgress > ProgressCompleted
                            // It can get stuck towards the end, give up deploying all the way after a while.
                            || (deployProgress > .95f && timeInState > 15000))
                        {
                            // next state, docked
                            state.State = RigState.Docked;
                        }
                    }
                    break;
                case RigState.Undeploying:
                    // Undeploying
                    // Available commands:
                    // 1: Deploy
                    SetEnabled(false, welders, grinders, drills, stoneDropSorters);
                    SetGearsLocked(parkingGears, false);
                    tStandPiston1.Velocity = 1.0f;
                    tStandPiston2.Velocity = 1.0f;
                    tStandMagPlate1.Lock();
                    tStandMagPlate2.Lock();
                    anchorPiston1.Retract();
                    anchorPiston2.Retract();
                    anchor1.Unlock();
                    anchor2.Unlock();
                    state.Options.Insert(0, ArgDeploy);
                    if (tStandMagPlate1.IsLocked && tStandMagPlate2.IsLocked)
                    {
                        var retractProgress = _deployHinges.Move(-0.5f);
                        state.StatusText = "Undeploying (" + ((1f - retractProgress) * 100f).ToString("##.#") + "%)";
                        if (retractProgress < (1 - ProgressCompleted))
                        {
                            // next state, docked
                            state.State = RigState.Loaded;
                        }
                    }
                    break;
                case RigState.Docked:
                    // Docked. Wait for command to undeploy or begin mining.
                    // Also, lock magplates
                    // Available commands:
                    // Undeploy
                    // Engage
                    proj.Enabled = false;
                    SetEnabled(false, welders, grinders, drills);
                    SetEnabled(true, stoneDropSorters);
                    anchorPiston1.Extend();
                    anchorPiston2.Extend();
                    if (anchorPiston1.CurrentPosition >= anchorPiston1.MaxLimit) anchor1.Lock();
                    if (anchorPiston2.CurrentPosition >= anchorPiston2.MaxLimit) anchor2.Lock();
                    //anchor1.Lock();
                    //anchor2.Lock();
                    state.Options.InsertRange(0, new[] { ArgEngage, ArgUndeploy });
                    state.StatusText = "Docked. Awaiting Engage command";
                    break;
                case RigState.ExtendingB:
                    // Extending | Chassis B moving. 
                    // 1. Merge A
                    // 2. Unmerge B
                    // 3. Extend legs
                    // TODO: Max depth checking
                    // 4. Wait until fully extended
                    // 5. goto ExtendingA
                    proj.Enabled = true;
                    SetEnabled(true, welders, drills, stoneDropSorters);
                    SetEnabled(grinders, false);
                    anchorPiston1.Extend();
                    anchorPiston2.Extend();
                    if (anchorPiston1.CurrentPosition >= anchorPiston1.MaxLimit) anchor1.Lock();
                    if (anchorPiston2.CurrentPosition >= anchorPiston2.MaxLimit) anchor2.Lock();
                    connW.Connect();
                    mergeA1.Enabled = true;
                    mergeA2.Enabled = true;
                    if(IsMerged(mergeA1) && IsMerged(mergeA2))
                    {
                        var progress = _walkerLegs.Move(Velocity);
                        state.StatusText = "Extending B (" + (progress * 100f).ToString("##.#") + "%)";
                        if (progress > ProgressCompleted)
                        {
                            mergeB1.Enabled = true;
                            mergeB2.Enabled = true;
                            if (IsMerged(mergeB1) && IsMerged(mergeB2))
                            {
                                state.State = RigState.ExtendingA;
                            }
                        }
                        else
                        {
                            mergeB1.Enabled = false;
                            mergeB2.Enabled = false;
                        }
                    }
                    else
                    {
                        state.StatusText = "Extending B (Waiting for merge)";
                    }
                    state.Options.InsertRange(0, new[] { ArgBackToDock });
                    break;
                case RigState.ExtendingA:
                    // Extending | Chassis A moving                    
                    // 1. Merge B, disconnect connector
                    // 2. Merge A
                    // 3. retract legs
                    proj.Enabled = true;
                    SetEnabled(true, welders, drills, stoneDropSorters);
                    SetEnabled(grinders, false);
                    connW.Disconnect();
                    mergeB1.Enabled = true;
                    mergeB2.Enabled = true;
                    if(IsMerged(mergeB1) && IsMerged(mergeB2))
                    {
                        var progress = _walkerLegs.Move(-Velocity * AChassisCoefficient);
                        state.StatusText = "Extending A (" + ((1f - progress) * 100f).ToString("##.#") + "%)";
                        if (progress < (1 - ProgressCompleted))
                        {
                            mergeA1.Enabled = true;
                            mergeA2.Enabled = true;
                            if (IsMerged(mergeA1) && IsMerged(mergeA2))
                            {
                                state.State = RigState.ExtendingB;
                            }
                        }
                        else
                        {
                            mergeA1.Enabled = false;
                            mergeA2.Enabled = false;
                        }
                    }
                    else
                    {
                        state.StatusText = "Extending A (Waiting for merge)";
                    }
                    state.Options.InsertRange(0, new[] { ArgBackToDock });
                    break;
                case RigState.RetractingA:
                    // Retracting | Chassis A moving
                    // 1. Merge B
                    // 2. Unmerge A
                    // 3. Extend Legs
                    // 4. wait for legs fully extended
                    // 5. goto RetractingB
                    proj.Enabled = false;
                    SetEnabled(false, welders, drills);
                    SetEnabled(true, grinders, stoneDropSorters);
                    connW.Disconnect();
                    mergeB1.Enabled = true;
                    mergeB2.Enabled = true;
                    if (IsMerged(mergeB1) && IsMerged(mergeB2))
                    {
                        var progress = _walkerLegs.Move(Velocity * AChassisCoefficient);
                        state.StatusText = "Retracting A (" + (progress * 100f).ToString("##.#") + "%)";
                        if (progress > ProgressCompleted)
                        {
                            mergeA1.Enabled = true;
                            mergeA2.Enabled = true;
                            if (IsMerged(mergeA1) && IsMerged(mergeA2))
                            {
                                state.State = RigState.RetractingB;
                            }
                        }
                        else
                        {
                            mergeA1.Enabled = false;
                            mergeA2.Enabled = false;
                        }
                    }
                    else
                    {
                        state.StatusText = "Retracting A (Waiting for merge)";
                    }
                    state.Options.InsertRange(0, new[] { ArgReverse });
                    break;
                case RigState.RetractingB:
                    // Retracting | Chassis B moving
                    // 1. Merge A
                    // 2. Unmerge B
                    // 3. Retract legs
                    // 4. once fully retracted, merge B
                    // 5. If origin connected, go to Docked, else RetractingA     
                    proj.Enabled = false;
                    SetEnabled(false, welders, drills);
                    SetEnabled(true, grinders, stoneDropSorters);
                    connW.Connect();
                    mergeA1.Enabled = true;
                    mergeA2.Enabled = true;
                    if (IsMerged(mergeA1) && IsMerged(mergeA2))
                    {
                        var progress = _walkerLegs.Move(-Velocity * ReverseCoefficient);
                        state.StatusText = "Retracting B (" + ((1f - progress) * 100f).ToString("##.#") + "%)";
                        if (progress < (1 - ProgressCompleted))
                        {
                            mergeB1.Enabled = true;
                            mergeB2.Enabled = true;
                            if(IsMerged(mergeB1) && IsMerged(mergeB2) && connW.Status == MyShipConnectorStatus.Connected)
                            {
                                if(connOrigin.Status == MyShipConnectorStatus.Connected)
                                {
                                    state.State = RigState.Docked;
                                }
                                else
                                {
                                    state.State = RigState.RetractingA;
                                }
                            }
                        }
                        else
                        {
                            mergeB1.Enabled = false;
                            mergeB2.Enabled = false;
                        }
                    }
                    else
                    {
                        state.StatusText = "Retracting B (Waiting for merge)";
                    }
                    state.Options.InsertRange(0, new[] { ArgReverse });
                    break;
            }

            if(state.State != state.StateStartedState)
            {
                state.StateStarted = DateTime.Now;
                state.StateStartedState = state.State;
            }
        }

        void SetGearsLocked(IMyBlockGroup group, bool locked)
        {
            List<IMyLandingGear> gears = new List<IMyLandingGear>();
            group.GetBlocksOfType(gears);

            for(int i = 0; i < gears.Count; ++i)
            {
                if (locked)
                    gears[i].Lock();
                else
                    gears[i].Unlock();
            }
        }

        void SetEnabled(IMyBlockGroup blockGroup, bool enabled)
        {
            var blocks = new List<IMyFunctionalBlock>();
            blockGroup.GetBlocksOfType(blocks);
            for(int i = 0; i < blocks.Count; ++i)
            {
                blocks[i].Enabled = enabled;
            }
        }

        void SetEnabled(bool enabled, params IMyBlockGroup[] groups)
        {
            for(int i = 0; i < groups.Length; ++i)
            {
                SetEnabled(groups[i], enabled);
            }
        }

        void WriteLogs()
        {
            Echo(_log.ToString());
            var panel = GridTerminalSystem.GetBlockWithName(LogPanelName) as IMyTextPanel;
            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
            }
        }

        bool IsMerged(IMyShipMergeBlock mrg1)
        {
            if (mrg1 == null) return false;

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

        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}