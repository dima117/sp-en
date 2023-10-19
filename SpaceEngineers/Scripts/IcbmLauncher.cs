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

        string BROAD_CAST_TAG = "start_icbm";
        string ECHO_TAG = "icbm_was_launched";

        IMyRadioAntenna antenna;
        List<Icbm> missiles = new List<Icbm>();
        IMyBroadcastListener listener;

        IMyTextSurface LCD => Me.GetSurface(0);

        public Program()
        {
            antenna = GridTerminalSystem.GetBlockWithName("ANTENNA") as IMyRadioAntenna;
            antenna.Radius = 10;
            antenna.Enabled = true;

            listener = IGC.RegisterBroadcastListener(BROAD_CAST_TAG);
            listener.SetMessageCallback();

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.IGC)
            {
                while (listener.HasPendingMessage)
                {
                    var message = listener.AcceptMessage();

                    if (StartNextMissile(message.Data)) {
                        antenna.Radius = 50000;
                        IGC.SendUnicastMessage(message.Source, ECHO_TAG, message.Data);
                        antenna.Radius = 10;
                    }
                }
            }
            else
            {
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
                LCD.WriteText(string.Join("\n\n", missiles));
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
