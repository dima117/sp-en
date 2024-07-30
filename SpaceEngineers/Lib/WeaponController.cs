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
    // import:HUD.cs
    // import:DirectionController2.cs
    // import:Torpedos\SpaceTorpedo.cs

    public class WeaponController
    {
        const int RAYCAST_DISTANCE = 6500;
        const int TORPEDO_LIFESPAN = 600;
        const int BEACON_RADIUS = 70;

        private LocalTime localTime;
        private TargetTracker tracker;
        private IMyTextSurface lcdTorpedos;
        private IMyTextSurface lcdSystem;
        private IMySoundBlock sound;
        private IMySoundBlock soundEnemyLock;
        private IMySmallMissileLauncherReload[] railguns;
        private IMySmallMissileLauncher[] artillery;
        private IMyLargeMissileTurret[] turrets;

        public bool EnemyLock { get; private set; }
        public FiringMode FiringMode { get; private set; }
        public ForwardWeapon? Aimbot { get; private set; }
        public TargetInfo CurrentTarget => tracker.Current;
        public Vector3D? CurrentAiTarget => tracker.CurrentAiTarget;

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

            var tpos = torpedos.Values.Where(t => t.GetStage(now) == LaunchStage.Started).Select(t => t.Position).ToArray();

            return new WeaponState
            {
                RalgunsСharge = rgPercent,
                RalgunsCount = railguns.Length,
                RalgunsReadyCount = rgReadyCount,
                ActiveTorpedos = tpos,
                TorpedosCount = torpedosCount,
                TurretsCount = turretsCount,
                TurretsFiringMode = FiringMode,
            };
        }

        public event Action<Exception> OnError;

        readonly Dictionary<long, SpaceTorpedo> torpedos = new Dictionary<long, SpaceTorpedo>();

        public WeaponController(
            LocalTime localTime,
            IMyCameraBlock[] cameras,
            IMyLargeMissileTurret[] turrets,
            IMySmallMissileLauncherReload[] railguns,
            IMySmallMissileLauncher[] artillery,
            IMyTextSurface lcdTorpedos,
            IMyTextSurface lcdSystem,
            IMySoundBlock sound,
            IMySoundBlock soundEnemyLock,
            IMyOffensiveCombatBlock ai,
            IMyFlightMovementBlock flight
        )
        {
            this.localTime = localTime;

            tracker = new TargetTracker(cameras, ai, flight);
            tracker.TargetLocked += Tracker_TargetChanged;
            tracker.TargetReleased += Tracker_TargetChanged;

            this.lcdTorpedos = lcdTorpedos;
            this.lcdSystem = lcdSystem;

            this.railguns = railguns;
            this.artillery = artillery;
            this.turrets = turrets;

            this.sound = sound;
            this.soundEnemyLock = soundEnemyLock;
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

        public void Scan(Vector3D? targetPos)
        {
            if (targetPos.HasValue)
            {
                tracker.TryLockPosition(localTime.Now, targetPos.Value);
            }
        }

        public void Reload(IMyBlockGroup[] groups)
        {
            var now = localTime.Now;

            foreach (var gr in groups)
            {
                var tmp = new SpaceTorpedo(gr, factor: 2f, lifespan: TORPEDO_LIFESPAN);

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

        public void TryFire()
        {
            if (Aimbot == ForwardWeapon.Artillery && FiringMode == FiringMode.Forward)
            {
                foreach (var t in turrets)
                {
                    t.Range = 0;
                    t.SetManualAzimuthAndElevation(0, 0);
                    t.SyncAzimuth();
                    t.SyncElevation();
                }
            }

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
