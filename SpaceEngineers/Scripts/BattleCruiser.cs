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
        // import:RotorTurret.cs

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        readonly List<RotorTurret> turrets;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("TURRET"));

            turrets = groups.Select(gr => new RotorTurret(gr) { 
                Enabled = true, 
                ShootingEnabled = true,
                MinElevationRad = -Math.PI / 12,
            }).ToList();

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // track runtime
            tracker.AddRuntime();

            switch (argument.ToLower())
            {
                case "enable":
                    turrets.ForEach(t => t.Enabled = true);
                    break;
                case "disable":
                    turrets.ForEach(t => t.Enabled = false);
                    break;
            }

            // update all
            turrets.ForEach(t => t.Update());

            // track instructions
            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
