using System;
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

namespace SpaceEngineers.Lib
{
    #region Copy

    public class AITracker
    {
        readonly IMyOffensiveCombatBlock ai;
        readonly IMyFlightMovementBlock flight;

        public AITracker(IMyOffensiveCombatBlock ai, IMyFlightMovementBlock flight)
        {
            ai.Enabled = true;
            flight.Enabled = true;
            flight.FlightMode = FlightMode.OneWay;

            this.ai = ai;
            this.flight = flight;
        }

        public Vector3D? Current => flight.CurrentWaypoint == null ? null as Vector3D? : new Vector3D(flight.CurrentWaypoint.Matrix.GetRow(3));
    }

    #endregion
}
