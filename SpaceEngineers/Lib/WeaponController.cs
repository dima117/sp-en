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
    // import:HUD.cs
    // import:DirectionController2.cs
    // import:Torpedos\SpaceTorpedo.cs

    public class WeaponController
    {
        const int RAYCAST_DISTANCE = 6500;
        const int TORPEDO_LIFESPAN = 600;
        const int BEACON_RADIUS = 70;

        public const int AIMBOT_RAILGUN_SPEED = 2000;
        public const int AIMBOT_ARTILLERY_SPEED = 500;

        private LocalTime localTime;
        private TargetTracker tracker;
        private IMyTextSurface lcdTorpedos;
        private IMyTextSurface lcdSystem;
        private IMyShipController cockpit;
        private IMySoundBlock sound;
        private IMySoundBlock soundEnemyLock;
        private IMySmallMissileLauncherReload[] railguns;
        private IMySmallMissileLauncher[] artillery;
        private IMyLargeMissileTurret[] turrets;

        public bool EnemyLock { get; private set; }
        public FiringMode FiringMode { get; private set; }
        public ForwardWeapon? Aimbot { get; private set; }
        public TargetInfo CurrentTarget => tracker.Current;

        public WeaponState GetState(DateTime now)
        {
            var railguns = this.railguns.Where(r => r.IsWorking).ToArray();
            int rgReadyCount = 0;
            float rgPercent = 0;

            for (int i = 0; i < railguns.Length; i++)
            {
                var value = GetRailgunChargeLevel(railguns[i]);

                if (value > 0.99) { rgReadyCount++; }

                if (value < 0.99 && value > rgPercent) { rgPercent = value; }
            }

            var torpedosCount = torpedos.Values.Count(t => t.GetStage(now) == LaunchStage.Ready);
            var turretsCount = turrets.Count(t => t.IsWorking);

            return new WeaponState
            {
                RalgunsСharge = rgPercent,
                RalgunsCount = railguns.Length,
                RalgunsReadyCount = rgReadyCount,
                TorpedosCount = torpedosCount,
                TurretsCount = turretsCount,
                TurretsFiringMode = FiringMode,
            };
        }

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
            IMyTextSurface lcdTorpedos,
            IMyTextSurface lcdSystem,
            IMySoundBlock sound,
            IMySoundBlock soundEnemyLock
        )
        {
            this.localTime = localTime;

            tracker = new TargetTracker(cameras);
            tracker.TargetLocked += Tracker_TargetChanged;
            tracker.TargetReleased += Tracker_TargetChanged;

            this.cockpit = cockpit;
            this.lcdTorpedos = lcdTorpedos;
            this.lcdSystem = lcdSystem;

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

            switch (Aimbot)
            {
                case null:
                    Aimbot = ForwardWeapon.Artillery;
                    break;
                case ForwardWeapon.Artillery:
                    Aimbot = ForwardWeapon.Railgun;
                    break;
                default:
                    Aimbot = null;
                    break;
            }

            SetAimbotState(now, aimbot.Reset());
        }

        public void SetEnemyLock()
        {
            EnemyLock = true;
            soundEnemyLock?.Play();
        }

        public void ClearEnemyLock()
        {
            EnemyLock = false;
            soundEnemyLock?.Stop();
        }

        public void ToggleFiringMode()
        {
            FiringMode = FiringMode == FiringMode.Forward ? FiringMode.Auto : FiringMode.Forward;

            foreach (var t in turrets)
            {
                if (FiringMode == FiringMode.Forward)
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
                yield return Exec(() => UpdateLcdSystem());
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
                return Aimbot.HasValue && tracker.Current != null;
            }
        }

        private void UpdateAimbot(DateTime now)
        {
            if (FiringMode == FiringMode.Forward)
            {
                foreach (var t in turrets)
                {
                    t.Range = 0;
                    t.SetManualAzimuthAndElevation(0, 0);
                    t.SyncAzimuth();
                    t.SyncElevation();
                }
            }

            if (Aimbot.HasValue)
            {
                var target = tracker.Current;

                if (target != null)
                {
                    SetAimbotState(now, aimbot.Aim(target, GetBulletSpeed(Aimbot.Value), now));

                    if (lastAimbotState == AimbotState.READY &&
                       (now - lastAimbotStateUpdated).TotalMilliseconds > 500)
                    {
                        IMyUserControllableGun[] list = null;
                        IMyLargeMissileTurret[] listTurrets = null;

                        switch (Aimbot)
                        {
                            case ForwardWeapon.Railgun:
                                list = railguns.Where(r => r.IsWorking).ToArray();
                                break;
                            case ForwardWeapon.Artillery:
                                list = artillery.Where(r => r.IsWorking).ToArray();

                                if (FiringMode == FiringMode.Forward)
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

        private double GetBulletSpeed(ForwardWeapon value)
        {
            switch (value)
            {
                case ForwardWeapon.Railgun:
                    return AIMBOT_RAILGUN_SPEED;
                case ForwardWeapon.Artillery:
                    return AIMBOT_ARTILLERY_SPEED;
            }

            throw new Exception();
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
