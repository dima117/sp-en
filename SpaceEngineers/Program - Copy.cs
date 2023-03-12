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

namespace SpaceEngineers
{
    public sealed class Program2 : MyGridProgram
    {
        #region Copy

        // тестирование поворота платформы

        readonly DirectionController dc;
        readonly IMyRemoteControl remote;
        readonly IMyGyro gyro;

        Vector3D pos = new Vector3D(-60242.3522425104, -78766.3192244481, -61360.2694747302);

        public Program2()
        {
            gyro = GridTerminalSystem.GetBlockWithName("GYRO") as IMyGyro;
            remote = GridTerminalSystem.GetBlockWithName("REMOTE") as IMyRemoteControl;
            dc = new DirectionController(remote);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "1":
                    gyro.GyroOverride = true;
                    break;
                case "2":
                    gyro.GyroOverride = false;
                    break;
                default:
                    if (gyro.GyroOverride)
                    {
                        var direction = dc.GetTargetAngle(pos);
                        gyro.Pitch = -Convert.ToSingle(direction.Pitch) * 3;
                        gyro.Yaw = Convert.ToSingle(direction.Yaw) * 3;
                    }
                    break;
            }
        }
        #endregion
    }
}
