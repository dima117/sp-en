using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineers.Lib
{
    #region Copy

    public class LocalTime
    {
        private const double oneTickMs = 1000 / 60;

        private IMyGridProgramRuntimeInfo runtime;
        private DateTime initial;

        private long currentTick = 0;
        private TimeSpan offset = TimeSpan.Zero;
        private DateTime now;

        public DateTime Now => now;
        public long CurrentTick => currentTick;

        public LocalTime(IMyGridProgramRuntimeInfo runtime, DateTime? initial = null)
        {
            this.initial = initial ?? DateTime.MinValue;
            this.runtime = runtime;
        }

        public DateTime Update(UpdateType updateSource)
        {
            switch (updateSource)
            {
                case UpdateType.Update1:
                    currentTick++;
                    offset = TimeSpan.Zero;
                    break;
                case UpdateType.Update10:
                    currentTick += 10;
                    offset = TimeSpan.Zero;
                    break;
                case UpdateType.Update100:
                    currentTick += 100;
                    offset = TimeSpan.Zero;
                    break;
                default:
                    offset += runtime.TimeSinceLastRun;
                    break;
            }

            now = initial.AddMilliseconds(currentTick * oneTickMs) + offset;

            return now;
        }
    }

    #endregion
}
