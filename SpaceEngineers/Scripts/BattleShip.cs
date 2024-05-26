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
        // import:Lib\WeaponController.cs

        private const string GROUP_PREFIX_TORPEDO = "ws_torpedo";

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;

        readonly Grid grid;
        readonly GravityDrive gdrive;
        readonly WeaponController weapons;

        readonly IMyCameraBlock cameraTop;
        readonly IMyCameraBlock cameraBottom;

        private bool iii = false;

        private bool sameGrid<T>(T b) where T : IMyTerminalBlock
        {
            return b.CubeGrid == Me.CubeGrid;
        }

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            grid = new Grid(GridTerminalSystem);

            cameraTop = grid.GetBlockWithName<IMyCameraBlock>("ws_cam_t");
            cameraBottom = grid.GetBlockWithName<IMyCameraBlock>("ws_cam_b");

            var cockpit = grid.GetBlockWithName<IMyCockpit>("ws_cockpit");
            var cameras = grid.GetBlocksOfType<IMyCameraBlock>();
            var antennas = grid.GetBlocksOfType<IMyRadioAntenna>();

            var railguns = grid.GetLargeRailguns(sameGrid);
            var artillery = grid.GetArtillery(sameGrid);
            var turrets = grid.GetArtilleryTurrets(sameGrid);

            var hud = grid.GetBlocksOfType<IMyTextPanel>(p => p.CustomName.StartsWith("ws_hud"));
            var lcdTorpedos = grid.GetBlockWithName<IMyTextPanel>("ws_lcd_1");
            var lcdTargets = grid.GetBlockWithName<IMyTextPanel>("ws_lcd_2");
            var lcdSystem = grid.GetBlockWithName<IMyTextPanel>("ws_lcd_3");
            var sound = grid.GetSound("ws_sound_1", "SoundBlockEnemyDetected");
            var soundEnemyLock = grid.GetSound("ws_sound_2", "SoundBlockAlert1");

            var group = GridTerminalSystem.GetBlockGroupWithName("ws_gdrive");

            var gyros = new List<IMyGyro>();
            group.GetBlocksOfType(gyros);

            gdrive = new GravityDrive(cockpit, group);
            weapons = new WeaponController(
                gyros.ToArray(),
                cockpit,
                cameras,
                turrets,
                railguns,
                artillery,
                hud,
                lcdTargets,
                lcdTorpedos,
                lcdSystem,
                IGC,
                antennas,
                sound,
                soundEnemyLock
              );

            weapons.OnError += HandleError;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        private void HandleError(Exception ex)
        {
            Echo(ex.ToString());
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            switch (argument)
            {
                // gdrive
                case "gd-on":
                    gdrive.Enabled = true;
                    break;
                case "gd-off":
                    gdrive.Enabled = false;
                    break;
                case "gd-info":
                    UpdateGdInfo();
                    break;

                // weapons
                case "aim-set":
                    weapons.Aim();
                    break;
                case "aim-clear":
                    weapons.ClearAimBotTarget();
                    break;
                case "filter":
                    weapons.ToggleFilter();
                    break;
                case "mode":
                    weapons.ToggleFiringMode();
                    break;
                case "lock-top":
                    weapons.Scan(cameraTop);
                    break;
                case "lock-bottom":
                    weapons.Scan(cameraBottom);
                    break;
                case "reload":
                    var groups = grid.GetBlockGroups(GROUP_PREFIX_TORPEDO);
                    weapons.Reload(groups);
                    break;
                case "start":
                    weapons.Launch();
                    break;
                case "set-enemy-lock":
                    weapons.SetEnemyLock();
                    break;
                case "clear-enemy-lock":
                    weapons.ClearEnemyLock();
                    break;

                default:

                    if (iii)
                    {
                        gdrive.Update(!weapons.AimbotEnabled);
                    }
                    else
                    {
                        weapons.UpdateNext(argument, updateSource);
                    }

                    iii = !iii;

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
        private void UpdateGdInfo()
        {
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
