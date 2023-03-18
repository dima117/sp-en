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

        IMyGyro tGyro;
        IMyThrust tEngine;
        IMyRemoteControl tRemote;
        DirectionController tControl;
        DateTime startTime = DateTime.MaxValue;

        public Vector3D Position => tRemote.GetPosition();
        public double Speed => tRemote.GetShipSpeed();

        public Torpedo(MyGridProgram program, string prefix = "T_", int delay = 1000, float factor = 10)
        {
            tEngine = program.GridTerminalSystem.GetBlockWithName($"{prefix}ENGINE") as IMyThrust;
            tGyro = program.GridTerminalSystem.GetBlockWithName($"{prefix}GYRO") as IMyGyro;
            tRemote = program.GridTerminalSystem.GetBlockWithName($"{prefix}REMOTE") as IMyRemoteControl;
            tControl = new DirectionController(tRemote);

            this.delay = delay;
            this.factor = factor;
        }

        public void Start()
        {
            tEngine.Enabled = true;
            tEngine.ThrustOverridePercentage = 100;
            tGyro.GyroOverride = true;
            startTime = DateTime.UtcNow;
        }

        public void Update(TargetTracker.TargetInfo? info)
        {
            if (info.HasValue && (DateTime.UtcNow - startTime).TotalMilliseconds > delay) {
                var target = info.Value.Entity;

                var d = tControl.GetInterceptAngle(target);
                //var d = tControl.GetTargetAngle(target.Position);
                tGyro.Pitch = -Convert.ToSingle(d.Pitch) * factor;
                tGyro.Yaw = Convert.ToSingle(d.Yaw) * factor;
            }
        }
    }
}
