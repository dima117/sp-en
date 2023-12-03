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
using SpaceEngineers2;

namespace SpaceEngineers.Scripts.BattleCruiser
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs
        // import:GravityDrive.cs

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        readonly GravityDrive drive;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            var control = GridTerminalSystem.GetBlockWithName("CONTROL") as IMyShipController;
            var group = GridTerminalSystem.GetBlockGroupWithName("GDRIVE");

            drive = new GravityDrive(control, group);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // track runtime
            tracker.AddRuntime();

            switch (argument)
            {
                case "on":
                    drive.Enabled = true;
                    break;
                case "off":
                    drive.Enabled = false;
                    break;
                default:
                    drive.Update();
                    break;
            }

            // track instructions
            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
