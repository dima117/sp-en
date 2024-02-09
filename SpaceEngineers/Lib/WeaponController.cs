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
using Sandbox.Game.GameSystems;
using SpaceEngineers.Lib;
using System.Data;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:Serializer.cs
    // import:Transmitter2.cs
    // import:TargetTracker2.cs

    public class WeaponController
    {
        const int DISTANCE = 6500;

        private TargetTracker2 tracker;
        private Transmitter2 transmitter;
        private IMyTextSurface lcdTargets;
        private IMyTextSurface lcdSystem;
        private IMyShipController cockpit;
        private IMySoundBlock sound;
        private IMyCameraBlock mainCamera;

        private bool onlyEnemies;
        private int targetIndex;
        private long targetId;

        public event Action<Exception> OnError;

        public WeaponController(
            IMyShipController cockpit,
            IMyCameraBlock mainCamera,
            IMyCameraBlock[] cameras,
            IMyLargeTurretBase[] turrets,
            IMyTextSurface lcdTargets,
            IMyTextSurface lcdSystem,
            IMyIntergridCommunicationSystem igc,
            IMyRadioAntenna[] antennas,
            IMySoundBlock sound
        )
        {
            tracker = new TargetTracker2(cameras, turrets);
            tracker.TargetListChanged += Tracker_TargetListChanged;

            transmitter = new Transmitter2(igc, antennas);
            transmitter.Subscribe(MsgTags.SYNC_TARGETS, Transmitter_SyncTargets, true);

            this.cockpit = cockpit;
            this.lcdTargets = lcdTargets;
            this.lcdSystem = lcdSystem;

            this.mainCamera = mainCamera;
            if (mainCamera != null)
            {
                mainCamera.Enabled = true;
                mainCamera.EnableRaycast = true;
            }

            this.sound = sound;
            if (sound != null)
            {
                sound.Enabled = true;
                sound.SelectedSound = "ArcSoundBlockAlert2";
                sound.Volume = 1;
                sound.Range = 100;
            }
        }

        public void ToggleFilter()
        {
            onlyEnemies = !onlyEnemies;
        }

        public void NextTarget()
        {
            if (targetIndex + 1 < tracker.TargetCount)
            {
                targetIndex++;
                targetId = tracker.GetTargets()[targetIndex].Entity.EntityId;
            }
        }
        public void PrevTarget()
        {
            if (targetIndex - 1 >= 0)
            {
                targetIndex--;
                targetId = tracker.GetTargets()[targetIndex].Entity.EntityId;
            }

        }

        public void Scan()
        {
            var target = TargetTracker2.Scan(mainCamera, DISTANCE, onlyEnemies);

            if (target != null)
            {
                sound?.Play();
                tracker.LockTarget(target);
            }
        }

        public void Execute(string argument, UpdateType updateSource)
        {
            // обрабатываем принятые сообщения
            transmitter.Update(argument, updateSource);

            // обновлям данные о цели
            tracker.Update();
        }

        public void Update()
        {
            // обновляем содержимое экранов
            UpdateLcdTargets();
            UpdateLcdSystem();
        }

        private void UpdateLcdSystem()
        {
            var filter = onlyEnemies ? "Enemies" : "All";

            var sb = new StringBuilder();
            sb.AppendLine($"Range: {mainCamera.AvailableScanRange:0.0}");
            sb.AppendLine($"Total range: {tracker.TotalRange:0.0}");
            sb.AppendLine($"Cam count: {tracker.Count}");
            sb.AppendLine($"Filter: {filter}");

            lcdSystem.WriteText(sb);
        }

        private void UpdateLcdTargets()
        {
            var sb = new StringBuilder();
            var targets = tracker.GetTargets();

            if (targets.Any())
            {
                var selfPos = cockpit.GetPosition();

                for (var i = 0; i < targets.Length; i++)
                {
                    var t = targets[i].Entity;

                    var type = t.Type.ToString().Substring(0, 1);
                    var name = TargetTracker2.GetName(t.EntityId);
                    var dist = (t.Position - selfPos).Length();
                    var speed = t.Velocity.Length();

                    var pointer = targetIndex == i ? "> " : " ";

                    sb.AppendLine($"{pointer}{type} {name} {dist:0}m {speed:0}m/s");
                }
            }
            else
            {
                sb.AppendLine("NO TARGETS");
            }

            lcdTargets.WriteText(sb);
        }

        private void Tracker_TargetListChanged()
        {
            sound?.Play();

            var targets = tracker.GetTargets();
            targetIndex = Array.FindIndex(targets, t => t.Entity.EntityId == targetId);

            if (targetIndex < 0)
            {
                targetIndex = 0;
                targetId = targets.Any() ? targets[0].Entity.EntityId : 0;
            }
        }

        private void Transmitter_SyncTargets(MyIGCMessage message)
        {
            try
            {
                var data = message.Data.ToString();
                var reader = new Serializer.StringReader(data);

                TargetInfo[] targets;
                if (Serializer.TryParseTargetInfoArray(reader, out targets))
                {
                    tracker.Merge(targets);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }
    }

    #endregion
}
