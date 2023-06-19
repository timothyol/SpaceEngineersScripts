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

namespace SpaceEngineersScripting
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

        // -1, 0, 1
        private float direction = 0f;
        private const float Velocity = 0.4f;
        //private const float MaxAngle = 63.5f;
        //private const float MinAngle = 3.5f;
        private const float RadianRatio = 0.0174533f;

        private StringBuilder _log;
        private const string LogPanelName = "LCD Panel SmoothHinge";

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        void Main(string argument, UpdateType updateSource)
        {
            var eHinge1 = GridTerminalSystem.GetBlockWithName("Hinge 3x3 E1") as IMyMotorStator;
            var eHinge2 = GridTerminalSystem.GetBlockWithName("Hinge 3x3 E2") as IMyMotorStator;
            var rHinge1 = GridTerminalSystem.GetBlockWithName("Hinge 3x3 R1") as IMyMotorStator;
            var rHinge2 = GridTerminalSystem.GetBlockWithName("Hinge 3x3 R2") as IMyMotorStator;

            _log = new StringBuilder("");


            // root hinges: 3.5 - 63.5
            // elbow hinges: -86.5000 - -26.5000

            if (argument != "")
            {
                switch (argument) 
                {
                    case "extend":
                        direction = 1f;
                        break;
                    case "retract":
                        direction = -1f;
                        break;
                    case "stop":
                        direction = 0f;
                        break;
                }                
            }

            if (direction == 0f)
            {
                eHinge1.TargetVelocityRPM = 0;
                eHinge2.TargetVelocityRPM = 0;
                rHinge1.TargetVelocityRPM = 0;
                rHinge2.TargetVelocityRPM = 0;
                return;
            }

            var minAngle = Math.Min((eHinge1.Angle / RadianRatio) + 90f, (eHinge2.Angle / RadianRatio) + 90f);
            minAngle = Math.Min(minAngle, rHinge1.Angle / RadianRatio);
            minAngle = Math.Min(minAngle, rHinge2.Angle / RadianRatio);

            var maxAngle = Math.Max((eHinge1.Angle / RadianRatio) + 90f, (eHinge2.Angle / RadianRatio) + 90f);
            maxAngle = Math.Max(maxAngle, rHinge1.Angle / RadianRatio);
            maxAngle = Math.Max(maxAngle, rHinge2.Angle / RadianRatio);

            var targetVelocity = Velocity * direction;

            _log.AppendLine("Min angle:" + minAngle);
            _log.AppendLine("Max angle:" + maxAngle);
            _log.AppendLine("Direction:" + direction);

            if ((maxAngle - minAngle) > 1.0f)
            {
                // Stuff is fucked, turn off hinges that have gotten ahead
                RestrictHinge(eHinge1, 90f, minAngle, maxAngle, targetVelocity);
                RestrictHinge(eHinge2, 90f, minAngle, maxAngle, targetVelocity);
                RestrictHinge(rHinge1, 0, minAngle, maxAngle, targetVelocity);
                RestrictHinge(rHinge2, 0, minAngle, maxAngle, targetVelocity);
            }
            else
            {                
                eHinge1.TargetVelocityRPM = targetVelocity;
                eHinge2.TargetVelocityRPM = targetVelocity;
                rHinge1.TargetVelocityRPM = targetVelocity;
                rHinge2.TargetVelocityRPM = targetVelocity;
            }

            WriteLogs();
        }

        void RestrictHinge(IMyMotorStator hinge, float offset, float minAngle, float maxAngle, float targetVelocity)
        {
            var hingeAngle = (hinge.Angle / RadianRatio) + offset;
            var display = hinge.CustomName + "(" + hingeAngle + ")";

            var halfDiff = (maxAngle - minAngle) / 2.0f;

            if (direction == 1f)
            {
                if (hingeAngle > maxAngle - halfDiff)
                {
                    hinge.TargetVelocityRPM = -targetVelocity;
                    _log.AppendLine("Rev: " + display);
                }
                else
                {
                    hinge.TargetVelocityRPM = targetVelocity;
                    _log.AppendLine("On: " + display);
                }
            }
            else if (direction == -1f)
            {
                if (hingeAngle < minAngle + halfDiff)
                {
                    hinge.TargetVelocityRPM = -targetVelocity;
                    _log.AppendLine("Rev: " + display);
                }
                else
                {
                    hinge.TargetVelocityRPM = targetVelocity;
                    _log.AppendLine("On: " + display);
                }
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

        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}