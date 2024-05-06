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

        public Vector3D LastError
        {
            get { return this.lastError; }
        }

        public PID(double kp, double ki, double kd)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
        }

        protected virtual Vector3D GetIntegral(Vector3D currentError, Vector3D errorSum, double dt)
        {
            return errorSum + currentError * dt;
        }

        public Vector3D Control(Vector3D axis, DateTime now, bool proportionalOnly = false)
        {
            var Sp = Kp * axis;

            if (proportionalOnly)
            {
                firstRun = true;
                return Sp;
            }

            if (firstRun)
            {
                errorSum = Vector3D.Zero;
                lastError = Vector3D.Zero;
                lastErrorTimestamp = now;
                firstRun = false;
            }

            var dt = (now - lastErrorTimestamp).TotalSeconds;

            // compute derivative term
            Vector3D errorDerivative = (axis - lastError) / dt;

            // get error sum
            errorSum = GetIntegral(axis, errorSum, dt);

            // store this error as last error
            lastError = axis;
            lastErrorTimestamp = now;

            // construct output
            return Sp + Ki * errorSum + Kd * errorDerivative;
        }

        public virtual void Reset()
        {
            firstRun = true;
        }
    }

    public class DecayingIntegralPID : PID
    {
        public double IntegralDecayRatio { get; set; }

        public DecayingIntegralPID(double kp, double ki, double kd, double decayRatio) : base(kp, ki, kd)
        {
            IntegralDecayRatio = decayRatio;
        }

        protected override Vector3D GetIntegral(Vector3D currentError, Vector3D errorSum, double timeStep)
        {
            return (1.0 - IntegralDecayRatio) * errorSum + currentError * timeStep;
        }
    }

    public enum AimbotState
    {
        UNKNOWN,
        READY,
        TOO_CLOSE
    }

    public class Aimbot
    {
        private const double PROPORTIONAL_MODE_LIMIT = 0.07;
        private const double MIN_DISTANCE = 600;
        private const double ACCURACY_LIMIT = 0.012;

        private readonly PID pid = new DecayingIntegralPID(1, 1, 0, 0);

        private readonly IMyShipController remoteControl;
        private readonly IEnumerable<IMyGyro> gyroList;

        public Aimbot(IMyShipController remoteControl, IEnumerable<IMyGyro> gyroList)
        {
            this.remoteControl = remoteControl;
            this.gyroList = gyroList;
        }

        public AimbotState Aim(MyDetectedEntityInfo target, double bulletSpeed)
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
            axis = pid.Control(axis, DateTime.UtcNow, axis.Length() > PROPORTIONAL_MODE_LIMIT);

            DirectionController2.SetGyroByAxis(axis, gyroList);

            if (targetVector.Length() < MIN_DISTANCE)
            {
                return AimbotState.TOO_CLOSE;
            }
            else if (pid.LastError.Length() < ACCURACY_LIMIT)
            {
                return AimbotState.READY;
            }

            return AimbotState.UNKNOWN;
        }

        public AimbotState Reset()
        {
            pid.Reset();

            return AimbotState.UNKNOWN;
        }
    }

    #endregion
}
