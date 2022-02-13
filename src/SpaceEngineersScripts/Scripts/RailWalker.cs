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

namespace SpaceEngineersScripting.Railwalker
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

        private const float ExtendingSpeed = .4f;
        private const float RetractingSpeed = -.7f;

        private int _state = 1;
        private bool _stop = false;

        private StringBuilder _log;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main(string argument, UpdateType updateSource)
        {
            //States:
            // Extending (Merged Merged, Back Connector connected, Front connector unlocked, piston extending)
            // Retracting (Merge Unmerged, Back connector unlocked, front locked, piston retracting)

            _log = new StringBuilder("");
            _log.AppendLine("State is: " + _state.ToString());

            var piston = GridTerminalSystem.GetBlockWithName("Piston Railwalker") as IMyPistonBase;
            var panel = GridTerminalSystem.GetBlockWithName("LCD Panel Railwalker Status") as IMyTextPanel;
            var merge = GridTerminalSystem.GetBlockWithName("Merge Block Railwalker") as IMyShipMergeBlock;
            var connectorBack = GridTerminalSystem.GetBlockWithName("Connector Railwalker Back") as IMyShipConnector;
            var connectorFront = GridTerminalSystem.GetBlockWithName("Connector Railwalker Front") as IMyShipConnector;

            if (connectorBack == null)
            {
                _log.AppendLine("Cannot find connector. :(");
                if (panel != null)
                {
                    panel.WriteText(_log.ToString(), false);
                }
                return;
            }
            if (connectorFront == null)
            {
                _log.AppendLine("Cannot find connector2. :(");
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
                piston.Velocity = 0f;
                return;
            }

            if (_state == 1)
            {
                // Extending - Moving front section forward
                if (piston.CurrentPosition == 10.0f)
                {
                    // Fully extended; attempt to connect front connector
                    connectorFront.Connect();
                    if (connectorFront.Status == MyShipConnectorStatus.Connected)
                    {
                        //Fully extended, front connector connected; disconnect the back stuff, retract piston, go to state 2
                        _state = 2;
                        merge.Enabled = false;
                        connectorBack.Disconnect();
                        piston.Velocity = RetractingSpeed;
                    }
                }
                else
                {
                    // Extending, keep extending. keep Merge block and back connector connected.
                    connectorBack.Connect();
                    if (connectorBack.Status == MyShipConnectorStatus.Connected)
                    {
                        connectorFront.Disconnect();
                    }
                    piston.Velocity = ExtendingSpeed;
                    merge.Enabled = true;
                }
            }
            else if (_state == 2)
            {
                // Retracting - Moving rear section forward
                if (piston.CurrentPosition == 0.0f && !IsMerged(merge))
                {
                    // Sometimes it doesn't merge, toggling the connector block will work sometimes?
                    if (connectorFront.Status == MyShipConnectorStatus.Connected)
                    {
                        if(connectorBack.Status == MyShipConnectorStatus.Connected)
                            connectorBack.Disconnect();
                        else
                            connectorBack.Connect();
                    }
                }
                else if (IsMerged(merge))
                {
                    // Merged, attempt to transition to state 1
                    connectorBack.Connect();
                    if (connectorBack.Status == MyShipConnectorStatus.Connected)
                    {
                        // Back section fully connected; disconnect front connector and extend
                        _state = 1;
                        connectorFront.Disconnect();
                        piston.Velocity = ExtendingSpeed;
                    }
                }
                else
                {
                    // Still retracting, keep retracting
                    connectorFront.Connect();
                    connectorBack.Disconnect();
                    piston.Velocity = RetractingSpeed;
                    if (piston.CurrentPosition <= 7.5f)
                    {
                        merge.Enabled = true;
                    }
                    else
                    {
                        merge.Enabled = false;
                    }
                }
            }

            if (panel != null)
            {
                panel.WriteText(_log.ToString(), false);
            }
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


        /***************To above this comment into space engineers**********
        ********************************************************************/
    }
}