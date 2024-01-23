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
using System.Net;

namespace SpaceEngineers.Scripts.RotorTest
{
    public class Program : MyGridProgram
    {
        #region Copy

        // скрипт для отладки учета угловой скорости
        // нужен сотиентированный гироскоп и ротор с тем же направлением up

        // отметка 180 == вперед
        // отметка 90 == влево


        private IMyMotorStator rotorAzimuth;
        private IMyMotorStator rotorElevation;
        private IMyShipController remote;
        private IMyGyro gyro1;
        private IMyCameraBlock cam;

        public Program()
        {
            rotorAzimuth = GridTerminalSystem.GetBlockWithName("AZIMUTH") as IMyMotorStator;
            rotorElevation = GridTerminalSystem.GetBlockWithName("ELEVATION") as IMyMotorStator;
            remote = GridTerminalSystem.GetBlockWithName("REMOTE") as IMyShipController;
            gyro1 = GridTerminalSystem.GetBlockWithName("GYRO1") as IMyGyro;
            cam = GridTerminalSystem.GetBlockWithName("CAMERA") as IMyCameraBlock;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "dump") {
                var pos = rotorAzimuth.GetPosition();

                Me.CustomData =
                    FormatGps(rotorAzimuth.GetPosition(), "Position") + "\n" +
                    FormatGps(pos + rotorAzimuth.WorldMatrix.Forward * 10, "Forward") + "\n" +
                    FormatGps(pos + rotorAzimuth.WorldMatrix.Up * 10, "Up") + "\n" +
                    FormatGps(pos + rotorAzimuth.WorldMatrix.Left * 10, "Left");
            }

            var input = remote.MoveIndicator;

            gyro1.GyroOverride = true;
            gyro1.Yaw = input.X * 2;
            gyro1.Pitch = input.Z;

            var invertedMatrix = MatrixD.Invert(cam.WorldMatrix.GetOrientation());

            var angularVelocity = remote.GetShipVelocities().AngularVelocity;
            var localAngularVelocity = Vector3D.Transform(angularVelocity, invertedMatrix);

            rotorAzimuth.TargetVelocityRad = Convert.ToSingle(localAngularVelocity.Dot(rotorAzimuth.WorldMatrix.Up));
            rotorElevation.TargetVelocityRad = Convert.ToSingle(localAngularVelocity.Dot(rotorElevation.WorldMatrix.Up));
        }


        static string FormatGps(Vector3D v, string name) {
            return $"GPS:{name}:{v.X:0.00}:{v.Y:0.00}:{v.Z:0.00}:#FFFFFF22:";
        }

        #endregion
    }
}


/*

 */