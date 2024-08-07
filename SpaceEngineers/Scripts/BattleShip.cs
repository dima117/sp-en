﻿using System;
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

        // import:Lib\Grid.cs
        // import:Lib\LocalTime.cs
        // import:Lib\GravityDrive.cs
        // import:Lib\ShipDirectionController.cs
        // import:Lib\WeaponController.cs
        // import:Lib\HUD.cs

        private const string TORPEDO_GROUP_PREFIX = "ws_torpedo";
        private const int TORPEDO_INITIAL_DISTANCE = 4500;

        readonly Grid grid;
        readonly LocalTime localTime;
        readonly GravityDrive gdrive;
        readonly ShipDirectionController directionController;
        readonly WeaponController weapons;
        readonly HUD hud;

        readonly IMyCameraBlock cameraTop;
        readonly IMyCameraBlock cameraBottom;
        readonly IMyShipWelder[] welders;
        readonly IMyCockpit cockpit;

        private const int TICK_COUNT = 12;
        private const double AVG_RUNTIME_LIMIT = 0.24;
        private int tick = 0;

        private bool sameGrid<T>(T b) where T : IMyTerminalBlock
        {
            return b.CubeGrid == Me.CubeGrid;
        }

        public Program()
        {
            grid = new Grid(GridTerminalSystem);
            localTime = new LocalTime(Runtime);

            welders = grid.GetBlocksOfType<IMyShipWelder>(sameGrid);

            cockpit = grid.GetBlockWithName<IMyCockpit>("ws_cockpit");
            cameraTop = grid.GetCamera("ws_cam_t");
            cameraBottom = grid.GetCamera("ws_cam_b");

            var cameras = grid.GetBlocksOfType<IMyCameraBlock>(cam => !cam.CustomName.StartsWith("ws_cam"));

            var beacon = grid.GetBlockWithName<IMyBeacon>("ws_beacon");

            var railguns = grid.GetLargeRailguns(sameGrid);
            var artillery = grid.GetArtillery(sameGrid);
            var turrets = grid.GetArtilleryTurrets(sameGrid);

            var lcdHUD = grid.GetBlocksOfType<IMyTextPanel>(p => p.CustomName.StartsWith("ws_hud"));
            var lcdSystem = cockpit.GetSurface(1);
            var lcdTorpedos = cockpit.GetSurface(2);
            var sound = grid.GetSound("ws_sound_1", "SoundBlockEnemyDetected");
            var soundEnemyLock = grid.GetSound("ws_sound_2", "SoundBlockAlert1");

            var ajs = grid.GetBlockWithName<IMyMotorStator>("ws_ajs");
            var ai = grid.GetByFilterOrAny<IMyOffensiveCombatBlock>(b => b.CubeGrid == ajs.TopGrid);
            var flight = grid.GetByFilterOrAny<IMyFlightMovementBlock>(b => b.CubeGrid == ajs.TopGrid);

            var group = GridTerminalSystem.GetBlockGroupWithName("ws_gdrive");

            var gyros = new List<IMyGyro>();
            group.GetBlocksOfType(gyros);

            hud = new HUD(lcdHUD, beacon, GetHudState);
            gdrive = new GravityDrive(cockpit, group);

            directionController = new ShipDirectionController(cockpit, gyros);
            directionController.OnReadyToShot += OnReadyToShot;

            weapons = new WeaponController(
                localTime,
                cameras,
                turrets,
                railguns,
                artillery,
                lcdTorpedos,
                lcdSystem,
                sound,
                soundEnemyLock,
                ai, flight
              );

            weapons.OnError += HandleError;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        private void OnReadyToShot()
        {
            weapons.TryFire();
        }

        private HudState GetHudState(DateTime now)
        {
            return new HudState
            {
                AiTarget = weapons.CurrentAiTarget,
                Aimbot = weapons.Aimbot,
                EnemyLock = weapons.EnemyLock,
                Target = weapons.CurrentTarget,
                Weapon = weapons.GetState(now),
            };
        }

        private void HandleError(Exception ex)
        {
            Echo(ex.ToString());
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var now = localTime.Update(updateSource);

            if ((updateSource & UpdateType.Update1) == UpdateType.Update1)
            {
                if (localTime.Avg < AVG_RUNTIME_LIMIT)
                {
                    tick = (tick + 1) % TICK_COUNT;

                    switch (tick)
                    {
                        case 2:
                        case 4:
                        case 8:
                            weapons.UpdateNext();
                            break;
                        case 1:
                        case 7:
                            gdrive.Update();
                            break;
                        case 5:
                        case 11:
                            hud.Update(now, cockpit.GetPosition());
                            break;
                        case 0:
                        case 3:
                        case 6:
                        case 9:
                            if (weapons.Aimbot.HasValue && weapons.CurrentTarget != null)
                            {
                                directionController.Aim(weapons.CurrentTarget, weapons.Aimbot.Value, now);
                            }
                            else
                            {
                                directionController.Update();
                            }
                            break;
                    }
                }
            }
            else
            {
                switch (argument)
                {
                    // weapons
                    case "aimbot":
                        weapons.ToggleAimbot();
                        break;
                    case "mode":
                        weapons.ToggleFiringMode();
                        break;
                    case "lock-ai":
                        weapons.Scan(weapons.CurrentAiTarget);
                        break;
                    case "lock-top":
                        weapons.Scan(cameraTop);
                        break;
                    case "lock-bottom":
                        weapons.Scan(cameraBottom);
                        break;
                    case "reload":
                        var groups = grid.GetBlockGroups(TORPEDO_GROUP_PREFIX);
                        weapons.Reload(groups);
                        break;
                    case "start":
                        var initialTarget = cockpit.CenterOfMass + cockpit.WorldMatrix.Forward * TORPEDO_INITIAL_DISTANCE;
                        weapons.Launch(initialTarget);
                        break;
                    case "set-enemy-lock":
                        weapons.SetEnemyLock();
                        break;
                    case "clear-enemy-lock":
                        weapons.ClearEnemyLock();
                        break;
                }
            }
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
