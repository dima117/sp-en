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
using SpaceEngineers.Lib.Torpedos;
using System.Data;
using SpaceEngineers.Scripts.Torpedos;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:Serializer.cs
    // import:Transmitter2.cs
    // import:TargetTracker2.cs
    // import:DirectionController2.cs
    // import:Torpedos\SpaceTorpedo.cs

    public class WeaponController
    {
        const int RAYCAST_DISTANCE = 6500;
        const int TORPEDO_LIFESPAN = 600;

        public const int RAILGUN_SPEED = 2000;
        public const int ARTILLERY_SPEED = 500;

        private TargetTracker2 tracker;
        private Transmitter2 transmitter;
        private IMyTextSurface lcdTargets;
        private IMyTextSurface lcdTorpedos;
        private IMyTextSurface lcdSystem;
        private IMyShipController cockpit;
        private IMySoundBlock sound;
        private IMyBeacon beacon;
        private IMyTextPanel[] hud;

        private bool onlyEnemies;
        private int targetIndex;
        private long targetId;

        private DateTime lastUpdateHUD = DateTime.MinValue;
        private int aimBotTargetShotSpeed;

        public event Action<Exception> OnError;

        readonly DirectionController2 directionController;
        readonly Dictionary<long, SpaceTorpedo> torpedos = new Dictionary<long, SpaceTorpedo>();
        readonly Dictionary<long, long> targeting = new Dictionary<long, long>(); // цели торпед

        public WeaponController(
            IMyGyro[] gyros,
            IMyShipController cockpit,
            IMyCameraBlock[] cameras,
            IMyLargeTurretBase[] turrets,
            IMyTextPanel[] hud,
            IMyTextSurface lcdTargets,
            IMyTextSurface lcdTorpedos,
            IMyTextSurface lcdSystem,
            IMyIntergridCommunicationSystem igc,
            IMyRadioAntenna[] antennas,
            IMySoundBlock sound,
            IMyBeacon beacon = null
        )
        {
            tracker = new TargetTracker2(cameras, turrets);
            tracker.TargetListChanged += Tracker_TargetListChanged;

            transmitter = new Transmitter2(igc, antennas);
            transmitter.Subscribe(MsgTags.SYNC_TARGETS, Transmitter_SyncTargets, true);
            transmitter.Subscribe(MsgTags.REMOTE_LOCK_TARGET, Transmitter_RemoteLock, true);

            this.beacon = beacon;
            this.cockpit = cockpit;
            this.lcdTargets = lcdTargets;
            this.lcdTorpedos = lcdTorpedos;
            this.lcdSystem = lcdSystem;
            this.hud = hud;

            this.sound = sound;
            if (sound != null)
            {
                sound.Enabled = true;
                sound.SelectedSound = "ArcSoundBlockAlert2";
                sound.Volume = 1;
                sound.Range = 100;
            }

            directionController = new DirectionController2(cockpit, gyros);
        }

        public TargetInfo Current => tracker.GetByEntityId(targetId);

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

        public void Scan(IMyCameraBlock cam)
        {
            var target = TargetTracker2.Scan(cam, RAYCAST_DISTANCE, onlyEnemies);

            if (target != null)
            {
                sound?.Play();
                tracker.LockTarget(target);

                // выбираем в залоченную цель в качестве текущей
                targetId = target.Entity.EntityId;

                FixTargetIndex();
            }
        }

        public void Reload(IMyBlockGroup[] groups)
        {
            foreach (var gr in groups)
            {
                var tmp = new SpaceTorpedo(gr, factor: 3f, lifespan: TORPEDO_LIFESPAN);

                // добавляем новые торпеды
                if (!torpedos.ContainsKey(tmp.EntityId))
                {
                    torpedos.Add(tmp.EntityId, tmp);
                }
            }

            foreach (var t in torpedos.ToArray())
            {
                if (!t.Value.IsAlive)
                {
                    torpedos.Remove(t.Key);
                    targeting.Remove(t.Key);
                }
            }
        }

        public void Aim(int shotSpeed)
        {
            aimBotTargetShotSpeed = shotSpeed;
        }

        public void ClearAimBotTarget()
        {
            aimBotTargetShotSpeed = 0;
        }

        public bool Launch()
        {
            // запускает торпеду по текущей цели
            var target = Current;
            var torpedo = torpedos.Values.FirstOrDefault(t => t.Stage == LaunchStage.Ready);

            if (target == null || torpedo == null)
            {
                return false;
            }

            targeting[torpedo.EntityId] = target.Entity.EntityId;
            torpedo.Start();

            return true;
        }

        public void Execute(string argument, UpdateType updateSource)
        {
            // обрабатываем принятые сообщения
            transmitter.Update(argument, updateSource);

            // обновлям данные о цели
            tracker.Update();
        }

        public bool Update()
        {
            var selfPos = cockpit.GetPosition();

            // обновляем цели торпед
            UpdateTorpedoTargets();

            var controlDirection = UpdateAimBot();

            // обновляем содержимое экранов
            UpdateHUD(selfPos);
            UpdateLcdTargets(selfPos);
            UpdateLcdSystem();

            return controlDirection;
        }

        private bool UpdateAimBot()
        {
            if (aimBotTargetShotSpeed > 0)
            {
                var target = Current;

                if (target != null)
                {
                    directionController.InterceptShot(target.Entity, aimBotTargetShotSpeed);
                    return true;
                }
            }

            return false;
        }

        private void UpdateTorpedoTargets()
        {
            var sb = new StringBuilder();

            foreach (var t in torpedos.Values)
            {
                var targetId = targeting.GetValueOrDefault(t.EntityId);
                var target = tracker.GetByEntityId(targetId);

                var state = t.Update(target);
                sb.AppendLine(state.ToString());
            }

            lcdTorpedos?.WriteText(sb);
        }

        private void UpdateHUD(Vector3D selfPos)
        {
            var now = DateTime.UtcNow;

            if (hud.Any() && (now - lastUpdateHUD).TotalMilliseconds > 100)
            {
                var targetName = "NO TARGET";
                var dist = "--";

                if (Current != null)
                {
                    var t = Current.Entity;
                    var d = (t.Position - selfPos).Length();

                    targetName = t.Name;
                    dist = d.ToString("0m");
                }

                var aimbot = "Off";

                if (aimBotTargetShotSpeed > 0)
                {

                    switch (aimBotTargetShotSpeed)
                    {
                        case RAILGUN_SPEED:
                            aimbot = "Rail";
                            break;
                        case ARTILLERY_SPEED:
                            aimbot = "Art";
                            break;
                        default:
                            aimbot = aimBotTargetShotSpeed.ToString("0 m/s");
                            break;
                    }
                }

                var tCount = torpedos.Values.Count(t => t.Stage == LaunchStage.Ready);


                foreach (var lcd in hud)
                {
                    var sprites = GetHudState(targetName, dist, aimbot, tCount);

                    using (var frame = lcd.DrawFrame())
                    {
                        frame.AddRange(sprites);
                    }
                }

                lastUpdateHUD = now;
            }
        }

        private MySprite[] GetHudState(string targetName, string dist, string aimbot, int tCount)
        {
            var list = new List<MySprite>();

            // target
            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "target",
                Position = new Vector2(256, 0),
                RotationOrScale = 0.8f,
                Color = Color.Teal,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            });

            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = targetName,
                Position = new Vector2(256, 20),
                RotationOrScale = 1f,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            });

            // target distance
            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "dist",
                Position = new Vector2(0, 0),
                RotationOrScale = 0.8f,
                Color = Color.Teal,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = dist,
                Position = new Vector2(0, 20),
                RotationOrScale = 1f,
                Color = Color.White,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            // torpedo count
            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "torpedos",
                Position = new Vector2(512, 0),
                RotationOrScale = 0.8f,
                Color = Color.Teal,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = tCount.ToString(),
                Position = new Vector2(512, 20),
                RotationOrScale = 1f,
                Color = Color.White,
                Alignment = TextAlignment.RIGHT,
                FontId = "White"
            });

            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "aimbot",
                Position = new Vector2(0, 462),
                RotationOrScale = 0.8f,
                Color = Color.Teal,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            list.Add(new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = aimbot,
                Position = new Vector2(0, 482),
                RotationOrScale = 1f,
                Color = Color.White,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            });

            return list.ToArray();
        }

        private void UpdateLcdSystem()
        {
            var filter = onlyEnemies ? "Enemies" : "All";

            var sb = new StringBuilder();
            sb.AppendLine($"Total range: {tracker.TotalRange:0.0}");
            sb.AppendLine($"Cam count: {tracker.Count}");
            sb.AppendLine($"Filter: {filter}");

            lcdSystem?.WriteText(sb);
        }

        private void UpdateLcdTargets(Vector3D selfPos)
        {
            var sb = new StringBuilder();
            var targets = tracker.GetTargets();

            if (targets.Any())
            {
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

            lcdTargets?.WriteText(sb);
        }

        private void Tracker_TargetListChanged()
        {
            sound?.Play();

            FixTargetIndex();
        }

        private void FixTargetIndex() {
            var targets = tracker.GetTargets();
            targetIndex = Array.FindIndex(targets, t => t.Entity.EntityId == targetId);

            if (targetIndex < 0)
            {
                targetIndex = 0;
                targetId = targets.Any() ? targets[0].Entity.EntityId : 0;
            }
        }

        private void Transmitter_RemoteLock(MyIGCMessage message)
        {
            try
            {
                var data = message.Data.ToString();
                var reader = new Serializer.StringReader(data);

                TargetInfo target;
                if (Serializer.TryParseTargetInfo(reader, out target))
                {
                    tracker.LockTarget(target);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
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
