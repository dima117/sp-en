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
using SpaceEngineers.Lib;

namespace SpaceEngineers.Scripts.AimBot
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\DirectionController2.cs

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;

        readonly DirectionController2 dc;
        IMyTextSurface lcdTarget;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            var list2 = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(list2);

            var gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyros);

            var control = list2.FirstOrDefault(x => x.CubeGrid.EntityId == Me.CubeGrid.EntityId);
            lcdTarget = control?.GetSurface(0);

            dc = new DirectionController2(control, gyros);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();


            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
