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

namespace SpaceEngineers.Examples
{

    #region Copy

    // import:DirectionController2.cs

    public class Icbm
    {
        public readonly string Id = DateTime.UtcNow.Ticks.ToString();

        readonly int delay;
        readonly string name;

        readonly DirectionController2 tControl;

        readonly List<IMyGyro> listGyro = new List<IMyGyro>();
        readonly List<IMyThrust> listEngine = new List<IMyThrust>();
        readonly List<IMyGasGenerator> listH2Gen = new List<IMyGasGenerator>();
        readonly IMyRemoteControl tRemote;
        readonly IMyShipMergeBlock tClamp;

        DateTime startTime = DateTime.MaxValue;

        public Vector3D Position => tRemote.GetPosition();
        public double Speed => Started && IsAlive ? tRemote.GetShipSpeed() : 0;
        public bool IsReady => listEngine.Any() && listGyro.Any() && tRemote != null && tClamp != null;
        public bool Started { get; private set; }
        public Vector3D TargetPos { get; private set; }

        public bool IsAlive =>
            tRemote.IsFunctional &&
            listEngine.All(e => e.IsFunctional && e.CubeGrid.EntityId == tRemote.CubeGrid.EntityId) &&
            listGyro.All(g => g.IsFunctional && g.CubeGrid.EntityId == tRemote.CubeGrid.EntityId);

        public Icbm(
            IMyBlockGroup group,
            int delay = 1000,  // задержка при старте
            float factor = 4)  // коэффициент мощности гироскопа
        {
            name = group.Name;
            this.delay = delay;

            group.GetBlocksOfType(listGyro);
            group.GetBlocksOfType(listEngine);
            group.GetBlocksOfType(listH2Gen);

            var tmp = new List<IMyTerminalBlock>();
            group.GetBlocks(tmp);

            tClamp = tmp.FirstOrDefault(b => b is IMyShipMergeBlock) as IMyShipMergeBlock;
            tRemote = tmp.FirstOrDefault(b => b is IMyRemoteControl) as IMyRemoteControl;
            tControl = new DirectionController2(tRemote, listGyro, factor);
        }

        public void Start(Vector3D targetPos)
        {
            TargetPos = targetPos;
            startTime = DateTime.UtcNow;

            tClamp.Enabled = false;

            listGyro.ForEach(g =>
            {
                g.Enabled = true;
                g.GyroOverride = true;
            });

            listH2Gen.ForEach(g => { g.Enabled = true; });

            listEngine.ForEach(e =>
            {
                e.Enabled = true;
                e.ThrustOverridePercentage = 1;
            });

            Started = true;
        }

        public void Update()
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds > delay)
            {
                tControl.ICBM(TargetPos);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            var dist = (TargetPos - Position).Length();

            sb.AppendLine($"name: {name}");
            sb.AppendLine($"target: {TargetPos}");
            sb.AppendLine($"dist: {dist}m");

            if (IsAlive)
            {
                if (Started)
                {
                    sb.AppendLine($"status: STARTED");
                }
                else if (IsReady)
                {
                    sb.AppendLine($"status: READY");
                }
                else
                {
                    sb.AppendLine($"status: INVALID");
                }
            }
            else
            {
                sb.AppendLine($"status: DEAD");
            }


            return sb.ToString();
        }
    }

    #endregion
}
