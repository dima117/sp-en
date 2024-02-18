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

namespace SpaceEngineers.Scripts.BattleShip
{
    public class Program : MyGridProgram
    {
        #region Copy

        // боевой корабль для PvP

        // import:RuntimeTracker.cs
        // import:Lib\Grid.cs
        // import:Lib\GravityDrive.cs

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;

        readonly Grid grid;
        readonly GravityDrive gdrive;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            grid = new Grid(GridTerminalSystem);

            var cockpit = grid.GetBlockWithName<IMyCockpit>("cockpit_main");
            var group = GridTerminalSystem.GetBlockGroupWithName("ws_gdrive");
            gdrive = new GravityDrive(cockpit, group);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            switch (argument)
            {
                case "gd-on":
                    gdrive.Enabled = true;
                    break;
                case "gd-off":
                    gdrive.Enabled = false;
                    break;
                case "gd-info":
                    UpdateGdInfo();
                    break;
                default:
                    gdrive.Update();
                    break;
            }

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        private string Format(double p, double v, string textP, string textN)
        {
            double r = v - p;
            string label = r > 0 ? textP : textN;

            return $"{label}: {r:0.0}";
        }
        private string FormatGPS(Vector3D point, string label)
        {
            return $"GPS:{label}:{point.X:0.00}:{point.Y:0.00}:{point.Z:0.00}:#FF89F175:";
        }
        private void UpdateGdInfo() {
            var x = gdrive.CalculateCenterOfMass();
            var p = x.Physical.Local;
            var v = x.Virtual.Local;

            var sb = new StringBuilder();

            sb.AppendLine(Format(p.Z, v.Z, "Fwd", "Bck"));
            sb.AppendLine(Format(p.X, v.X, "Rgt", "Lft"));
            sb.AppendLine(Format(p.Z, v.Z, "Top", "Btm"));

            Me.GetSurface(0).WriteText(sb);

            Me.CustomData =
                FormatGPS(x.Physical.World, "GD Center") + "\n" +
                FormatGPS(x.Virtual.World, "GD Virtual center");
        }

        #endregion
    }
}
