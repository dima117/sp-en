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

        public IMySmallMissileLauncherReload[] GetLargeRailguns(Func<IMySmallMissileLauncherReload, bool> filter = null)
        {
            return GetBlocksOfType<IMySmallMissileLauncherReload>(
                b => b.BlockDefinition.SubtypeId == "LargeRailgun" &&
                (filter == null || filter(b))).ToArray();
        }

        public IMySoundBlock GetSound(string name, string soundName = "SoundBlockAlert2")
        {
            /*
                SoundBlockLightsOn
                SoundBlockLightsOff
                SoundBlockEnemyDetected
                SoundBlockObjectiveComplete
                SoundBlockAlert1
                SoundBlockAlert2
                SoundBlockAlert3
             */

            var sound = GetBlockWithName<IMySoundBlock>(name);

            if (sound != null)
            {
                sound.Enabled = true;
                sound.SelectedSound = soundName;
                sound.Volume = 1;
                sound.Range = 100;
            }

            return sound;
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
