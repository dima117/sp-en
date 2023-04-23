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

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        #region Copy

        TargetTracker tt;
        IMyCameraBlock cam;

        List<Torpedo> torpedos;
        List<IMyShipWelder> welders = new List<IMyShipWelder>();

        IMyTextPanel lcd1; // система
        IMyTextPanel lcd2; // цель
        IMyTextPanel lcd3; // торпеда

        public Program()
        {
            // массив камер радара
            tt = new TargetTracker(this, "Камера");

            // главная камера
            cam = GridTerminalSystem.GetBlockWithName("MAIN_CAM") as IMyCameraBlock;
            cam.EnableRaycast = true;

            // сварщики
            GridTerminalSystem.GetBlocksOfType(welders, w => w.CustomName.StartsWith("WELDER_"));

            lcd1 = GridTerminalSystem.GetBlockWithName("LCD1") as IMyTextPanel;
            lcd2 = GridTerminalSystem.GetBlockWithName("LCD2") as IMyTextPanel;
            lcd3 = GridTerminalSystem.GetBlockWithName("LCD3") as IMyTextPanel;

            Me.GetSurface(0).WriteText("TARGETING");
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "lock":
                    var entity = TargetTracker.Scan(cam, 10000);

                    if (entity.HasValue)
                    {
                        tt.LockOn(entity.Value);
                    }

                    break;
                case "reload":
                    torpedos = null;
                    welders.ForEach(w => w.Enabled = true);
                    break;
                case "prepare":

                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("TORPEDO"));

                    torpedos = groups.Select(gr => new Torpedo(gr)).ToList();

                    welders.ForEach(w => w.Enabled = false);
                    break;
                case "start":
                    // запускаем одну из торпед
                    torpedos.FirstOrDefault(t => !t.Started)?.Start();

                    break;
                default:
                    tt.Update();

                    // обновляем параметры цели на всех торпедах
                    torpedos?.ForEach(t => t.Update(tt.Current));

                    break;
            }

            UpdateSystemLcd();
            UpdateTargetLcd();
            UpdateTorpedoLcd(torpedos);
        }

        void UpdateSystemLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Range: {cam.AvailableScanRange:0.000}");
            sb.AppendLine($"Cam count: {tt.Count}");
            lcd1.WriteText(sb.ToString());
        }

        void UpdateTargetLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {tt.Current.HasValue}");

            if (tt.Current.HasValue)
            {
                var target = tt.Current.Value.Entity;
                var distance = Vector3D.Distance(cam.GetPosition(), target.Position);

                sb.AppendLine($"- type: {target.Type}");
                sb.AppendLine($"- position: {target.Position}");
                sb.AppendLine($"- speed: {target.Velocity.Length():0.000}");
                sb.AppendLine($"- distance: {distance:0.000}");
            }

            lcd2.WriteText(sb.ToString());
        }

        void UpdateTorpedoLcd(List<Torpedo> torpedos)
        {
            var targetPos = tt.Current?.Entity.Position;

            var sb = new StringBuilder();

            for (var i = 0; i < torpedos?.Count; i++)
            {
                var t = torpedos[i];
                var myPos = t.Position;

                sb.Append($"{i + 1} SPD {t.Speed:0.0}");

                if (!t.Started)
                {
                    sb.Append(" STE Ready");
                }
                else if (t.IsAlive)
                {
                    sb.Append(" STE Active");
                }
                else
                {
                    sb.Append(" STE Dead");
                }

                if (targetPos != null)
                {
                    var distance = Vector3D.Distance(myPos, targetPos.Value);

                    sb.Append($" DST {distance:0.0}");
                }

                sb.AppendLine();
            }

            lcd3.WriteText(sb.ToString());
        }
        #endregion
    }
}
