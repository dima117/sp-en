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
        // import:Lib\Transmitter.cs
        // import:Lib\TargetInfo.cs
        // import:Lib\Serializer.cs
        // import:Lib\Torpedo.cs
        // import:Lib\Grid.cs
        // import:TargetTracker.cs

        const int DISTANCE = 7000;
        const int LIFESPAN = 600;

        const string BLOCK_NAME_CAMERA = "CAMERA";
        const string BLOCK_NAME_SOUND = "SOUND";
        const string GROUP_PREFIX_TORPEDO = "TORPEDO";

        bool onlyEnemies = false;

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        readonly Grid grid;

        readonly Transmitter tsm;
        readonly TargetTracker2 tt;
        readonly IMyCameraBlock cam;

        readonly IMyTextSurface lcdTarget;
        readonly IMyTextSurface lcdSystem;
        readonly IMyTextSurface lcdTorpedos;

        readonly IMySoundBlock sound;

        readonly List<Torpedo> torpedos = new List<Torpedo>();

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            grid = new Grid(GridTerminalSystem);

            // антенна
            tsm = new Transmitter(this);
            tsm.Subscribe(MsgTags.SYNC_TARGETS, SyncTargets, true);

            // массив камер радара
            var cameras = new List<IMyCameraBlock>();

            tt = new TargetTracker2(grid.GetBlocksOfType<IMyCameraBlock>());
            tt.TargetListChanged += TargetListChanged
                ;

            // главная камера
            cam = grid.GetBlockWithName<IMyCameraBlock>(BLOCK_NAME_CAMERA);
            cam.EnableRaycast = true;

            // lcd
            var list2 = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(list2);

            var control = grid.GetByFilterOrAny<IMyCockpit>(x => x.CubeGrid.EntityId == Me.CubeGrid.EntityId);
            lcdTorpedos = control?.GetSurface(0);
            lcdTarget = control?.GetSurface(1);
            lcdSystem = control?.GetSurface(2);

            // динамик
            sound = grid.GetBlockWithName<IMySoundBlock>(BLOCK_NAME_SOUND);

            if (sound != null)
            {
                sound.SelectedSound = "ArcSoundBlockAlert2";
                sound.LoopPeriod = 300;
                sound.Range = 50;
                sound.Enabled = true;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        private void TargetListChanged()
        {
            sound?.Play();
        }

        private void SyncTargets(MyIGCMessage message)
        {
            try
            {
                var data = message.Data.ToString();
                var reader = new Serializer.StringReader(data);

                TargetInfo[] targets;
                if (Serializer.TryParseTargetInfoArray(reader, out targets))
                {
                    tt.Merge(targets);
                }

                Me.CustomData = data;
            }
            catch (Exception ex)
            {
                Me.CustomData = ex.Message + "\n" + ex.StackTrace;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
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

                    if (target.HasValue)
                    {
                        tt.LockOn(target.Value);

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

                    torpedos.RemoveAll(t => !t.IsAlive);

                    break;
                case "start":
                    // запускаем одну из торпед
                    torpedos.FirstOrDefault(t => !t.Started)?.Start();

                    break;
                default:
                    // обновлям данные о цели
                    tt.Update();

                    // обновляем параметры цели на всех торпедах
                    var state = torpedos?.Select(t => t.Update(tt.Current));
                    var text = String.Join("\n", state?.Select(s => s.ToString()));

                    lcdTorpedos?.WriteText(text);

                    // выключаем звук, если цель потеряна
                    if (!tt.Current.HasValue && sound?.IsWorking == true)
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
            sb.AppendLine($"Locked: {tt.Current.HasValue}");

            if (tt.Current.HasValue)
            {
                var target = tt.Current.Value.Entity;
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
