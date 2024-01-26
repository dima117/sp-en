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

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:../BlockArray.cs
    // import:Lib/TargetInfo.cs

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

        const int SCAN_DELAY_MS = 60;
        const int SCAN_RETRY_MS = 25;
        const double DISTANCE_SCAN_DEFAULT = 7500;

        static readonly HashSet<MyDetectedEntityType> targetTypes = new HashSet<MyDetectedEntityType> {
            MyDetectedEntityType.SmallGrid,
            MyDetectedEntityType.LargeGrid
        };

        private BlockArray<IMyCameraBlock> camArray;
        private SortedDictionary<long, TargetInfo> targets = new SortedDictionary<long, TargetInfo>();

        public int Count
        {
            get { return camArray.Count; }
        }

        public double TotalRange
        {
            get
            {
                return camArray.Aggregate<double>(0, (a, c) => a + c.AvailableScanRange);
            }
        }

        public void UpdateCamArray()
        {
            camArray.UpdateBlocks();
        }

        public TargetTracker2(MyGridProgram program)
        {
            camArray = new BlockArray<IMyCameraBlock>(program, cam =>
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            });
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

            return TargetInfo.CreateTargetInfo(entity, DateTime.UtcNow, camPos);
        }

        public void LockTarget(TargetInfo target)
        {
            var id = target.Entity.EntityId;

            if (!targets.ContainsKey(id) || target.Timestamp > targets[id].Timestamp)
            {
                targets[id] = target;
            }
        }

        public void Merge(TargetInfo[] list)
        {
            foreach (var t in list)
            {
                LockTarget(t);
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

            TargetInfo target = targets.Values.FirstOrDefault(t => t.NextScan < now);

            if (target.Entity.IsEmpty())
            {
                return;
            }

            var timePassed = now - target.Timestamp;

            var scanResult = ScanNextPosition(target, timePassed);

            if (scanResult.IsEmpty())
            {
                if (timePassed.TotalSeconds > 2)
                {
                    // если последнее успешное сканирование было больше 2 секунд назад
                    ReleaseTarget(target.Entity.EntityId);
                }
                else
                {
                    target.UpdateNextScan(now.AddMilliseconds(SCAN_RETRY_MS));
                }
            }
            else
            {
                target.Update(scanResult, now, now.AddMilliseconds(SCAN_DELAY_MS));
            }
        }

        private static Vector3D CalculateTargetLocation(TargetInfo info, TimeSpan timePassed)
        {
            var target = info.Entity;

            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return target.Position +
                (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds)) +
                Vector3D.Transform(info.HitPos, target.Orientation);
        }

        private MyDetectedEntityInfo ScanNextPosition(TargetInfo prevTarget, TimeSpan timePassed)
        {
            // вычисляем новую позицию цели
            var calculatedTargetPos = CalculateTargetLocation(prevTarget, timePassed);

            var camera = camArray.GetNext(cam => cam.CanScan(calculatedTargetPos));

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

                var type = t.Type;
                var name = GetName(t.EntityId);
                var dist = (t.Position - selfPos).Length();
                var speed = t.Velocity.Length();

                sb.AppendLine($"{type} {name} {dist:0}m {speed:0}m/s");
            }

            return sb.ToString();
        }

    }

    #endregion
}
