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

    public class TargetInfo
    {
        const int HIT_POINT_DEPTH = 5;
        public MyDetectedEntityInfo Entity { get; private set; }

        // время последнего обнаружения цели
        public DateTime Timestamp { get; private set; }

        // время следующего сканирования
        public DateTime NextScan { get; private set; }

        // смещение точки прицеливания относительно геометрического центра цели
        // в системе координат цели
        public Vector3D HitPosRelative { get; private set; }

        public Vector3D GetHitPosWorld()
        {
            return Entity.Position + Vector3D.Transform(HitPosRelative, Entity.Orientation);
        }

        public TargetInfo(
            MyDetectedEntityInfo entity = default(MyDetectedEntityInfo),
            DateTime timestamp = default(DateTime),
            DateTime nextScan = default(DateTime),
            Vector3D hitPosRelative = default(Vector3D))
        {
            Entity = entity;
            Timestamp = timestamp;
            NextScan = nextScan;
            HitPosRelative = hitPosRelative;
        }

        public TargetInfo Update(MyDetectedEntityInfo entity, DateTime timestamp, DateTime nextScan)
        {
            Entity = entity;
            Timestamp = timestamp;
            NextScan = nextScan;

            return this;
        }

        public void UpdateNextScan(DateTime nextScan)
        {
            NextScan = nextScan;
        }

        public static TargetInfo CreateTargetInfo(MyDetectedEntityInfo entity, Vector3D camPos, DateTime timestamp, DateTime? nextScan = null)
        {
            // hitpos
            var relativeHitPos = default(Vector3D);

            if (entity.HitPosition.HasValue)
            {
                var hitPos = entity.HitPosition.Value;

                // ставим метку на 5 метров вперед от точки пересечения
                var correctedHitPos = hitPos + Vector3D.Normalize(hitPos - camPos) * HIT_POINT_DEPTH;
                var invertedMatrix = MatrixD.Invert(entity.Orientation);

                relativeHitPos = Vector3D.Transform(correctedHitPos - entity.Position, invertedMatrix);
            }

            return new TargetInfo(entity, timestamp, nextScan ?? timestamp, relativeHitPos);
        }
    }

    #endregion
}
