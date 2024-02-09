using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Scripts.Torpedos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Input;

namespace SpaceEngineers.Lib.Torpedos
{
    #region Copy

    // import:BaseTorpedo.cs

    public class SpaceTorpedo : BaseTorpedo
    {
        const double INTERCEPT_DISTANCE = 1200;

        public SpaceTorpedo(IMyBlockGroup group, int delay = 2000, float factor = 7, int lifespan = 360) : base(group, delay, factor, lifespan)
        {
        }

        protected override void SetDirection(TargetInfo targetInfo, double distance)
        {
            var targetPos = targetInfo.GetHitPosWorld();

            if (distance < INTERCEPT_DISTANCE)
            {
                // при большом расстоянии до цели точка перехвата далеко перемещается
                // при маневрах цели, поэтому рассчитываем её только на дальности до 1 км
                tControl.Intercept(targetPos, targetInfo.Entity.Velocity);
            }
            else
            {
                tControl.Aim(targetPos);
            }
        }
    }

    #endregion
}
