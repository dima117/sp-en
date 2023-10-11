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
using Sandbox.Game.GameSystems;
using System.Collections.ObjectModel;

namespace SpaceEngineers
{
    #region Copy

    // import:WcPbApi.cs

    public class WcRadar
    {
        private readonly Dictionary<MyDetectedEntityInfo, float> threats;

        public WcRadar()
        {
            threats = new Dictionary<MyDetectedEntityInfo, float>();
        }

        public void Update() {
            threats.Clear();
            WcPbApi.Instance.GetSortedThreats(threats);
        }

        public MyDetectedEntityInfo[] Threats => threats.Keys.ToArray();

        public bool IsEmpty => !threats.Any();

        public override string ToString()
        {
            var pos = WcPbApi.Instance.pBlock.GetPosition();

            var sb = new StringBuilder();

            foreach (var obj in threats) {
                var entity = obj.Key;
                var distance = (entity.Position - pos).Length();

                sb.AppendFormat("{0} {1} :: D {2:0}m :: S {3:0}m/s\n", entity.Type, entity.EntityId, distance, entity.Velocity.Length());
            }

            return sb.ToString();
        }
    }

    #endregion
}
