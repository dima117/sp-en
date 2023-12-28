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
        // 1. захват цели камерой
        // 2. удаленный захват цели
        // 3. сброс цели
        // 4. отображение информации о цели
        // 5. повторяемая инициализация камер
        // 6. отображение информации о камерах
        // 7. повторяемая инициализация торпед
        // 8. пуск торпед
        // 9. отображать статус торпед
        // 0. обновлять цель на торпедах


        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Transmitter.cs
        // import:Lib\TargetInfo.cs
        // import:Lib\Serializer.cs
        // import:Lib\Torpedo.cs
        // import:TargetTracker.cs

        const int DISTANCE = 15000;
        const int LIFESPAN = 540;

        const string BLOCK_NAME_CAMERA = "CAMERA";
        const string BLOCK_NAME_SOUND = "SOUND";

        bool onlyEnemies = false;

        TargetInfo? target = null;

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        IMyTextSurface lcdTarget;
        IMyTextSurface lcdSystem;
        IMyTextSurface lcdTorpedos;

        readonly Transmitter tsm;
        readonly TargetTracker tt;
        readonly IMyCameraBlock cam;
        readonly List<Torpedo> torpedos = new List<Torpedo>();
        readonly IMySoundBlock sound;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            // антенна
            tsm = new Transmitter(this);
            tsm.Subscribe(MsgTags.LOCK_TARGET, RemoteLock, true);

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

                    if (tt.Current.HasValue)
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

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "init":
                    var ids = new HashSet<long>(torpedos.Select(t => t.EntityId));

                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("TORPEDO"));

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

                    // обновляем параметры цели на всех торпедах
                    var state = torpedos?.Select(t => t.Update(target));
                    var text = String.Join("\n", state?.Select(s => s.ToString()));

                    lcdTorpedos.WriteText(text);

                    break;
            }

            UpdateTargetLcd();

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        void UpdateTargetLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {target.HasValue}");

            if (target.HasValue)
            {
                var entity = target.Value.Entity;
                var distance = Vector3D.Distance(Me.GetPosition(), entity.Position);

                sb.AppendLine($"{entity.Type}\nV={entity.Velocity.Length():0.0}\nS={distance:0.0}");
            }

            lcdTarget?.WriteText(sb.ToString());
        }

        #endregion
    }
}
