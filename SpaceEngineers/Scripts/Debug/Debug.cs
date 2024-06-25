using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Net;
using Sandbox.Game.Lights;

namespace SpaceEngineers.Scripts.Debug
{
    public class Program : MyGridProgram
    {
        #region Copy

        IMyTextPanel lcd;
        IMyOffensiveCombatBlock ai;
        IMyFlightMovementBlock flight;

        public Program()
        {

            ai = GridTerminalSystem.GetBlockWithName("ai") as IMyOffensiveCombatBlock;
            lcd = GridTerminalSystem.GetBlockWithName("lcd") as IMyTextPanel;
            flight = GridTerminalSystem.GetBlockWithName("flight") as IMyFlightMovementBlock;

            lcd.WriteText(ai.DetailedInfo);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var wp = new List<IMyAutopilotWaypoint>();
            flight.GetWaypoints(wp);

            lcd.WriteText(ai.DetailedInfo + "-----\n");
            lcd.WriteText(wp.Count.ToString() + "-----\n", true);
            lcd.WriteText(FormatGPS(flight.CurrentWaypoint.Matrix.GetRow(3), "TARGET"), true);

        }

        private string FormatGPS(Vector4D point, string label)
        {
            return $"GPS:{label}:{point.X:0.00}:{point.Y:0.00}:{point.Z:0.00}:#FF89F175:";
        }

        #endregion
    }
}


/*

 */