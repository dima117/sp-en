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

        const double DISTANCE_RESERVE = 50;
        const double DISTANCE_DISPERSION = 25;
        const double DISTANCE_SCAN_DEFAULT = 10000;

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

        static readonly HashSet<MyDetectedEntityType> targetTypes = new HashSet<MyDetectedEntityType> {
            MyDetectedEntityType.SmallGrid,
            MyDetectedEntityType.LargeGrid
        };

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

            return Entity2Target(entity, camPos, onlyEnemies);
        }

        public static TargetInfo? ScanArea(IMyCameraBlock cam, double distance = DISTANCE_SCAN_DEFAULT, bool onlyEnemies = false)
        {
            if (cam == null)
            {
                return null;
            }

            var up = cam.WorldMatrix.Up * DISTANCE_DISPERSION;
            var left = cam.WorldMatrix.Left * DISTANCE_DISPERSION;
            var camPos = cam.GetPosition();

            var targetPos = camPos + distance * cam.WorldMatrix.Forward;

            // сканируем заданную точку
            var target = Entity2Target(cam.Raycast(targetPos), camPos, onlyEnemies);
            if (target.HasValue)
            {
                return target.Value;
            }

            // left
            target = Entity2Target(cam.Raycast(targetPos + left), camPos, onlyEnemies);
            if (target.HasValue)
            {
                return target.Value;
            }

            // right
            target = Entity2Target(cam.Raycast(targetPos - left), camPos, onlyEnemies);
            if (target.HasValue)
            {
                return target.Value;
            }

            // up
            target = Entity2Target(cam.Raycast(targetPos + up), camPos, onlyEnemies);
            if (target.HasValue)
            {
                return target.Value;
            }

            // down
            return Entity2Target(cam.Raycast(targetPos - up), camPos, onlyEnemies);
        }

        private static TargetInfo? Entity2Target(MyDetectedEntityInfo entity, Vector3D camPos, bool onlyEnemies)
        {
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

        private static Vector3D CalculateTargetLocation(TargetInfo info, TimeSpan timePassed)
        {
            var target = info.Entity;

            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return target.Position +
                (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds)) +
                Vector3D.Transform(info.HitPos, target.Orientation);
        }

        public void UpdateCamArray()
        {
            camArray.UpdateBlocks();
        }

        public void LockTarget(TargetInfo target)
        {
            var id = target.Entity.EntityId;

            if (!targets.ContainsKey(id) || target.Timestamp > targets[id].Timestamp)
            {
                targets[id] = target;
            }
        }

        public void Merge(TargetInfo[] array)
        {
            foreach (var t in array)
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

        public void Update() { 

        }

        public string GetDisplayState(Vector3D camPos)
        {
            var sb = new StringBuilder();

            foreach(var target in targets)
            {
                var t = target.Value.Entity;

                var type = t.Type;
                var name = GetName(t.EntityId);
                var dist = (t.Position - camPos).Length();
                var speed = t.Velocity.Length();

                sb.AppendLine($"{type} {name} {dist:0} / {speed:0}");
            }

            return sb.ToString();
        }
    }

    #endregion
}
