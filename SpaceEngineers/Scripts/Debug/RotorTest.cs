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

namespace SpaceEngineers.Scripts.RotorTest
{
    public class Program : MyGridProgram
    {
        #region Copy

        // скрипт для отладки учета угловой скорости
        // нужен сотиентированный гироскоп и ротор с тем же направлением up

        private IMyMotorStator rotorAzimuth;
        private IMyMotorStator rotorElevation;
        private IMyShipController remote;
        private IMyGyro gyro1;
        private IMyGyro gyro2;

        public Program()
        {
            rotorAzimuth = GridTerminalSystem.GetBlockWithName("AZIMUTH") as IMyMotorStator;
            rotorElevation = GridTerminalSystem.GetBlockWithName("ELEVATION") as IMyMotorStator;
            remote = GridTerminalSystem.GetBlockWithName("REMOTE") as IMyShipController;
            gyro1 = GridTerminalSystem.GetBlockWithName("GYRO1") as IMyGyro;
            gyro2 = GridTerminalSystem.GetBlockWithName("GYRO2") as IMyGyro;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var input = remote.MoveIndicator;

            gyro1.GyroOverride = true;
            gyro2.GyroOverride = true;

            gyro1.Yaw = gyro2.Yaw = input.X;
            gyro1.Pitch = gyro2.Pitch = input.Z;

            var invertedMatrix = MatrixD.Invert(rotorAzimuth.WorldMatrix.GetOrientation());
            //var invertedMatrix2 = MatrixD.Invert(rotorElevation.WorldMatrix.GetOrientation());

            var angularVelocity = remote.GetShipVelocities().AngularVelocity;
            var localAngularVelocity = Vector3D.Transform(angularVelocity, invertedMatrix);
            //var localAngularVelocity2 = Vector3D.Transform(angularVelocity, invertedMatrix2);

            rotorAzimuth.TargetVelocityRad = Convert.ToSingle(localAngularVelocity.Dot(rotorAzimuth.WorldMatrix.Up));
            //rotorElevation.TargetVelocityRad = Convert.ToSingle(localAngularVelocity2.Dot(rotorElevation.WorldMatrix.Up));
        }

        #endregion
    }
}
