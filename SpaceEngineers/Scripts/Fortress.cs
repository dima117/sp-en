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

        private readonly Grid grid;
        private readonly WeaponController weapons;
        public Program()
        {
            tracker = new RuntimeTracker(this);
            trackerLcd = Me.GetSurface(1);
            trackerLcd.ContentType = ContentType.TEXT_AND_IMAGE;

            grid = new Grid(GridTerminalSystem);
            var mainCam = grid.GetByFilterOrAny<IMyCameraBlock>(
                x => x.CustomName.StartsWith("CAMERA"));

            var cockpit = grid.GetByFilterOrAny<IMyCockpit>();
            var cameras = grid.GetBlocksOfType<IMyCameraBlock>();
            var turrets = grid.GetBlocksOfType<IMyLargeTurretBase>();
            var antennas = grid.GetBlocksOfType<IMyRadioAntenna>();
            var lcdTargets = cockpit.GetSurface(0);
            var lcdTorpedos = cockpit.GetSurface(1);
            var lcdSystem = cockpit.GetSurface(2);
            var sound = grid.GetByFilterOrAny<IMySoundBlock>(x => x.CustomName.StartsWith("SOUND"));

            weapons = new WeaponController(
                cockpit,
                mainCam,
                cameras,
                turrets,
                lcdTargets,
                lcdTorpedos,
                lcdSystem,
                IGC,
                antennas,
                sound
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

            weapons.Execute(argument, updateSource);

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
                    weapons.Scan();
                    break;
                case "reload":
                    var groups = grid.GetBlockGroups(GROUP_PREFIX_TORPEDO);
                    weapons.Reload(groups);
                    break;
                case "start":
                    weapons.Launch();
                    break;
            }

            weapons.Update();

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
