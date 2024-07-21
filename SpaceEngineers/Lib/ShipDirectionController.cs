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

    public class ShipDirectionController
    {
        public const int AIMBOT_RAILGUN_SPEED = 2000;
        public const int AIMBOT_ARTILLERY_SPEED = 500;

        const float MAX_ANGULAR_VELOCITY = 30.0f;

        private const double MIN_DISTANCE = 600;
        private const double MAX_DISTANCE = 1900;
        private const double ACCURACY_LIMIT = 0.012;

        const float ROTATION_RATIO = 10.0f;

        public class PID
        {
            private const double PROPORTIONAL_MODE_LIMIT = 0.07;

            public double MaxLength { get; set; }

            public double Kp { get; set; }
            public double Ki { get; set; }
            public double Kd { get; set; }

            private Vector3D errorSum;
            private Vector3D lastError;
            private DateTime lastErrorTimestamp;
            private bool reset = true;

            public Vector3D? LastError
            {
                get
                {
                    if (reset) return null;
                    return lastError;
                }
            }

            public PID(double kp, double ki, double kd, double maxLength)
            {
                Kp = kp;
                Ki = ki;
                Kd = kd;
                MaxLength = maxLength;
            }

            protected virtual Vector3D GetIntegral(Vector3D currentError, Vector3D errorSum, double dt)
            {
                var result = errorSum + currentError * dt;
                var length = result.Length();

                return length < MaxLength ? result : result / length * MaxLength;
            }

            public Vector3D Control(Vector3D axis, DateTime now)
            {
                var Sp = Kp * axis;

                if (Sp.Length() > PROPORTIONAL_MODE_LIMIT)
                {
                    reset = true;
                    return Sp;
                }

                if (reset)
                {
                    errorSum = Vector3D.Zero;
                    lastError = Vector3D.Zero;
                    lastErrorTimestamp = now;
                    reset = false;
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
                reset = true;
            }
        }

        private readonly IMyShipController controller;
        private readonly IEnumerable<IMyGyro> gyroList;

        private readonly PID pid = new PID(1, 1, 0, MAX_ANGULAR_VELOCITY);

        private DateTime? readyTimestamp;
        public event Action OnReadyToShot;

        public ShipDirectionController(IMyShipController controller, IEnumerable<IMyGyro> gyroList)
        {
            this.controller = controller;
            this.gyroList = gyroList;

            foreach (IMyGyro b in gyroList)
            {
                b.GyroOverride = true;
                b.Yaw = 0f;
                b.Pitch = 0f;
                b.Roll = 0f;
            }
        }

        public void Update()
        {
            MatrixD wm = controller.WorldMatrix;
            MyShipVelocities velocities = controller.GetShipVelocities();
            Vector3D worldAngularVelocity = velocities.AngularVelocity;

            Vector3D axis = worldAngularVelocity * 100 * worldAngularVelocity.LengthSquared();
            axis += wm.Right * controller.RotationIndicator.X * ROTATION_RATIO;
            axis += wm.Up * controller.RotationIndicator.Y * ROTATION_RATIO;
            axis += wm.Backward * controller.RollIndicator * ROTATION_RATIO;

            SetGyroByAxis(axis);
        }

        public void Aim(TargetInfo targetInfo, ForwardWeapon weapon, DateTime now)
        {
            MatrixD wm = controller.WorldMatrix;
            var target = targetInfo.Entity;

            double bulletSpeed = GetBulletSpeed(weapon);

            var ownPos = controller.CenterOfMass;
            var ownVelocity = controller.GetShipVelocities().LinearVelocity;

            var targetPos = targetInfo.GetHitPosWorld();
            var relativeTargetVelocity = target.Velocity - ownVelocity;

            var point = Helpers.CalculateInterceptPoint(ownPos, bulletSpeed, targetPos, relativeTargetVelocity);

            var targetVector = point == null
                ? (targetPos - ownPos) // если снаряд не может догнать цель, то целимся в текущую позицию цели 
                : (point.Position - ownPos); // иначе курс на точку перехвата

            var axis = DirectionController2.GetAxis(wm.Forward, targetVector);

            // fix error
            if (Math.Abs(controller.RollIndicator) > 0.001) {
                axis += wm.Backward * controller.RollIndicator * ROTATION_RATIO;
                pid.Reset();
            } else
            {
                axis = pid.Control(axis, now);
            }

            SetGyroByAxis(axis);

            var distance = targetVector.Length();

            if (distance > MIN_DISTANCE && distance < MAX_DISTANCE && pid.LastError.HasValue && pid.LastError.Value.Length() < ACCURACY_LIMIT)
            {
                if (readyTimestamp.HasValue)
                {
                    if ((now - readyTimestamp.Value).TotalMilliseconds > 500)
                    {
                        OnReadyToShot?.Invoke();
                    }
                }
                else
                {
                    readyTimestamp = now;
                }
            }
            else
            {
                readyTimestamp = null;
            }
        }

        private void SetGyroByAxis(Vector3D axis) {
            if (axis.Length() > MAX_ANGULAR_VELOCITY) { 
                axis = Vector3D.Normalize(axis) * MAX_ANGULAR_VELOCITY;
            }

            DirectionController2.SetGyroByAxis(axis, gyroList);
        }

        private double GetBulletSpeed(ForwardWeapon value)
        {
            switch (value)
            {
                case ForwardWeapon.Railgun:
                    return AIMBOT_RAILGUN_SPEED;
                case ForwardWeapon.Artillery:
                    return AIMBOT_ARTILLERY_SPEED;
            }

            throw new Exception();
        }
    }

    #endregion
}
