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
using System.Reflection;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:Serializer.cs
    // import:TargetTracker.cs
    // import:Aimbot.cs
    // import:DirectionController2.cs
    // import:Torpedos\SpaceTorpedo.cs

    public class WeaponController
    {
        const int RAYCAST_DISTANCE = 6500;
        const int TORPEDO_LIFESPAN = 600;
        const int BEACON_RADIUS = 70;

        public const int AIMBOT_OFF = 0;
        public const int AIMBOT_RAILGUN_SPEED = 2000;
        public const int AIMBOT_ARTILLERY_SPEED = 500;

        private LocalTime localTime;
        private TargetTracker tracker;
        private IMyTextSurface lcdTorpedos;
        private IMyTextSurface lcdSystem;
        private IMyShipController cockpit;
        private IMyBeacon beacon;
        private IMySoundBlock sound;
        private IMySoundBlock soundEnemyLock;
        private IMyTextPanel[] hud;
        private IMySmallMissileLauncherReload[] railguns;
        private IMySmallMissileLauncher[] artillery;
        private IMyLargeMissileTurret[] turrets;

        private bool courseFiringMode = false;

        private bool enemyLock;

        private DateTime lastUpdateHUD = DateTime.MinValue;

        private int aimbotTargetShotSpeed;
        private AimbotState lastAimbotState = AimbotState.UNKNOWN;
        private DateTime lastAimbotStateUpdated;
        private void SetAimbotState(DateTime now, AimbotState state)
        {
            if (state != lastAimbotState)
            {
                lastAimbotStateUpdated = now;
            }

            lastAimbotState = state;
        }


        public event Action<Exception> OnError;

        readonly Aimbot aimbot;
        readonly Dictionary<long, SpaceTorpedo> torpedos = new Dictionary<long, SpaceTorpedo>();

        public WeaponController(
            LocalTime localTime,
            IMyGyro[] gyros,
            IMyShipController cockpit,
            IMyCameraBlock[] cameras,
            IMyLargeMissileTurret[] turrets,
            IMySmallMissileLauncherReload[] railguns,
            IMySmallMissileLauncher[] artillery,
            IMyTextPanel[] hud,
            IMyTextSurface lcdTorpedos,
            IMyTextSurface lcdSystem,
            IMyIntergridCommunicationSystem igc,
            IMyBeacon beacon,
            IMySoundBlock sound,
            IMySoundBlock soundEnemyLock
        )
        {
            this.localTime = localTime;

            tracker = new TargetTracker(cameras);
            tracker.TargetLocked += Tracker_TargetChanged;
            tracker.TargetReleased += Tracker_TargetChanged;

            this.beacon = beacon;
            this.beacon.Enabled = true;
            this.beacon.Radius = BEACON_RADIUS;

            this.cockpit = cockpit;
            this.lcdTorpedos = lcdTorpedos;
            this.lcdSystem = lcdSystem;
            this.hud = hud;

            this.railguns = railguns;
            this.artillery = artillery;
            this.turrets = turrets;

            this.sound = sound;
            this.soundEnemyLock = soundEnemyLock;

            aimbot = new Aimbot(cockpit, gyros);
        }

        public void Scan(IMyCameraBlock cam)
        {
            var now = localTime.Now;

            var target = TargetTracker.Scan(now, cam, RAYCAST_DISTANCE);

            if (target != null)
            {
                tracker.LockTarget(target);
            }
        }

        public void Reload(IMyBlockGroup[] groups)
        {
            var now = localTime.Now;

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
                if (!t.Value.IsAlive(now))
                {
                    torpedos.Remove(t.Key);
                }
            }
        }

        public void ToggleAimbot()
        {
            var now = localTime.Now;

            switch (aimbotTargetShotSpeed)
            {
                case AIMBOT_OFF:
                    aimbotTargetShotSpeed = AIMBOT_ARTILLERY_SPEED;
                    SetAimbotState(now, aimbot.Reset());
                    break;
                case AIMBOT_ARTILLERY_SPEED:
                    aimbotTargetShotSpeed = AIMBOT_RAILGUN_SPEED;
                    SetAimbotState(now, aimbot.Reset());
                    break;
                default:
                    aimbotTargetShotSpeed = AIMBOT_OFF;
                    break;
            }
        }

        public void SetEnemyLock()
        {
            enemyLock = true;
            soundEnemyLock?.Play();
        }

        public void ClearEnemyLock()
        {
            enemyLock = false;
            soundEnemyLock?.Stop();
        }

        public void ToggleFiringMode()
        {
            courseFiringMode = !courseFiringMode;

            foreach (var t in turrets)
            {
                if (courseFiringMode)
                {
                    t.Range = 0;
                    t.EnableIdleRotation = false;
                    t.SyncEnableIdleRotation();
                }
                else
                {
                    t.Range = 1000;
                    t.EnableIdleRotation = true;
                    t.SyncEnableIdleRotation();
                }
            }

        }

        public bool Launch()
        {
            var now = localTime.Now;

            // запускает одну из готовых к запусу торпед
            var torpedo = torpedos.Values.FirstOrDefault(t => t.GetStage(now) == LaunchStage.Ready);

            if (torpedo == null)
            {
                return false;
            }

            torpedo.Start(now);

            return true;
        }

        private bool Exec(Action action)
        {
            try { action(); }
            catch (Exception e) { OnError(e); }
            return true;
        }

        private IEnumerator<bool> GetEventLoop()
        {
            while (true)
            {
                // обновлям данные о цели
                yield return Exec(() => tracker.Update(localTime.Now));

                // обновляем наведение курсовых орудий
                yield return Exec(() => UpdateAimbot(localTime.Now));

                // обновлям данные о цели (повторно)
                yield return Exec(() => tracker.Update(localTime.Now));

                // обновляем цели торпед
                yield return Exec(() => UpdateTorpedoTargets(localTime.Now));

                // обновляем содержимое экранов
                yield return Exec(() =>
                {
                    var selfPos = cockpit.GetPosition();

                    UpdateHUD(localTime.Now, selfPos);
                    UpdateLcdSystem();
                });
            }
        }

        private IEnumerator<bool> actions = null;

        public void UpdateNext()
        {
            var a = actions ?? (actions = GetEventLoop());

            if (!a.MoveNext())
            {
                a.Dispose();
                actions = null;
            }
        }

        public bool AimbotIsActive
        {
            get
            {
                return aimbotTargetShotSpeed > 0 && tracker.Current != null;
            }
        }

        private void UpdateAimbot(DateTime now)
        {
            if (courseFiringMode)
            {
                foreach (var t in turrets)
                {
                    t.Range = 0;
                    t.SetManualAzimuthAndElevation(0, 0);
                    t.SyncAzimuth();
                    t.SyncElevation();
                }
            }

            if (aimbotTargetShotSpeed > 0)
            {
                var target = tracker.Current;

                if (target != null)
                {
                    SetAimbotState(now, aimbot.Aim(target, aimbotTargetShotSpeed, now));

                    if (lastAimbotState == AimbotState.READY &&
                       (now - lastAimbotStateUpdated).TotalMilliseconds > 500)
                    {
                        IMyUserControllableGun[] list = null;
                        IMyLargeMissileTurret[] listTurrets = null;

                        switch (aimbotTargetShotSpeed)
                        {
                            case AIMBOT_RAILGUN_SPEED:
                                list = railguns.Where(r => r.IsWorking).ToArray();
                                break;
                            case AIMBOT_ARTILLERY_SPEED:
                                list = artillery.Where(r => r.IsWorking).ToArray();

                                if (courseFiringMode)
                                {
                                    listTurrets = turrets.Where(r => r.IsWorking
                                        && r.Azimuth < 0.001
                                        && r.Elevation < 0.001).ToArray();
                                }

                                break;
                        }

                        if (list != null && list.Any())
                        {
                            foreach (var r in list)
                            {
                                r.ShootOnce();
                            }
                        }

                        if (listTurrets != null && listTurrets.Any())
                        {
                            foreach (var r in listTurrets)
                            {
                                r.ShootOnce();
                            }
                        }
                    }
                }
            }
        }

        private void UpdateTorpedoTargets(DateTime now)
        {
            var sb = new StringBuilder();

            var target = tracker.Current;

            foreach (var t in torpedos.Values)
            {
                var state = t.Update(now, target);
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

        private void UpdateHUD(DateTime now, Vector3D selfPos)
        {
            if ((now - lastUpdateHUD).TotalMilliseconds > 100)
            {
                string targetName = null;
                string dist = null;

                if (tracker.Current != null)
                {
                    var t = tracker.Current.Entity;
                    var d = (t.Position - selfPos).Length();

                    var size = t.Type == MyDetectedEntityType.SmallGrid ? "SM" : "LG";
                    var name = TargetTracker.GetName(t.EntityId);

                    targetName = friends.Contains(t.Relationship)
                        ? $"{size} ∙ {t.Name}"
                        : $"{size} ∙ {name}";

                    dist = d.ToString("0m");
                }

                var tc = this.turrets.Count(t => t.IsWorking);
                var tm = courseFiringMode ? "Fwd" : "Auto";
                var turrets = tc > 0 ? $"{tc} ∙ {tm}" : tm;

                var aimbot = "Off";

                if (aimbotTargetShotSpeed > 0)
                {
                    switch (aimbotTargetShotSpeed)
                    {
                        case AIMBOT_RAILGUN_SPEED:
                            aimbot = "Rail";
                            break;
                        case AIMBOT_ARTILLERY_SPEED:
                            aimbot = "Art";
                            break;
                        default:
                            aimbot = aimbotTargetShotSpeed.ToString("0 m/s");
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

                var tCount = torpedos.Values.Count(t => t.GetStage(now) == LaunchStage.Ready);
                var sprites = GetHudState(
                    targetName, dist, aimbot, turrets,
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

                var ti = targetName == null ? "NO TARGET" : $"{targetName} | {dist}";
                var p = rgPercent * 100;
                var rp = p > 0 ? $" ∙ {p:0}%" : "";
                beacon.HudText = $"{ti}\n{aimbot} | {tm} | Rail: {rgReadyCount} {rp}";
            }
        }

        private MySprite[] GetHudState(
            string targetName, string dist, string aimbot, string turrets, int tCount, bool enemyLock,
            int rgTotal, int rgReady, float rgChargeLevel)
        {
            var list = new List<MySprite>();

            // target

            list.AddRange(Text("target", targetName ?? "NO TARGET", TextAlignment.CENTER, TOP));

            if (dist != null)
            {
                list.AddRange(Text("dist", dist, TextAlignment.LEFT, TOP));
            }


            // torpedo count
            list.AddRange(Text("torpedos", tCount.ToString(), TextAlignment.RIGHT, TOP));


            // aimbot
            list.AddRange(Text("aimbot", aimbot, TextAlignment.CENTER, BOTTOM));
            list.AddRange(Text("turrets", turrets, TextAlignment.LEFT, BOTTOM));


            // enemy lock
            if (enemyLock)
            {
                list.AddRange(Text(null, "ENEMY LOCK", TextAlignment.CENTER, BOTTOM - 1, color: Color.OrangeRed));
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
        const int CELL_WIDTH = 128;

        private MySprite[] Text(
            string label,
            string text,
            TextAlignment alignment,
            byte line = TOP,
            byte offset = 0,
            Color? color = null)
        {
            int x = 0;

            switch (alignment)
            {
                case TextAlignment.LEFT:
                    x = 0 + offset * CELL_WIDTH;
                    break;
                case TextAlignment.RIGHT:
                    x = 512 - offset * CELL_WIDTH;
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
            var sb = new StringBuilder();
            sb.AppendLine($"Total range: {tracker.TotalRange:0.0}");
            sb.AppendLine($"Cam count: {tracker.Count}");

            lcdSystem?.WriteText(sb);
        }

        private void Tracker_TargetChanged()
        {
            sound?.Play();
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
