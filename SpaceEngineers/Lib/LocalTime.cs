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
        private double avg = 0;

        public DateTime Now => now;
        public long CurrentTick => currentTick;
        public double Avg => avg;

        public LocalTime(IMyGridProgramRuntimeInfo runtime, DateTime? initial = null)
        {
            this.initial = initial ?? DateTime.MinValue;
            this.runtime = runtime;
        }

        public DateTime Update(UpdateType updateSource)
        {
            if ((updateSource & UpdateType.Update1) == UpdateType.Update1)
            {
                avg = avg * 0.99 + runtime.LastRunTimeMs * 0.01;

                currentTick++;
                offset = TimeSpan.Zero;
            }
            else
            {
                offset += runtime.TimeSinceLastRun;
            }

            now = initial.AddMilliseconds(currentTick * oneTickMs) + offset;

            return now;
        }
    }

    #endregion
}
