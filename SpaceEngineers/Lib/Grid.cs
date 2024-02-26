using Sandbox.Game.GameSystems;
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

        public IMyBlockGroup[] GetBlockGroups(string prefix = "")
        {
            var groups = new List<IMyBlockGroup>();

            system.GetBlockGroups(groups, g => g.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            return groups.ToArray();
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

        public IMyCameraBlock GetCamera(string name)
        {
            var camera = GetBlockWithName<IMyCameraBlock>(name);

            if (camera != null)
            {
                camera.Enabled = true;
                camera.EnableRaycast = true;
            }

            return camera;
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
