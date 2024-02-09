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

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        #region Copy

        // import:Lib\Torpedo.cs
        // import:TargetTracker.cs
        // import:WcRadar.cs

        TargetTracker tt;
        IMyCameraBlock cam;

        List<Torpedo> torpedos = new List<Torpedo>();
        List<IMyShipWelder> welders = new List<IMyShipWelder>();

        IMyTextPanel lcd1; // система
        IMyTextPanel lcd2; // цель
        IMyTextPanel lcd3; // торпеда
        IMyTextPanel lcd4; // цели из weapon core

        IMySoundBlock sound; // динамик
        WcRadar radar; // радар weapon core

        bool onlyEnemies = false;

        public Program()
        {
            WcPbApi.Instance.Activate(Me);

            // массив камер радара
            tt = new TargetTracker(this);
            radar = new WcRadar();

            // главная камера
            cam = GridTerminalSystem.GetBlockWithName("MAIN_CAM") as IMyCameraBlock;
            cam.EnableRaycast = true;

            // сварщики
            GridTerminalSystem.GetBlocksOfType(welders, w => w.CustomName.StartsWith("T_WELDER_"));

            lcd1 = GridTerminalSystem.GetBlockWithName("LCD1") as IMyTextPanel;
            lcd2 = GridTerminalSystem.GetBlockWithName("LCD2") as IMyTextPanel;
            lcd3 = GridTerminalSystem.GetBlockWithName("LCD3") as IMyTextPanel;
            lcd4 = GridTerminalSystem.GetBlockWithName("LCD4") as IMyTextPanel;
            sound = GridTerminalSystem.GetBlockWithName("T_SOUND") as IMySoundBlock;

            Me.GetSurface(0).WriteText("TARGETING");
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "filter":
                    onlyEnemies = !onlyEnemies;

                    break;
                case "lock":
                    var entity = TargetTracker.Scan(cam, 5000, onlyEnemies);

                    if (entity != null)
                    {
                        tt.LockOn(entity);

                        sound?.Play();
                    }

                    break;

                case "reset":
                    tt.Clear();

                    break;
                case "reload":
                    welders.ForEach(w => w.Enabled = true);
                    break;
                case "init":
                    var ids = new HashSet<long>(torpedos.Select(t => t.EntityId));

                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("TORPEDO"));

                    torpedos.AddRange(groups
                        .Select(gr => new Torpedo(gr))
                        .Where(t => !ids.Contains(t.EntityId)));

                    torpedos.RemoveAll(t => !t.IsAlive);

                    welders.ForEach(w => w.Enabled = false);
                    break;
                case "start":
                    // запускаем одну из торпед
                    torpedos.FirstOrDefault(t => !t.Started)?.Start();

                    break;
                default:
                    tt.Update();

                    if (sound?.IsWorking == true && tt.Current == null)
                    {
                        sound?.Stop();
                    }

                    // обновляем параметры цели на всех торпедах
                    torpedos?.ForEach(t => t.Update(tt.Current));

                    break;
            }


            UpdateSystemLcd();
            UpdateTargetLcd();
            UpdateWcLcd();
            UpdateTorpedoLcd(torpedos);
        }

        private void UpdateWcLcd()
        {
            radar.Update();
            lcd4.WriteText(radar.ToString());
        }

        void UpdateSystemLcd()
        {
            var filter = onlyEnemies ? "Enemies" : "All";

            var sb = new StringBuilder();
            sb.AppendLine($"Range: {cam.AvailableScanRange:0.0}");
            sb.AppendLine($"Cam count: {tt.Count}");
            sb.AppendLine($"Welders count: {welders.Count}");
            sb.AppendLine($"Filter: {filter}");
            lcd1.WriteText(sb.ToString());
        }

        void UpdateTargetLcd()
        {
            var sb = new StringBuilder();

            if (tt.Current != null)
            {
                var target = tt.Current.Entity;
                var distance = Vector3D.Distance(cam.GetPosition(), target.Position);

                sb.AppendLine($"Locked: TRUE");

                sb.AppendLine($"- type: {target.Type}");
                sb.AppendLine($"- speed: {target.Velocity.Length():0.0}");
                sb.AppendLine($"- distance: {distance:0.0}");
                sb.AppendLine($"- position X: {target.Position.X:0.0}");
                sb.AppendLine($"- position Y: {target.Position.Y:0.0}");
                sb.AppendLine($"- position Z: {target.Position.Z:0.0}");
            }
            else
            {
                sb.AppendLine($"Locked: FALSE");
            }

            lcd2.WriteText(sb);
        }

        void UpdateTorpedoLcd(List<Torpedo> torpedos)
        {
            var targetPos = tt.Current?.Entity.Position;

            var sb = new StringBuilder();

            for (var i = 0; i < torpedos?.Count; i++)
            {
                var t = torpedos[i];
                var myPos = t.Position;

                sb.Append($"{i + 1} Speed: {t.Speed:0.0}");

                if (!t.Started)
                {
                    sb.Append(" Status: Ready");
                }
                else if (t.IsAlive)
                {
                    sb.Append(" Status: Active");
                }
                else
                {
                    sb.Append(" Status: Dead");
                }

                if (targetPos != null && t.IsAlive)
                {
                    var distance = Vector3D.Distance(myPos, targetPos.Value);

                    sb.Append($" Distance: {distance:0.0}");
                }

                sb.AppendLine();
            }

            lcd3.WriteText(sb.ToString());
        }
        #endregion
    }
}
