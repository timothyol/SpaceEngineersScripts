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

namespace SpaceEngineersScripting.BoreWalker
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


        /* Operating the BoreWalker

(The bars should be set up to make this easier, but that 
almost never works for me so I will assume they are lost)

Blueprint is in the "Parked" state

To Begin:

1. Extend groups Pistons A and Pistons B. They should all have their max lengh set to 1.8. 
    Once extended, all of the landing gears should be yellow and ready to lock.
2. Lock Landing Gears A and Landing Gears B. Make sure AutoLock is disabled. 
2. Start the drills by toggling the Drillz group on
3. Start the front rotor. 3 rpm is what I use. Might have to slow it down to work in MP. 
4. With the rotor spinning, the drills on, with all eight pistons extended and the gears locked to the frame, start the timer block. 
    Status info should show up on the LCD panel.

Driving manually:

Important note: NEVER let all of the landing gears become unlocked at once. 
You will lose alignment and it will be super painful to get it back into place. 
If you're using a button or a timer block, make sure all landing gears are locked 
before unlocking either group.  

Driving manually (like if you were to back up) is done by unlocking a group of gears and extending/retracting Piston Middle to inch along. 

To go backward, with the drills being on the FRONT of the craft:

Lock front gears(Gears B), unlock back gears(Gears A), extend Piston Middle
Wait until desired extension is reached
Lock rear gears(Gears A), unlock front gears (Gears B), retract Piston middle
Repeat

Going foreward is the opposite, you should be able to figure it out. 
*/

        //------------------------------------------------------------------------------------------------------------------------------

        private const float ExtendingSpeed = .05f;
        private const float RetractingSpeed = -1.0f;

        private int _state = 1;
        private bool _stop = false;

        private TimeSpan _elapsed = new TimeSpan(0);
        private StringBuilder _log;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private void ToggleGear(List<IMyTerminalBlock> gears, bool state)
        {
            for (int i = 0; i < gears.Count; ++i)
            {
                var gear = gears[i] as IMyLandingGear;
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


        void Main(string argument, UpdateType updateSource)
        {
            if (argument == "start")
            {
                _stop = false;
            }
            else if (argument == "stop")
            {
                _stop = true;
            }

            var nextState = _state;

            _log = new StringBuilder("");
            _log.AppendLine("State is: " + _state.ToString());

            var piston = GridTerminalSystem.GetBlockWithName("Piston Middle") as IMyPistonBase;
            var panel = GridTerminalSystem.GetBlockWithName("LCD Status") as IMyTextPanel;

            _log.AppendLine("Piston is at " + piston.CurrentPosition.ToString());

            var gears_a = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("Landing Gear a", gears_a);
            var gears_b = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("Landing Gear b", gears_b);

            if (_stop)
            {
                ToggleGear(gears_a, true);
                ToggleGear(gears_b, true);
                return;
            }

            //States:
            //1. Extending (Gears A locked, gears B unlocked, piston extending)
            //2. Retracting (Gears B locked, Gears A unlocked, piston retracting)

            if (_state == 1)
            {
                if (piston.CurrentPosition == 10.0f)
                {
                    _state = 2;
                    ToggleGear(gears_b, true);
                    piston.Velocity = RetractingSpeed;
                }
                else
                {
                    piston.Velocity = ExtendingSpeed;
                    ToggleGear(gears_a, true);
                    if (AreLocked(gears_a))
                    {
                        ToggleGear(gears_b, false);
                    }
                }
            }
            else if (_state == 2)
            {
                if (piston.CurrentPosition == 0f)
                {
                    _state = 1;
                    ToggleGear(gears_a, true);
                    piston.Velocity = ExtendingSpeed;
                }
                else
                {
                    ToggleGear(gears_b, true);
                    piston.Velocity = RetractingSpeed;
                    if (AreLocked(gears_b))
                    {
                        ToggleGear(gears_a, false);
                    }
                }
            }

            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
            }
        }


            //    //Alter these to change the speed at which it attempts to drill. Retract should always be greater than extend time, just to be safe.
            //    var DrillTimeSeconds = 12;
            //    var ExtendTimeSeconds = 1;
            //    var RetractTimeSeconds = 3;

            //    _log = new StringBuilder("");
            //    _log.AppendLine("State is: " + _state.ToString());

            //    var piston = GridTerminalSystem.GetBlockWithName("Piston Middle") as IMyPistonBase;
            //    var panel = GridTerminalSystem.GetBlockWithName("LCD Status") as IMyTextPanel;

            //    var gears_a = new List<IMyTerminalBlock>();
            //    GridTerminalSystem.SearchBlocksOfName("Landing Gear a", gears_a);
            //    var gears_b = new List<IMyTerminalBlock>();
            //    GridTerminalSystem.SearchBlocksOfName("Landing Gear b", gears_b);

            //    //states: 
            //    //1. Extending (gears a locked, gears b unlocked, piston extending) 
            //    //2. Waiting for drill to extend 
            //    //3. Retracting (gears a unlocked, gears b locked, piston retracting) 
            //    //4. Waiting for Retracting 
            //    //5. Giving the drill some time 

            //    if (_state == 1)
            //    {
            //        var ga = gears_a[0] as IMyLandingGear;
            //        var gb = gears_b[0] as IMyLandingGear;

            //        if (IsReadyToLock(ga) || IsReadyToLock(gb))
            //        {
            //            //Only unlock gears if both are locked at the beginning of the frame  
            //            ToggleGear(gears_a, true);
            //            ToggleGear(gears_b, true);
            //            _log.AppendLine("Waiting for Gears to Lock");
            //        }
            //        else
            //        {
            //            ToggleGear(gears_b, false);
            //            piston.Extend();

            //            _elapsed = new TimeSpan(0);
            //            nextState = 2;
            //            _log.AppendLine("Extending Drill Head");
            //        }
            //    }

            //    if (_state == 2)
            //    {
            //        _elapsed += Runtime.TimeSinceLastRun;
            //        if (_elapsed.TotalSeconds >= ExtendTimeSeconds)
            //            nextState = 3;

            //        _log.AppendLine("Extending Drill Head");
            //    }

            //    if (_state == 3)
            //    {
            //        var ga = gears_a[0] as IMyLandingGear;
            //        var gb = gears_b[0] as IMyLandingGear;

            //        if (IsReadyToLock(ga) || IsReadyToLock(gb))
            //        {
            //            //Only unlock gears if both are locked at the beginning of the frame  
            //            ToggleGear(gears_a, true);
            //            ToggleGear(gears_b, true);
            //            _log.AppendLine("Waiting for Gears to Lock");
            //        }
            //        else
            //        {
            //            ToggleGear(gears_a, false);
            //            piston.Retract();

            //            _elapsed = new TimeSpan(0);
            //            _log.AppendLine("Retracting Tail Gears");
            //            nextState = 4;
            //        }
            //    }

            //    if (_state == 4)
            //    {
            //        _elapsed += Runtime.TimeSinceLastRun;
            //        if (_elapsed.TotalSeconds >= RetractTimeSeconds)
            //        {
            //            ToggleGear(gears_a, true);
            //            nextState = 5;
            //        }
            //        _log.AppendLine("Retracting Tail Gears");
            //    }

            //    if (_state == 5)
            //    {
            //        _elapsed += Runtime.TimeSinceLastRun;
            //        if (_elapsed.TotalSeconds >= DrillTimeSeconds)
            //        {
            //            nextState = 1;
            //        }
            //        _log.AppendLine("Waiting for Drill");
            //    }

            //    panel.WritePublicText(_log.ToString(), false);
            //    _state = nextState;
            // }


            /***************To above this comment into space engineers**********
            ********************************************************************/
    }
}