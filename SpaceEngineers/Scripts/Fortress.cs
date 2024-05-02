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

namespace SpaceEngineers.Scripts.Fortress
{
    public class Program : MyGridProgram
    {

        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Grid.cs
        // import:Lib\WeaponController.cs

        private const string GROUP_PREFIX_TORPEDO = "TORPEDO";

        private readonly RuntimeTracker tracker;
        private readonly IMyTextSurface trackerLcd;

        private readonly IMyCameraBlock camera;

        private readonly Grid grid;
        private readonly WeaponController weapons;

        private bool sameGrid<T>(T b) where T : IMyTerminalBlock
        {
            return b.CubeGrid == Me.CubeGrid;
        }

        public Program()
        {
            tracker = new RuntimeTracker(this);
            trackerLcd = Me.GetSurface(1);
            trackerLcd.ContentType = ContentType.TEXT_AND_IMAGE;

            grid = new Grid(GridTerminalSystem);

            camera = grid.GetCamera("CAMERA");

            var gyros = grid.GetBlocksOfType<IMyGyro>(sameGrid);
            var cockpit = grid.GetByFilterOrAny<IMyCockpit>(sameGrid);
            var cameras = grid.GetBlocksOfType<IMyCameraBlock>();
            var turrets = grid.GetBlocksOfType<IMyLargeTurretBase>();
            var railguns = grid.GetLargeRailguns();
            var antennas = grid.GetBlocksOfType<IMyRadioAntenna>();
            var lcdTargets = cockpit.GetSurface(0);
            var lcdTorpedos = cockpit.GetSurface(1);
            var lcdSystem = cockpit.GetSurface(2);
            var sound = grid.GetSound("SOUND", "SoundBlockEnemyDetected");
            var soundEnemyLock = grid.GetSound("LOCK_SOUND", "SoundBlockAlert1");

            var hud = grid.GetBlocksOfType<IMyTextPanel>(p => p.CustomName.StartsWith("HUD"));


            weapons = new WeaponController(
                gyros,
                cockpit,
                cameras,
                turrets,
                railguns,
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
                case "filter":
                    weapons.ToggleFilter();
                    break;
                case "prev":
                    weapons.PrevTarget();
                    break;
                case "next":
                    weapons.NextTarget();
                    break;
                case "lock":
                    weapons.Scan(camera);
                    break;
                case "reload":
                    var groups = grid.GetBlockGroups(GROUP_PREFIX_TORPEDO);
                    weapons.Reload(groups);
                    break;
                case "start":
                    weapons.Launch();
                    break;
            }

            weapons.UpdateNext(argument, updateSource);

            //switch (argument)
            //{
            //    case "init":
            //        tt.UpdateCamArray();
            //        break;
            //    case "reset":
            //        tt.Clear();

            //        break;
            //}

            tracker.AddInstructions();
            trackerLcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
