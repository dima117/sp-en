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
        public readonly string Id = Guid.NewGuid().ToString("N");

        IMyGyro tGyro;
        IMyThrust tEngine;
        IMyRemoteControl tRemote;
        DirectionController tControl;

        public Torpedo(MyGridProgram program, string prefix = "T_")
        {
            tEngine = program.GridTerminalSystem.GetBlockWithName($"{prefix}ENGINE") as IMyThrust;
            tGyro = program.GridTerminalSystem.GetBlockWithName($"{prefix}GYRO") as IMyGyro;
            tRemote = program.GridTerminalSystem.GetBlockWithName($"{prefix}REMOTE") as IMyRemoteControl;
            tControl = new DirectionController(tRemote);
        }

        public void Start()
        {
            tEngine.ThrustOverridePercentage = 100;
            tGyro.GyroOverride = true;
        }

        public string Update(MyDetectedEntityInfo target)
        {
            if (target.IsEmpty())
            {
                return "";
            }

            var speed = tRemote.GetShipVelocities().LinearVelocity.Length();
            var d = tControl.GetTargetAngle(target.Position);
            tGyro.Pitch = Convert.ToSingle(d.Pitch);
            tGyro.Yaw = Convert.ToSingle(d.Yaw);

            var sb = new StringBuilder();

            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"Speed: {speed:0.00}");
            sb.AppendLine($"Target: {target.Position}");
            sb.AppendLine($"Missile pos: {tRemote.GetPosition()}");
            sb.AppendLine($"Missile forward: {tRemote.WorldMatrix.Forward}");
            sb.AppendLine($"Missile up: {tRemote.WorldMatrix.Up}");
            sb.AppendLine($"Missile left: {tRemote.WorldMatrix.Left}");

            sb.AppendLine(d.ToString());

            return sb.ToString();
        }
    }
}
