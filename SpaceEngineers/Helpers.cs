using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SpaceEngineers
{
    #region Copy

    public static class Helpers
    {
        public class InterceptResult
        {
            public Vector3D Position;
            public double TimeMs;
        }

        public static InterceptResult CalculateInterceptPoint(
            Vector3D ownPosition,
            double interceptSpeed,
            Vector3D targetPosition,
            Vector3D targetVelocity)
        {
            // вектор от нас до цели
            Vector3D directionToTarget = Vector3D.Normalize(targetPosition - ownPosition);

            // орт. скорость цели (по направлению от нас)
            double targetSpeedlOrth = Vector3D.Dot(targetVelocity, directionToTarget);
            Vector3D targetVelOrth = directionToTarget * targetSpeedlOrth;

            // танг. скорость цели (вбок от нас)
            Vector3D targetVelTang = targetVelocity - targetVelOrth;
            double targetSpeedlTang = targetVelTang.Length();

            // если targetSpeedlTang > interceptSpeed то перехват невозможен
            // (нужно двигаться с танг. скоростью цели)
            if (targetSpeedlTang >= interceptSpeed)
            {
                return null;
            }

            // считаем, что танг. скорость цели и снаряда должны быть одинаковыми
            // вычисляем ортогональную скорость снаряда
            double missileSpeedTang = targetSpeedlTang;
            double missileSpeedOrth = Math.Sqrt(interceptSpeed * interceptSpeed - missileSpeedTang * missileSpeedTang);

            // если targetSpeedlOrth > missileSpeedOrth то перехват невозможен
            // (нужно двигаться с орт. скоростью, превышающей орт. скорость цели)
            if (targetSpeedlOrth >= missileSpeedOrth)
            {
                return null;
            }

            // время до перехвата = дистанция до цели / суммарную орт. скорость
            double timeS = Vector3D.Distance(ownPosition, targetPosition) / (missileSpeedOrth - targetSpeedlOrth);

            Vector3D point = targetPosition + targetVelocity * timeS;

            return new InterceptResult { Position = point, TimeMs = timeS * 1000 };
        }
    }

    #endregion
}
