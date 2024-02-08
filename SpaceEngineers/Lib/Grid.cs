using Sandbox.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SpaceEngineers.Lib
{
    #region Copy

    public class Grid
    {
        readonly IMyGridTerminalSystem system;

        public Grid(IMyGridTerminalSystem system)
        {
            this.system = system;
        }

        public T[] GetBlocksOfType<T>(Func<T, bool> filter = null) where T : class, IMyTerminalBlock
        {
            var list = new List<T>();
            system.GetBlocksOfType(list, filter);


            return list.ToArray();
        }

        public T GetBlockWithName<T>(string name) where T : class, IMyTerminalBlock
        {
            return system.GetBlockWithName(name) as T;
        }

        public T GetByFilterOrAny<T>(Func<T, bool> filter = null, Action<T> init = null) where T : class, IMyTerminalBlock
        {
            var all = new List<T>();
            system.GetBlocksOfType(all, filter);

            T res = null;

            if (filter != null)
            {
                res = all.FirstOrDefault(filter);
            }

            if (res == null)
            {
                res = all.FirstOrDefault();
            }

            if (res != null && init != null)
            {
                init(res);
            }

            return res;
        }

    }

    #endregion
}
