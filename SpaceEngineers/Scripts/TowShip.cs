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

namespace SpaceEngineers.Scripts.TowShip
{
    public class Program : MyGridProgram
    {

        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Transmitter2.cs
        // import:Lib\TargetInfo.cs
        // import:Lib\Serializer.cs
        // import:Lib\Torpedo.cs
        // import:TargetTracker.cs

        const int DISTANCE = 15000;
        const int LIFESPAN = 600;

        const string BLOCK_NAME_CAMERA = "CAMERA";
        const string BLOCK_NAME_SOUND = "SOUND";
        const string GROUP_PREFIX_TORPEDO = "TORPEDO";

        bool onlyEnemies = false;

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;

        readonly Transmitter tsm;
        readonly TargetTracker tt;
        readonly IMyCameraBlock cam;

        readonly IMyTextSurface lcdTarget;
        readonly IMyTextSurface lcdSystem;
        readonly IMyTextSurface lcdTorpedos;

        readonly IMySoundBlock sound;

        readonly List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
        readonly List<Torpedo> torpedos = new List<Torpedo>();

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            // антенна
            GridTerminalSystem.GetBlocksOfType(antennas);
            tsm = new Transmitter2(IGC, antennas.ToArray());
            tsm.Subscribe(MsgTags.REMOTE_LOCK_TARGET, RemoteLock, true);
            tsm.Subscribe(MsgTags.REMOTE_START, RemoteStart, true);
            tsm.Subscribe(MsgTags.GET_STATUS, GetStatus, true);

            // массив камер радара
            tt = new TargetTracker(this);

            // главная камера
            cam = GridTerminalSystem.GetBlockWithName(BLOCK_NAME_CAMERA) as IMyCameraBlock;
            cam.EnableRaycast = true;

            // lcd
            var list2 = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(list2);

            var control = list2.FirstOrDefault(x => x.CubeGrid.EntityId == Me.CubeGrid.EntityId);
            lcdTorpedos = control?.GetSurface(0);
            lcdTarget = control?.GetSurface(1);
            lcdSystem = control?.GetSurface(2);

            // динамик
            sound = GridTerminalSystem.GetBlockWithName(BLOCK_NAME_SOUND) as IMySoundBlock;

            if (sound != null)
            {
                sound.SelectedSound = "ArcSoundBlockAlert2";
                sound.LoopPeriod = 300;
                sound.Range = 50;
                sound.Enabled = true;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        private void RemoteLock(MyIGCMessage message)
        {
            try
            {
                var data = message.Data.ToString();
                var reader = new Serializer.StringReader(data);

                TargetInfo target;
                if (Serializer.TryParseTargetInfo(reader, out target))
                {
                    tt.LockOn(target);

                    if (tt.Current != null)
                    {
                        sound?.Play();
                    }
                }

                Me.CustomData = data;
            }
            catch (Exception ex)
            {
                Me.CustomData = ex.Message + "\n" + ex.StackTrace;
            }
        }

        private void RemoteStart(MyIGCMessage message)
        {
            var now = DateTime.UtcNow;

            // запускаем одну из торпед
            torpedos.FirstOrDefault(t => !t.Started)?.Start(now);
        }

        private void GetStatus(MyIGCMessage message)
        {
            var t = tt.Current == null ? "FALSE" : tt.Current.Entity.Type.ToString();
            var text = $"Locked: ${t}\n" + lcdTorpedos.GetText();

            tsm.Send(MsgTags.REMOTE_STATUS, text, message.Source);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var now = DateTime.UtcNow;

            tracker.AddRuntime();

            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "filter":
                    onlyEnemies = !onlyEnemies;

                    break;
                case "init":
                    tt.UpdateCamArray();
                    break;
                case "lock":
                    var target = TargetTracker.Scan(cam, DISTANCE, onlyEnemies);

                    if (target != null)
                    {
                        tt.LockOn(target);

                        sound?.Play();
                    }

                    break;
                case "reset":
                    tt.Clear();

                    break;
                case "reload":
                    var ids = new HashSet<long>(torpedos.Select(t => t.EntityId));

                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith(GROUP_PREFIX_TORPEDO));

                    torpedos.AddRange(groups
                        .Select(gr => new Torpedo(gr, factor: 3f, lifespan: LIFESPAN))
                        .Where(t => !ids.Contains(t.EntityId)));

                    torpedos.RemoveAll(t => !t.IsAlive(now));

                    break;
                case "start":
                    // запускаем одну из торпед
                    torpedos.FirstOrDefault(t => !t.Started)?.Start(now);

                    break;
                default:
                    // обновлям данные о цели
                    tt.Update();

                    // обновляем параметры цели на всех торпедах
                    var state = torpedos?.Select(t => t.Update(now, tt.Current));
                    var text = String.Join("\n", state?.Select(s => s.ToString()));

                    lcdTorpedos?.WriteText(text);

                    // выключаем звук, если цель потеряна
                    if (tt.Current == null && sound?.IsWorking == true)
                    {
                        sound?.Stop();
                    }

                    break;
            }

            UpdateTargetLcd();
            UpdateSystemLcd();

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        void UpdateTargetLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {tt.Current != null}");

            if (tt.Current != null)
            {
                var target = tt.Current.Entity;
                var distance = Vector3D.Distance(cam.GetPosition(), target.Position);

                sb.AppendLine($"{target.Type}\nv: {target.Velocity.Length():0.0}\ns: {distance:0.0}");
            }

            lcdTarget?.WriteText(sb.ToString());
        }

        void UpdateSystemLcd()
        {
            var filter = onlyEnemies ? "Enemies" : "All";

            var sb = new StringBuilder();
            sb.AppendLine($"Range: {cam.AvailableScanRange:0.0}");
            sb.AppendLine($"Total range: {tt.TotalRange:0.0}");
            sb.AppendLine($"Cam count: {tt.Count}");
            sb.AppendLine($"Filter: {filter}");

            lcdSystem.WriteText(sb.ToString());
        }

        #endregion
    }
}
