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
        public SpaceTorpedo(IMyBlockGroup group, int delay = 2000, float factor = 7, int lifespan = 360) : base(group, delay, factor, lifespan)
        {
        }

        protected override void SetDirection(TargetInfo targetInfo)
        {
            tControl.Intercept(targetInfo.GetHitPosWorld(), targetInfo.Entity.Velocity);
        }
    }

    #endregion
}
