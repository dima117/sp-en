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
using Sandbox.Game.Entities;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:DirectionController2.cs

    public class PID
    {
        public double Kp { get; set; }
        public double Ki { get; set; }
        public double Kd { get; set; }

        private Vector3D errorSum;
        private Vector3D lastError;
        private DateTime lastErrorTimestamp;
        private bool firstRun = true;
        private long periodMs;

        public PID(double kp, double ki, double kd, long periodMs = 1000)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;

            this.periodMs = periodMs;
        }

        protected virtual Vector3D GetIntegral(Vector3D currentError, Vector3D errorSum, double timeStep)
        {
            return errorSum + currentError * timeStep;
        }

        public Vector3D Control(Vector3D axis, DateTime now)
        {
            var timeStep = (now - lastErrorTimestamp).TotalMilliseconds / periodMs;

            //Compute derivative term
            Vector3D errorDerivative = (axis - lastError) / timeStep;

            if (firstRun)
            {
                errorDerivative = Vector3D.Zero;
                firstRun = false;
            }

            //Get error sum
            errorSum = GetIntegral(axis, errorSum, timeStep);

            //Store this error as last error
            lastError = axis;
            lastErrorTimestamp = now;

            //Construct output
            return Kp * axis + Ki * errorSum + Kd * errorDerivative;
        }

        public virtual void Reset()
        {
            errorSum = Vector3D.Zero;
            lastError = Vector3D.Zero;
            firstRun = true;
        }
    }

    public class DecayingIntegralPID : PID
    {
        public double IntegralDecayRatio { get; set; }

        public DecayingIntegralPID(double kp, double ki, double kd, long periodMs, double decayRatio) : base(kp, ki, kd, periodMs)
        {
            IntegralDecayRatio = decayRatio;
        }

        protected override Vector3D GetIntegral(Vector3D currentError, Vector3D errorSum, double timeStep)
        {
            return errorSum * (1.0 - IntegralDecayRatio) + currentError * timeStep;
        }
    }

    public class Aimbot
    {
        private readonly PID pid = new DecayingIntegralPID(1, 1, 0, 1000, 0);

        private readonly IMyShipController remoteControl;
        private readonly IEnumerable<IMyGyro> gyroList;

        public Aimbot(IMyShipController remoteControl, IEnumerable<IMyGyro> gyroList)
        {
            this.remoteControl = remoteControl;
            this.gyroList = gyroList;
        }

        public Vector3D Aim(MyDetectedEntityInfo target, double bulletSpeed)
        {
            var ownPos = remoteControl.GetPosition();
            var ownVelocity = remoteControl.GetShipVelocities().LinearVelocity;

            var relativeTargetVelocity = target.Velocity - ownVelocity;

            var point = Helpers.CalculateInterceptPoint(ownPos, bulletSpeed, target.Position, relativeTargetVelocity);

            var targetVector = point == null
                ? (target.Position - ownPos) // если снаряд не может догнать цель, то целимся в текущую позицию цели 
                : (point.Position - ownPos); // иначе курс на точку перехвата

            var axis = DirectionController2.GetAxis(remoteControl.WorldMatrix.Forward, targetVector);

            // fix error
            axis = pid.Control(axis, DateTime.UtcNow);

            DirectionController2.SetGyroByAxis(axis, gyroList);

            return axis;
        }

        public void Reset()
        {
            pid.Reset();
        }
    }

    #endregion
}
