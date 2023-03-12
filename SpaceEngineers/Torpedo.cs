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

        IMyGyro tGyro;
        IMyThrust tEngine;
        IMyRemoteControl tRemote;
        DirectionController tControl;
        DateTime startTime = DateTime.MaxValue;

        

        public Torpedo(MyGridProgram program, string prefix = "T_", int delay = 1000)
        {
            // TODO: сделать поиск компонентов с фильтрацией по гриду
            // (чтобы можно было пускать несколько торпед)
            tEngine = program.GridTerminalSystem.GetBlockWithName($"{prefix}ENGINE") as IMyThrust;
            tGyro = program.GridTerminalSystem.GetBlockWithName($"{prefix}GYRO") as IMyGyro;
            tRemote = program.GridTerminalSystem.GetBlockWithName($"{prefix}REMOTE") as IMyRemoteControl;
            tControl = new DirectionController(tRemote);

            this.delay = delay;
        }

        public void Start()
        {
            tEngine.Enabled = true;
            tEngine.ThrustOverridePercentage = 100;
            tGyro.GyroOverride = true;
            startTime = DateTime.UtcNow;
        }

        public string Update(MyDetectedEntityInfo target)
        {
            // TODO: придумать, что сделать, если торпеда промахнулась мимо цели
            if (target.IsEmpty() || (DateTime.UtcNow - startTime).TotalMilliseconds < delay)
            {
                return "";
            }

            var myPos = tRemote.GetPosition();
            var mySpeed = tRemote.GetShipVelocities().LinearVelocity.Length();
            var distance = (target.Position - myPos).Length();

            // TODO: сделать учет смещения относительно центра цели
            // TODO: рассчитывать точку перехвата
            // TODO: вынести управление гироскопом в хелпер (управление несколькими, множитель)
            var d = tControl.GetTargetAngle(target.Position);
            tGyro.Pitch = -Convert.ToSingle(d.Pitch);
            tGyro.Yaw = Convert.ToSingle(d.Yaw);

            var sb = new StringBuilder();

            sb.AppendLine("Missile:");
            sb.AppendLine($"- speed: {mySpeed:0.00}");
            sb.AppendLine($"- position: {myPos}");
            sb.AppendLine($"- target distance: {distance:0}");

            sb.AppendLine(d.ToString());

            return sb.ToString();
        }
    }
}
