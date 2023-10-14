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

namespace SpaceEngineers
{
    #region Copy

    // import:Helpers.cs

    public class DirectionController2
    {

        public const double MIN_SPEED = 50;

        readonly IMyShipController remoteControl;
        readonly IEnumerable<IMyGyro> gyroList;

        readonly float factor; // множитель мощности гироскопа

        public DirectionController2(
            IMyShipController remoteControl,
            IEnumerable<IMyGyro> gyroList,
            float factor)
        {
            this.remoteControl = remoteControl;
            this.gyroList = gyroList;
            this.factor = factor;
        }

        public void ICBM(Vector3D targetPos)
        {
            var grav = remoteControl.GetNaturalGravity();

            if (!grav.IsZero())
            {
                var ownPos = remoteControl.GetPosition();
                var targetVector = targetPos - ownPos;

                Vector3D direction;

                if (targetVector.Length() > 3500)
                {
                    // компенсируем гравитацию
                    direction = CompensateSideVelocity(grav, targetVector);
                }
                else
                {
                    // на финальном участке пути компенсируем боковую скорость
                    var velocity = remoteControl.GetShipVelocities().LinearVelocity;
                    direction = CompensateSideVelocity(velocity, targetVector, 1.5f);
                }

                var axis = GetAxis(remoteControl.WorldMatrix.Forward, direction);
                SetGyroByAxis(axis);
            }
        }

        public void KeepHorizon()
        {
            var grav = remoteControl.GetNaturalGravity();

            if (!grav.IsZero())
            {
                // вращаем вектор down по направлению к вектору гравитации
                var axis = GetAxis(remoteControl.WorldMatrix.Down, grav);

                SetGyroByAxis(axis);
            }
        }

        public void Aim(Vector3D targetPos)
        {
            var ownPos = remoteControl.GetPosition();
            var velocity = remoteControl.GetShipVelocities().LinearVelocity;

            var targetVector = CompensateSideVelocity(velocity, targetPos - ownPos);
            var axis = GetAxis(remoteControl.WorldMatrix.Forward, targetVector);

            SetGyroByAxis(axis);
        }

        public void Intercept(MyDetectedEntityInfo target)
        {
            var ownPos = remoteControl.GetPosition();
            var velocity = remoteControl.GetShipVelocities().LinearVelocity;
            var ownSpeed = Math.Max(velocity.Length(), MIN_SPEED);

            var point = Helpers.CalculateInterceptPoint(ownPos, ownSpeed, target.Position, target.Velocity);

            var direction = point == null
                ? new Vector3D(target.Velocity) // если ракета не может догнать цель, то двигаемся параллельно 
                : (point.Position - ownPos); // иначе курс на точку перехвата

            var targetVector = CompensateSideVelocity(velocity, direction);
            var axis = GetAxis(remoteControl.WorldMatrix.Forward, targetVector);

            SetGyroByAxis(axis);
        }

        // корректирует направление на цель для компенсации боковой скорости
        public static Vector3D CompensateSideVelocity(Vector3D velocity, Vector3D targetVector, float ratio = 1)
        {
            // вектор боковой скорости = отклонение вектора скорости от вектора на цель
            var sideVelocity = Vector3D.Reject(velocity, Vector3D.Normalize(targetVector));

            var sameDirection = Vector3D.Dot(velocity, targetVector) > 0;

            return sameDirection
                ? velocity - (1 + ratio) * sideVelocity
                : (1 - ratio) * sideVelocity - velocity;
        }

        // получить ось вращения для совмещения векторов
        // длина зависит от угла между векторами
        public static Vector3D GetAxis(Vector3D currentDirection, Vector3D targetDirection)
        {
            var target = Vector3D.Normalize(targetDirection);
            var current = Vector3D.Normalize(currentDirection);

            var axis = target.Cross(current);

            // если угол больше 90 градусов, то нормализуем длину
            if (target.Dot(current) < 0)
            {
                axis = Vector3D.Normalize(axis);
            }

            return axis;
        }

        public void SetGyroByAxis(Vector3D axis)
        {
            // установка скорости вращения вне зависимости от направления установки гироскопа
            foreach (var gyro in gyroList)
            {
                gyro.Yaw = factor * Convert.ToSingle(axis.Dot(gyro.WorldMatrix.Up));
                gyro.Pitch = factor * Convert.ToSingle(axis.Dot(gyro.WorldMatrix.Right));
                gyro.Roll = factor * Convert.ToSingle(axis.Dot(gyro.WorldMatrix.Backward));
            }
        }
    }

    #endregion
}
