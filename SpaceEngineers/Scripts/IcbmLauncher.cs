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

namespace SpaceEngineers.Scripts.IcbmLauncher
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:Icbm.cs
        // import:Transmitter.cs

        IMyTextSurface lcd => Me.GetSurface(0);

        Transmitter tsm;
        HashSet<long> spotters = new HashSet<long>();
        List<Icbm> missiles = new List<Icbm>();
        DateTime nextUpdate = DateTime.MinValue;

        public Program()
        {
            // антенна
            tsm = new Transmitter(this);
            tsm.Subscribe(Transmitter.TAG_ICBM_CONNECT, Connect, true);
            tsm.Subscribe(Transmitter.TAG_TARGET_POSITION, Launch, true);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        private void Connect(MyIGCMessage message)
        {
            spotters.Add(message.Source);
            nextUpdate = DateTime.MinValue;
        }

        private void Launch(MyIGCMessage message)
        {
            StartNextMissile(message.Data);
            nextUpdate = DateTime.MinValue;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "init":
                    var ids = new HashSet<long>(missiles.Select(t => t.EntityId));
                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("ICBM"));

                    missiles.AddRange(groups
                        .Select(gr => new Icbm(gr))
                        .Where(t => !ids.Contains(t.EntityId)));

                    missiles.RemoveAll(m => !m.IsAlive);

                    break;
                case "start":
                    StartNextMissile(Me.CustomData);

                    break;
            }

            foreach (var m in missiles.Where(m => m.IsAlive && m.Started))
            {
                m.Update();
            }

            // missiles states
            var state = string.Join("\n", missiles);
            lcd.WriteText($"{spotters.Count}\n" + state);

            var now = DateTime.UtcNow;
            if (now > nextUpdate) {
                foreach (var address in spotters) {
                    tsm.Send(Transmitter.TAG_ICBM_STATE, state, address);
                }

                nextUpdate = now.AddSeconds(10);
            }
        }

        private bool StartNextMissile(object value)
        {
            Vector3D target;

            if (Vector3D.TryParse(value.ToString(), out target))
            {
                var missile = missiles.FirstOrDefault(m => m.IsReady && !m.Started);

                if (missile != null && !target.IsZero())
                {
                    missile.Start(target);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
