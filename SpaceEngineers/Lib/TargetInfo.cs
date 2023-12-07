using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SpaceEngineers.Lib
{
    #region Copy

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

        public static TargetInfo CreateTargetInfo(MyDetectedEntityInfo entity, DateTime timestamp, Vector3D camPos)
        {
            // hitpos
            var relativeHitPos = default(Vector3D);

            if (entity.HitPosition.HasValue)
            {
                var hitPos = entity.HitPosition.Value;

                // ставим метку на 1 метр вперед от точки пересечения
                var correctedHitPos = hitPos + Vector3D.Normalize(hitPos - camPos);
                var invertedMatrix = MatrixD.Invert(entity.Orientation);

                relativeHitPos = Vector3D.Transform(correctedHitPos - entity.Position, invertedMatrix);
            }

            return new TargetInfo(entity, timestamp, 0, relativeHitPos);
        }
    }

    #endregion
}
