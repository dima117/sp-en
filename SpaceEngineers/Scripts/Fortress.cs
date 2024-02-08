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

            var cockpit = grid.GetByFilterOrAny<IMyCockpit>();
            var mainCamera = grid.GetByFilterOrAny<IMyCameraBlock>(x => x.CustomName.StartsWith("CAMERA"), cam => cam.EnableRaycast = true);
            var cameras = grid.GetBlocksOfType<IMyCameraBlock>();
            var turrets = grid.GetBlocksOfType<IMyLargeTurretBase>();
            var antennas = grid.GetBlocksOfType<IMyRadioAntenna>();
            var lcdTargets = grid.GetByFilterOrAny<IMyTextPanel>(x => x.CustomName.StartsWith("TARGETS"));
            var sound = grid.GetByFilterOrAny<IMySoundBlock>(x => x.CustomName.StartsWith("SOUND"));

            weapons = new WeaponController(
                cockpit,
                mainCamera,
                cameras,
                turrets,
                lcdTargets,
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

            weapons.Update();
            


            //switch (argument)
            //{
            //    case "filter":
            //        onlyEnemies = !onlyEnemies;

            //        break;
            //    case "init":
            //        tt.UpdateCamArray();
            //        break;
            //    case "lock":
            //        var target = TargetTracker.Scan(cam, DISTANCE, onlyEnemies);

            //        if (target.HasValue)
            //        {
            //            tt.LockOn(target.Value);

            //            sound?.Play();
            //        }

            //        break;
            //    case "reset":
            //        tt.Clear();

            //        break;
            //    case "reload":
            //        var ids = new HashSet<long>(torpedos.Select(t => t.EntityId));

            //        var groups = new List<IMyBlockGroup>();
            //        GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith(GROUP_PREFIX_TORPEDO));

            //        torpedos.AddRange(groups
            //            .Select(gr => new Torpedo(gr, factor: 3f, lifespan: LIFESPAN))
            //            .Where(t => !ids.Contains(t.EntityId)));

            //        torpedos.RemoveAll(t => !t.IsAlive);

            //        break;
            //    case "start":
            //        // запускаем одну из торпед
            //        torpedos.FirstOrDefault(t => !t.Started)?.Start();

            //        break;
            //    default:
            //        // обновлям данные о цели
            //        tt.Update();

            //        // обновляем параметры цели на всех торпедах
            //        var state = torpedos?.Select(t => t.Update(tt.Current));
            //        var text = String.Join("\n", state?.Select(s => s.ToString()));

            //        lcdTorpedos?.WriteText(text);

            //        // выключаем звук, если цель потеряна
            //        if (!tt.Current.HasValue && sound?.IsWorking == true)
            //        {
            //            sound?.Stop();
            //        }

            //        break;
            //}

            tracker.AddInstructions();
            trackerLcd.WriteText(tracker.ToString());
        }


        //void UpdateSystemLcd()
        //{
        //    var filter = onlyEnemies ? "Enemies" : "All";

        //    var sb = new StringBuilder();
        //    sb.AppendLine($"Range: {cam.AvailableScanRange:0.0}");
        //    sb.AppendLine($"Total range: {tt.TotalRange:0.0}");
        //    sb.AppendLine($"Cam count: {tt.Count}");
        //    sb.AppendLine($"Filter: {filter}");

        //    lcdSystem.WriteText(sb.ToString());
        //}

        #endregion
    }
}
