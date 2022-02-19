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

namespace SpaceEngineersScripting.TreadWrapper
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

        private string _prefix = "TML";

        void Main(string argument, UpdateType updateSource)
        {
            // Detach 0, 27
            // Old: (subract 9)
            // 15 17 21 26 30 32
            // 15, 32: 90 degrees
            // 17, 30: 65 degrees
            // 21 26: 25 degrees
            // torque: 3000000


            if (argument != null && argument != "")
            {
                _prefix = argument;
            }

            //var hinges6 = GetHinges(6);
            //var hinges8 = GetHinges(8);
            //var hinges12 = GetHinges(12);
            //var hinges17 = GetHinges(17);
            //var hinges21 = GetHinges(21);
            //var hinges23 = GetHinges(23);

            //DetachHinges(10);
            DetachHinges(28);

            SetHingesTo(GetHinges(5), 90.0f);
            SetHingesTo(GetHinges(7), 65.0f);
            SetHingesTo(GetHinges(11), 25.0f);
            SetHingesTo(GetHinges(16), 25.0f);
            SetHingesTo(GetHinges(20), 65.0f);
            SetHingesTo(GetHinges(22), 90.0f);
        }

        void DetachHinges(int num)
        {
            var hinges = GetHinges(num);

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

        IMyMotorStator[] GetHinges(int num)
        {
            var hingeName = GetLinkHingeName(num);

            var hinge1 = GridTerminalSystem.GetBlockWithName(hingeName + "A") as IMyMotorStator;
            var hinge2 = GridTerminalSystem.GetBlockWithName(hingeName + "B") as IMyMotorStator;

            return new[] { hinge1, hinge2 };
        }

        string GetLinkHingeName(int num)
        {
            return _prefix + " " + "Hinge Link " + (num < 10 ? "0" : "") + num;
        }


        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}