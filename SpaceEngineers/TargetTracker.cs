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

namespace SpaceEngineers
{
    public class TargetTracker
    {
        const double DISTANCE_RESERVE = 50;
        const double DISTANCE_SCAN_DEFAULT = 10000;

        public struct TargetInfo
        {
            public readonly MyDetectedEntityInfo Entity;
            public readonly Vector3D HitPos; // смещение точки прицеливания относительно геометрического центра цели
            public readonly DateTime Timestamp; // время последнего обнаружения цели
            public readonly double ScanDelayMs; // время до следующего сканирования

            public TargetInfo(
                MyDetectedEntityInfo entity = default(MyDetectedEntityInfo),
                DateTime timestamp = default(DateTime),
                double scanDelayMs = default(double),
                Vector3D hitPos = default(Vector3D))
            {
                Entity = entity;
                Timestamp = timestamp;
                ScanDelayMs = scanDelayMs;
                HitPos = hitPos;
            }

            public TargetInfo Update(MyDetectedEntityInfo entity, DateTime timestamp, double scanDelayMs)
            {
                return new TargetInfo(entity, timestamp, scanDelayMs, HitPos);
            }
        }

        public static TargetInfo? Scan(IMyCameraBlock cam, double distance = DISTANCE_SCAN_DEFAULT)
        {
            if (cam == null)
            {
                return null;
            }

            var target = cam.Raycast(distance);

            if (target.IsEmpty())
            {
                return null;
            }

            if (target.Type != MyDetectedEntityType.SmallGrid &&
                target.Type != MyDetectedEntityType.LargeGrid)
            {
                return null;
            }

            // hitpos
            var relativeHitPos = default(Vector3D);

            if (target.HitPosition.HasValue)
            {
                var hitPos = target.HitPosition.Value;
                var camPos = cam.GetPosition();

                // ставим метку на 1 метр вперед от точки пересечения
                var correctedHitPos = hitPos + Vector3D.Normalize(hitPos - camPos);
                var invertedMatrix = MatrixD.Invert(target.Orientation);

                relativeHitPos = Vector3D.Transform(correctedHitPos - target.Position, invertedMatrix);
            }

            return new TargetInfo(target, DateTime.UtcNow, 0, relativeHitPos);
        }

        private BlockArray<IMyCameraBlock> camArray;

        public TargetInfo? Current; // последняя захваченная цель

        static Vector3D CalculateTargetLocation(
            TargetInfo info, TimeSpan timePassed)
        {
            var target = info.Entity;

            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return target.Position +
                (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds)) +
                Vector3D.Transform(info.HitPos, target.Orientation);
        }

        public TargetTracker(MyGridProgram program, string prefix = "Camera")
        {
            camArray = new BlockArray<IMyCameraBlock>(program, prefix, cam => cam.EnableRaycast = true);
        }

        public int Count
        {
            get { return camArray.Count; }
        }

        public void UpdateCamArray()
        {
            camArray.UpdateBlocks();
        }

        public void LockOn(TargetInfo target)
        {
            Current = target;
        }

        public void Clear()
        {
            Current = null;
        }

        private TargetInfo? TryGetUpdatedEntity(TargetInfo prevTarget, TimeSpan timePassed, DateTime now)
        {
            // вычисляем новую позицию цели
            var calculatedTargetPos = CalculateTargetLocation(prevTarget, timePassed);

            var camera = camArray.GetNext(cam => cam.CanScan(calculatedTargetPos));

            if (camera == null)
            {
                return null;
            }

            var target = camera.Raycast(calculatedTargetPos);

            // если не удалось повторно отсканировать ту же цель
            if (target.IsEmpty() || target.EntityId != prevTarget.Entity.EntityId)
            {
                return null;
            }

            var camPos = camera.GetPosition();

            var distance = (target.Position - camPos).Length() + DISTANCE_RESERVE;

            // считаем, сколько мс нужно для накопления камерами дистанции до цели
            // камера накапливает дистанцию 2 м/мс
            var scanDelayMs = distance / camArray.Count / 2;

            return prevTarget.Update(target, now, scanDelayMs);
        }

        public void Update()
        {
            // если цель не захвачена, то ничего не делаем
            if (!Current.HasValue)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var prevTarget = Current.Value;
            var timePassed = now - prevTarget.Timestamp;

            // если прошло мало времени, то ничего не делаем
            if (timePassed.TotalMilliseconds < prevTarget.ScanDelayMs)
            {
                return;
            }

            var target = TryGetUpdatedEntity(prevTarget, timePassed, now);

            if (target.HasValue)
            {
                Current = target.Value;
            }
            else if (timePassed.TotalSeconds > 2)
            {
                // если последнее успешное сканирование было больше 2 секунд назад
                Clear();
            }
        }
    }
}
