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

namespace SpaceEngineersScripting.TreadMagnets
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

        private const float RadianRatio = 0.0174533f;

        private StringBuilder _log;
        private StringBuilder _errorLog;
        private bool _paused = false;
        //private bool _hasPaused = false;

        private Dictionary<string, IMyMotorStator> _hinges = null;
        private string _prefix = null;
        private int maxTreads = 23;

        public void Main(string argument, UpdateType updateSource)
        {
            _errorLog = new StringBuilder("");
            _log = new StringBuilder("");

            if (argument != null && argument.StartsWith("start"))
            {
                var args = argument.Split(' ');
                if (args.Length > 1) _prefix = args[1];
            }
            else if (argument == "stop")
            {
                _prefix = null;
            }
            else if (argument == "pause")
            {
                _paused = !_paused;
            }

            if(_paused)
            {
                _log.AppendLine("Paused");
                WriteLogs();
                return;
            }

            if (_prefix != null)
            {
                if (_hinges == null)
                {
                    GetHinges(_prefix);
                }

                try
                {
                    GrabGround();
                }
                catch (Exception e)
                {
                    _log.AppendLine("Error grabbing ground. " + e.Message);
                }
            }

            _log.AppendLine("End Tick");
            WriteLogs();
        }

        void GrabGround()
        {
            var previousHinges = GetHinges(maxTreads - 1, _prefix);
            var currentHinges = GetHinges(0, _prefix);
            var nextHinges = GetHinges(1, _prefix);

            if (previousHinges[0] == null) throw new Exception("PrevHinge is null");
            if (currentHinges[0] == null) throw new Exception("currentHinges is null");
            if (nextHinges[0] == null) throw new Exception("nextHinges is null");

            var minAngleToLock = 5.0f; // degrees
            var minAngleRads = minAngleToLock * RadianRatio;

            for (int i = 0; i < maxTreads; ++i)
            {
                var magPlate = GetMagPlate(i);
                _log.Append(currentHinges[0].CustomName + " angle: " + currentHinges[0].Angle);
                if (previousHinges[0].Angle < minAngleRads
                    && currentHinges[0].Angle < minAngleRads
                    && nextHinges[0].Angle < minAngleRads)
                {
                    _log.Append(" (Would lock)");
                    magPlate.Lock();
                }
                else
                {
                    magPlate.Unlock();
                }

                _log.AppendLine();

                previousHinges = currentHinges;
                currentHinges = nextHinges;
                nextHinges = GetHinges((i + 2) % (maxTreads - 1), _prefix);
            }
        }

        void GetHinges(string prefix)
        {
            _hinges = new Dictionary<string, IMyMotorStator>();

            var allHinges = new List<IMyMotorStator>();
            GridTerminalSystem.GetBlocksOfType(allHinges);

            for (int i = 0; i < allHinges.Count; ++i)
            {
                var hinge = allHinges[i];
                if (hinge.CustomName.StartsWith(prefix))
                {
                    _hinges.Add(hinge.CustomName, hinge);
                }
            }
        }

        IMyLandingGear GetMagPlate(int num)
        {
            var name = GetMagPlateName(num, _prefix);

            var magPlate = GridTerminalSystem.GetBlockWithName(name) as IMyLandingGear;
            return magPlate;
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

        string GetMagPlateName(int num, string prefix)
        {
            return prefix + " " + "MagPlate " + (num < 10 ? "0" : "") + num;
        }

        void WriteLogs()
        {
            Echo(_log.ToString());
            var panel = GridTerminalSystem.GetBlockWithName("LCD Panel TreadMagnet Status") as IMyTextPanel;
            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
            }

            Echo(_errorLog.ToString());
            var errorPanel = GridTerminalSystem.GetBlockWithName("LCD Panel TreadMagnet Errors") as IMyTextPanel;
            if (errorPanel != null)
            {
                errorPanel.WriteText(_errorLog.ToString(), false);
            }
        }

        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}