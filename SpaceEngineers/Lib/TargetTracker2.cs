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
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Collections;
using System.Reflection;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:../BlockArray.cs
    // import:TargetInfo.cs

    public class TargetTracker2
    {
        public static readonly string[] names = new[] {
            "корова",
            "пёс",
            "кролик",
            "конь",
            "медвед",
            "кот",
            "болт",
            "кабан",
            "волк",
            "бобр",
            "жук",
            "zombie",
            "сом",
        };

        public static string GetName(long entityId)
        {
            var name = names[entityId % names.Length];
            var index = entityId % 89;

            return $"{name}-{index}";
        }

        const int MIN_CAM_COUN = 10;
        const int SCAN_DELAY_MS = 60;
        const int SCAN_RETRY_MS = 25;
        const double DISTANCE_SCAN_DEFAULT = 7500;

        static readonly HashSet<MyDetectedEntityType> targetTypes = new HashSet<MyDetectedEntityType> {
            MyDetectedEntityType.SmallGrid,
            MyDetectedEntityType.LargeGrid
        };

        private readonly IMyTurretControlBlock[] turretControllers;
        private readonly IMyLargeTurretBase[] turrets;
        private readonly IMyCameraBlock[] cameras;

        private int camIndex = 0;

        private SortedDictionary<long, TargetInfo> targets = new SortedDictionary<long, TargetInfo>();

        public event Action TargetListChanged;

        public int Count
        {
            get { return cameras.Length; }
        }

        public double TotalRange
        {
            get
            {
                return cameras.Aggregate(0d, (a, c) => a + c.AvailableScanRange);
            }
        }

        public TargetTracker2(
            IMyCameraBlock[] cameras = null,
            IMyLargeTurretBase[] turrets = null,
            IMyTurretControlBlock[] turretControllers = null
            )
        {
            this.cameras = cameras ?? new IMyCameraBlock[0];
            this.turrets = turrets ?? new IMyLargeTurretBase[0];
            this.turretControllers = turretControllers ?? new IMyTurretControlBlock[0];

            foreach (var cam in this.cameras)
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            }
        }

        public static TargetInfo? Scan(
            IMyCameraBlock cam,
            double distance = DISTANCE_SCAN_DEFAULT,
            bool onlyEnemies = false)
        {
            if (cam == null)
            {
                return null;
            }

            var camPos = cam.GetPosition();
            var entity = cam.Raycast(distance);

            if (entity.IsEmpty())
            {
                return null;
            }

            if (!targetTypes.Contains(entity.Type))
            {
                return null;
            }

            if (onlyEnemies && entity.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies)
            {
                return null;
            }

            return TargetInfo.CreateTargetInfo(entity, camPos, DateTime.UtcNow);
        }

        public void LockTarget(TargetInfo target)
        {
            if (AddOrUpdateTarget(target))
            {
                TargetListChanged?.Invoke();
            }
        }

        public void Merge(TargetInfo[] list)
        {
            var changed = false;

            foreach (var t in list)
            {
                changed |= AddOrUpdateTarget(t);
            }

            if (changed)
            {
                TargetListChanged?.Invoke();
            }
        }

        public void ReleaseTarget(long targetId)
        {
            targets.Remove(targetId);
        }

        public void Clear()
        {
            targets.Clear();
        }

        public int TargetCount => targets.Count;

        public TargetInfo[] GetTargets()
        {
            return targets.Values.ToArray();
        }

        public TargetInfo? GetByEntityId(long entityId)
        {
            return targets[entityId];
        }

        public void Update()
        {
            var now = DateTime.UtcNow;
            var nextScan = now.AddMilliseconds(SCAN_DELAY_MS);

            var isChanged = UpdateFromTurrets(now, nextScan);

            if (Count >= MIN_CAM_COUN)
            {
                isChanged |= UpdateOneFromCameras(now, nextScan);
            }

            if (isChanged)
            {
                TargetListChanged?.Invoke();
            }
        }

        private bool AddOrUpdateTarget(TargetInfo target)
        {
            var id = target.Entity.EntityId;

            if (targets.ContainsKey(id))
            {
                if (target.Timestamp > targets[id].Timestamp)
                {
                    // todo: может теряться hitpos
                    targets[id] = target;
                }

                return false;
            }
            else
            {
                targets.Add(id, target);

                return true;
            }
        }

        private bool AddOrUpdateTarget(MyDetectedEntityInfo entity, Vector3D pos, DateTime now, DateTime nextScan)
        {
            if (targets.ContainsKey(entity.EntityId))
            {
                var target = targets[entity.EntityId];

                if (target.Timestamp < now)
                {
                    target.Update(entity, now, nextScan);
                }

                return false;
            }
            else
            {
                targets.Add(entity.EntityId, TargetInfo.CreateTargetInfo(entity, pos, now, nextScan));

                return true;
            }

        }

        private bool UpdateOneFromCameras(DateTime now, DateTime nextScan)
        {
            TargetInfo target = targets.Values.FirstOrDefault(t => t.NextScan < now);

            if (target.Entity.IsEmpty())
            {
                return false;
            }

            var timePassed = now - target.Timestamp;

            var scanResult = ScanNextPosition(target, timePassed);

            if (scanResult.IsEmpty())
            {
                if (timePassed.TotalSeconds > 2)
                {
                    // если последнее успешное сканирование было больше 2 секунд назад
                    ReleaseTarget(target.Entity.EntityId);
                    return true;
                }
                else
                {
                    target.UpdateNextScan(now.AddMilliseconds(SCAN_RETRY_MS));
                }
            }
            else
            {
                target.Update(scanResult, now, nextScan);
            }

            return false;
        }

        private bool UpdateFromTurrets(DateTime now, DateTime nextScan)
        {
            var changed = false;

            foreach (var t in turrets)
            {
                if (!t.Closed && t.HasTarget)
                {
                    var entity = t.GetTargetedEntity();

                    changed |= AddOrUpdateTarget(entity, t.GetPosition(), now, nextScan);
                }
            }

            foreach (var t in turretControllers)
            {
                if (!t.Closed && t.HasTarget)
                {
                    var entity = t.GetTargetedEntity();

                    changed |= AddOrUpdateTarget(entity, t.GetPosition(), now, nextScan);
                }
            }

            return changed;
        }

        private static Vector3D CalculateTargetLocation(TargetInfo info, TimeSpan timePassed)
        {
            var target = info.Entity;

            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return info.GetHitPosWorld() + (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds));
        }

        private MyDetectedEntityInfo ScanNextPosition(TargetInfo prevTarget, TimeSpan timePassed)
        {
            // вычисляем новую позицию цели
            var calculatedTargetPos = CalculateTargetLocation(prevTarget, timePassed);

            var camera = GetNext(cameras, ref camIndex, cam => cam.CanScan(calculatedTargetPos));

            if (camera == null)
            {
                return default(MyDetectedEntityInfo);
            }

            var entity = camera.Raycast(calculatedTargetPos);

            // если не удалось повторно отсканировать ту же цель
            if (entity.EntityId != prevTarget.Entity.EntityId)
            {
                return default(MyDetectedEntityInfo);
            }

            return entity;
        }

        public string GetDisplayState(Vector3D selfPos)
        {
            var sb = new StringBuilder();

            foreach (var target in targets)
            {
                var t = target.Value.Entity;

                var type = t.Type.ToString().Substring(0, 1);
                var name = GetName(t.EntityId);
                var dist = (t.Position - selfPos).Length();
                var speed = t.Velocity.Length();

                sb.AppendLine($"{type} {name} {dist:0}m {speed:0}m/s");
            }

            return sb.ToString();
        }

        public static T GetNext<T>(T[] a, ref int index, Func<T, bool> filter = null)
        {
            if (a.Length == 0)
            {
                return default(T);
            }

            for (var count = 0; count < a.Length; count++)
            {
                index = (index + 1) % a.Length;

                T block = a[index];

                if (filter == null || filter(block))
                {
                    return block;
                }
            }

            return default(T);
        }

    }

    #endregion
}
