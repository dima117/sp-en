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

namespace SpaceEngineers
{
    public class Torpedo
    {
        public readonly string Id = DateTime.UtcNow.Ticks.ToString();

        readonly int delay;
        readonly float factor;

        readonly DirectionController tControl;

        readonly List<IMyGyro> listGyro = new List<IMyGyro>();
        readonly List<IMyThrust> listEngine = new List<IMyThrust>();
        readonly List<IMyArtificialMassBlock> listMass = new List<IMyArtificialMassBlock>();
        readonly IMyRemoteControl tRemote;
        readonly IMyShipMergeBlock tClamp;

        DateTime startTime = DateTime.MaxValue;

        public Vector3D Position => tRemote.GetPosition();
        public double Speed => Started && IsAlive ? tRemote.GetShipSpeed() : 0;
        public bool IsReady => listEngine.Any() && listGyro.Any() && tRemote != null && tClamp != null;
        public bool Started { get; private set; }

        public bool IsAlive =>
            tRemote.IsFunctional &&
            listEngine.All(e => e.IsFunctional && e.CubeGrid.EntityId == tRemote.CubeGrid.EntityId) &&
            listGyro.All(g => g.IsFunctional && g.CubeGrid.EntityId == tRemote.CubeGrid.EntityId);


        public Torpedo(IMyBlockGroup group, int delay = 2000, float factor = 7)
        {
            group.GetBlocksOfType(listGyro);
            group.GetBlocksOfType(listMass);
            group.GetBlocksOfType(listEngine);

            var tmp = new List<IMyTerminalBlock>();
            group.GetBlocks(tmp);

            tClamp = tmp.FirstOrDefault(b => b is IMyShipMergeBlock) as IMyShipMergeBlock;
            tRemote = tmp.FirstOrDefault(b => b is IMyRemoteControl) as IMyRemoteControl;
            tControl = new DirectionController(tRemote);

            this.delay = delay;
            this.factor = factor;
        }

        public void Start()
        {
            startTime = DateTime.UtcNow;

            tClamp.Enabled = false;

            listGyro.ForEach(g => { g.GyroOverride = true; });
            listMass.ForEach(m => { m.Enabled = true; });

            listEngine.ForEach(e =>
            {
                e.Enabled = true;
                e.ThrustOverridePercentage = 1;
            });

            Started = true;
        }

        public void Update(TargetTracker.TargetInfo? info)
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds > delay)
            {
                if (info.HasValue)
                {
                    var target = info.Value.Entity;

                    var d = tControl.GetInterceptAngle(target);
                    //var d = tControl.GetTargetAngle(target.Position);

                    listGyro.ForEach(g =>
                    {
                        g.Pitch = -Convert.ToSingle(d.Pitch) * factor;
                        g.Yaw = Convert.ToSingle(d.Yaw) * factor;
                    });
                }

                listMass.ForEach(m => { m.Enabled = false; });
            }
        }
    }
}
