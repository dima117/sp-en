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
using Sandbox.Game.Entities;

namespace SpaceEngineers.Scripts.Fortress
{
    public class Program : MyGridProgram
    {

        #region Copy

        // import:Lib\Grid.cs
        // import:Lib\LocalTime.cs
        // import:Lib\WeaponController.cs

        private const string TORPEDO_GROUP_PREFIX = "ws_torpedo";
        private const int TORPEDO_INITIAL_DISTANCE = 4500;


        private readonly IMyCameraBlock camera;
        private readonly IMyCockpit cockpit;

        private readonly Grid grid;
        private readonly LocalTime localTime;
        private readonly WeaponController weapons;

        private bool sameGrid<T>(T b) where T : IMyTerminalBlock
        {
            return b.CubeGrid == Me.CubeGrid;
        }

        public Program()
        {
            grid = new Grid(GridTerminalSystem);
            localTime = new LocalTime(Runtime);

            camera = grid.GetCamera("CAMERA");
            cockpit = grid.GetByFilterOrAny<IMyCockpit>(sameGrid);

            var gyros = grid.GetBlocksOfType<IMyGyro>(sameGrid);
            var cameras = grid.GetBlocksOfType<IMyCameraBlock>();
            var turrets = grid.GetArtilleryTurrets();

            var railguns = grid.GetLargeRailguns();
            var artillery = grid.GetArtillery();
            var beacon = grid.GetBlockWithName<IMyBeacon>("ws_beacon");
            var lcdTargets = cockpit.GetSurface(0);
            var lcdTorpedos = cockpit.GetSurface(1);
            var lcdSystem = cockpit.GetSurface(2);
            var sound = grid.GetSound("SOUND", "SoundBlockEnemyDetected");
            var soundEnemyLock = grid.GetSound("LOCK_SOUND", "SoundBlockAlert1");

            var hud = grid.GetBlocksOfType<IMyTextPanel>(p => p.CustomName.StartsWith("HUD"));


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
                ai: null,
                flight: null
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
            var now = localTime.Update(updateSource);

            switch (argument)
            {
                case "lock":
                    weapons.Scan(camera);
                    break;
                case "reload":
                    var groups = grid.GetBlockGroups(TORPEDO_GROUP_PREFIX);
                    weapons.Reload(groups);
                    break;
                case "start":
                    var initialTarget = cockpit.CenterOfMass + cockpit.WorldMatrix.Forward * TORPEDO_INITIAL_DISTANCE;
                    weapons.Launch(initialTarget);
                    break;
            }

            weapons.UpdateNext();
        }

        #endregion
    }
}
