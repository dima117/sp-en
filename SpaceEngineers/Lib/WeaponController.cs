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
    // import:Aimbot.cs
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
        private IMySoundBlock soundEnemyLock;
        private IMyTextPanel[] hud;
        private IMySmallMissileLauncherReload[] railguns;

        private bool onlyEnemies;
        private int targetIndex;
        private long targetId;

        private DateTime? enemyLock;

        private DateTime lastUpdateHUD = DateTime.MinValue;

        private int aimbotTargetShotSpeed;
        private AimbotState lastAimbotState = AimbotState.UNKNOWN;
        private DateTime lastAimbotStateUpdated;
        private void SetAimbotState(AimbotState state)
        {
            if (state != lastAimbotState)
            {
                lastAimbotStateUpdated = DateTime.UtcNow;
            }

            lastAimbotState = state;
        }


        public event Action<Exception> OnError;

        readonly Aimbot aimbot;
        readonly Dictionary<long, SpaceTorpedo> torpedos = new Dictionary<long, SpaceTorpedo>();
        readonly Dictionary<long, long> targeting = new Dictionary<long, long>(); // цели торпед

        public WeaponController(
            IMyGyro[] gyros,
            IMyShipController cockpit,
            IMyCameraBlock[] cameras,
            IMyLargeTurretBase[] turrets,
            IMySmallMissileLauncherReload[] railguns,
            IMyTextPanel[] hud,
            IMyTextSurface lcdTargets,
            IMyTextSurface lcdTorpedos,
            IMyTextSurface lcdSystem,
            IMyIntergridCommunicationSystem igc,
            IMyRadioAntenna[] antennas,
            IMySoundBlock sound,
            IMySoundBlock soundEnemyLock
        )
        {
            tracker = new TargetTracker2(cameras, turrets);
            tracker.TargetListChanged += Tracker_TargetListChanged;

            transmitter = new Transmitter2(igc, antennas);
            transmitter.Subscribe(MsgTags.SYNC_TARGETS, Transmitter_SyncTargets, true);
            transmitter.Subscribe(MsgTags.REMOTE_LOCK_TARGET, Transmitter_RemoteLock, true);

            this.cockpit = cockpit;
            this.lcdTargets = lcdTargets;
            this.lcdTorpedos = lcdTorpedos;
            this.lcdSystem = lcdSystem;
            this.hud = hud;

            this.railguns = railguns;

            this.sound = sound;
            this.soundEnemyLock = soundEnemyLock;

            aimbot = new Aimbot(cockpit, gyros);
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
                var tmp = new SpaceTorpedo(gr, factor: 2.5f, lifespan: TORPEDO_LIFESPAN);

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

        public void Aim()
        {
            aimbotTargetShotSpeed = aimbotTargetShotSpeed == ARTILLERY_SPEED
                ? RAILGUN_SPEED : ARTILLERY_SPEED;

            SetAimbotState(aimbot.Reset());
        }

        public void ClearAimBotTarget()
        {
            aimbotTargetShotSpeed = 0;
        }

        public void SetEnemyLock()
        {
            enemyLock = DateTime.UtcNow;
            soundEnemyLock?.Play();
        }

        public void ClearEnemyLock()
        {
            enemyLock = null;
            soundEnemyLock?.Stop();
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

        private int updateIndex = 0;

        public void UpdateNext(string argument, UpdateType updateSource)
        {

            switch (updateIndex)
            {
                case 0:
                    // обрабатываем принятые сообщения
                    transmitter.Update(argument, updateSource);

                    // обновлям данные о цели
                    tracker.Update();
                    break;
                case 1:
                    // обновляем цели торпед
                    UpdateTorpedoTargets();
                    break;
                case 2:
                    // обновляем наведение курсовых орудий
                    UpdateAimBot();
                    break;
                case 3:
                    var selfPos = cockpit.GetPosition();

                    // обновляем содержимое экранов
                    UpdateHUD(selfPos);
                    UpdateLcdTargets(selfPos);
                    UpdateLcdSystem();
                    break;
            }

            updateIndex = (updateIndex + 1) % 4;
        }


        public void Update(string argument, UpdateType updateSource)
        {
            try
            {
                // обрабатываем принятые сообщения
                transmitter.Update(argument, updateSource);

                // обновлям данные о цели
                tracker.Update();

                var selfPos = cockpit.GetPosition();

                // обновляем цели торпед
                UpdateTorpedoTargets();

                UpdateAimBot();

                // обновляем содержимое экранов
                UpdateHUD(selfPos);
                UpdateLcdTargets(selfPos);
                UpdateLcdSystem();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public bool AimbotEnabled
        {
            get
            {
                return aimbotTargetShotSpeed > 0;
            }
        }

        private void UpdateAimBot()
        {
            if (aimbotTargetShotSpeed > 0)
            {
                var target = Current;

                if (target != null)
                {
                    SetAimbotState(aimbot.Aim(target.Entity, aimbotTargetShotSpeed));
                }
            }
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

        private HashSet<MyRelationsBetweenPlayerAndBlock> friends =
            new HashSet<MyRelationsBetweenPlayerAndBlock> {
                MyRelationsBetweenPlayerAndBlock.Owner,
                MyRelationsBetweenPlayerAndBlock.FactionShare,
                MyRelationsBetweenPlayerAndBlock.Friends,
            };

        private void UpdateHUD(Vector3D selfPos)
        {
            var now = DateTime.UtcNow;

            if (hud.Any() && (now - lastUpdateHUD).TotalMilliseconds > 100)
            {
                var targetName = "NO TARGET";
                string dist = null;

                if (Current != null)
                {
                    var t = Current.Entity;
                    var d = (t.Position - selfPos).Length();

                    var size = t.Type == MyDetectedEntityType.SmallGrid ? "SM" : "LG";
                    var name = TargetTracker2.GetName(t.EntityId);

                    targetName = friends.Contains(t.Relationship)
                        ? $"{size} — {t.Name}"
                        : $"{size} — {name}";

                    dist = d.ToString("0m");
                }

                var aimbot = "Off";

                if (aimbotTargetShotSpeed > 0)
                {

                    switch (aimbotTargetShotSpeed)
                    {
                        case RAILGUN_SPEED:
                            aimbot = "Rail";
                            break;
                        case ARTILLERY_SPEED:
                            aimbot = "Art";
                            break;
                        default:
                            aimbot = aimbotTargetShotSpeed.ToString("0 m/s");
                            break;
                    }

                    switch (lastAimbotState)
                    {
                        case AimbotState.READY:
                            aimbot += $" :: READY";
                            break;
                        case AimbotState.TOO_CLOSE:
                            aimbot += $" :: TOO CLOSE";
                            break;
                    }
                }

                var rg = railguns.Where(r => r.IsWorking).ToArray();
                int rgReadyCount = 0;
                float rgPercent = 0;

                for (int i = 0; i < rg.Length; i++)
                {
                    var value = GetRailgunChargeLevel(rg[i]);

                    if (value > 0.99) { rgReadyCount++; }

                    if (value < 0.99 && value > rgPercent) { rgPercent = value; }
                }

                var tCount = torpedos.Values.Count(t => t.Stage == LaunchStage.Ready);
                var enemyLock = false;

                if (this.enemyLock.HasValue)
                {
                    var diff = (now - this.enemyLock.Value);
                    enemyLock = diff.TotalSeconds > 4 || diff.Milliseconds < 600;
                }

                var sprites = GetHudState(
                    targetName, dist, aimbot,
                    tCount, enemyLock,
                    rg.Length, rgReadyCount, rgPercent);

                foreach (var lcd in hud)
                {
                    using (var frame = lcd.DrawFrame())
                    {
                        frame.AddRange(sprites);
                    }
                }

                lastUpdateHUD = now;
            }
        }

        private MySprite[] GetHudState(
            string targetName, string dist, string aimbot, int tCount, bool enemyLock,
            int rgTotal, int rgReady, float rgChargeLevel)
        {
            var list = new List<MySprite>();

            // target

            list.AddRange(Text("target", targetName, TextAlignment.CENTER, TOP));

            if (dist != null)
            {
                list.AddRange(Text("dist", dist, TextAlignment.LEFT, TOP));
            }


            // torpedo count
            list.AddRange(Text("torpedos", tCount.ToString(), TextAlignment.RIGHT, TOP));


            // aimbot
            list.AddRange(Text("aimbot", aimbot, TextAlignment.LEFT, BOTTOM));


            // enemy lock
            if (enemyLock)
            {
                list.AddRange(Text(null, "ENEMY LOCK", TextAlignment.CENTER, BOTTOM, Color.OrangeRed));
            }

            // railguns
            if (rgTotal > 0)
            {
                list.AddRange(Text("railgun", $"{rgReady} / {rgTotal}", TextAlignment.RIGHT, BOTTOM));

                list.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(452, 509),
                    Size = new Vector2(Convert.ToInt32(60 * rgChargeLevel), 3),
                    Color = Color.Teal,
                });
            }

            return list.ToArray();
        }

        const int TOP = 0;
        const int BOTTOM = 9;
        const int LABEL_HEIGHT = 20;
        const int VALUE_HEIGHT = 30;
        const int LINE_HEIGHT = 51; // label + value + space

        private MySprite[] Text(string label, string text, TextAlignment alignment, byte line = TOP, Color? color = null)
        {
            int x = 0;

            switch (alignment)
            {
                case TextAlignment.LEFT:
                    x = 0;
                    break;
                case TextAlignment.RIGHT:
                    x = 512;
                    break;
                case TextAlignment.CENTER:
                    x = 256;
                    break;
            }

            int y = line * LINE_HEIGHT;

            var valueSprite = new MySprite() // text
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(x, y + LABEL_HEIGHT),
                RotationOrScale = 1f,
                Color = color ?? Color.White,
                Alignment = alignment,
                FontId = "White"
            };

            if (string.IsNullOrEmpty(label))
            {
                return new[] { valueSprite };
            }

            return new[] {
                new MySprite() // label
                {
                    Type = SpriteType.TEXT,
                    Data = label,
                    Position = new Vector2(x, y),
                    RotationOrScale = 0.8f,
                    Color = Color.Teal,
                    Alignment = alignment,
                    FontId = "White"
                },
                valueSprite
            };
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

        private void FixTargetIndex()
        {
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

        private float GetRailgunChargeLevel(IMySmallMissileLauncherReload railgun, float max = 500)
        {
            // возвращает число от 0 до 1
            if (railgun.BlockDefinition.SubtypeId != "LargeRailgun")
            {
                return 0f;
            }

            var lines = railgun.DetailedInfo.Split('\n');
            var chargeInfo = lines[1];

            // parse number
            var start = 0;

            while (!char.IsDigit(chargeInfo[start]))
            {
                start++;
            }

            var end = start + 1;

            while (char.IsDigit(chargeInfo[end]) || chargeInfo[end] == '.')
            {
                end++;
            }

            var strValue = chargeInfo.Substring(start, end - start);
            var current = Convert.ToSingle(strValue);

            float result = current / max;

            return result;
        }
    }

    #endregion
}
