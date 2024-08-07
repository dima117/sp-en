﻿using System;
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

    // import:Helpers.cs

    public class DirectionController2
    {

        public const double MIN_SPEED = 50;
        public const float DEFAULT_FACTOR = 2;

        readonly IMyShipController remoteControl;
        readonly IEnumerable<IMyGyro> gyroList;

        readonly float factor; // множитель мощности гироскопа

        public DirectionController2(
            IMyShipController remoteControl,
            IEnumerable<IMyGyro> gyroList,
            float factor = DEFAULT_FACTOR)
        {
            this.remoteControl = remoteControl;
            this.gyroList = gyroList;
            this.factor = factor;
        }

        public void ICBM(MyDetectedEntityInfo target)
        {
            var grav = remoteControl.GetNaturalGravity();
            var ownPos = remoteControl.GetPosition();
            var velocity = remoteControl.GetShipVelocities().LinearVelocity;
            var ownSpeed = Math.Max(velocity.Length(), MIN_SPEED);
            var point = Helpers.CalculateInterceptPoint(ownPos, ownSpeed, target.Position, target.Velocity);

            var targetPos = point == null ? target.Position : point.Position;

            var targetVector = targetPos - ownPos;

            if (grav.IsZero() || targetVector.Length() < 3500)
            {
                // если нет гравитации или мы на финальном участке пути,
                // то нацеливаемся на заданную точку и компенсируем боковую скорость
                Aim(targetPos);
            }
            else
            {
                // иначе компенсируем гравитацию
                Vector3D direction = CompensateSideVelocity(grav, targetVector);

                var axis = GetAxis(remoteControl.WorldMatrix.Forward, direction);
                SetGyroByAxis(axis, gyroList, factor);
            }
        }

        public void KeepHorizon(Vector3D? grav = null)
        {
            var direction = grav ?? remoteControl.GetNaturalGravity();

            if (!direction.IsZero())
            {
                // вращаем вектор down по направлению к вектору гравитации
                var axis = GetAxis(remoteControl.WorldMatrix.Down, direction);

                SetGyroByAxis(axis, gyroList, factor);
            }
        }

        public Vector3D Aim(Vector3D targetPos)
        {
            var ownPos = remoteControl.GetPosition();
            var velocity = remoteControl.GetShipVelocities().LinearVelocity;

            var targetVector = CompensateSideVelocity(velocity, targetPos - ownPos);
            var axis = GetAxis(remoteControl.WorldMatrix.Forward, targetVector);

            SetGyroByAxis(axis, gyroList, factor);

            return targetPos;
        }

        public Vector3D? Intercept(Vector3D targetPosition, Vector3 targetVelocity)
        {
            var ownPos = remoteControl.GetPosition();
            var velocity = remoteControl.GetShipVelocities().LinearVelocity;
            var ownSpeed = Math.Max(velocity.Length(), MIN_SPEED);

            var interceptPoint = Helpers.CalculateInterceptPoint(ownPos, ownSpeed, targetPosition, targetVelocity);

            // если ракета не может догнать цель, то двигаемся в текущую позицию цели
            var aimingPointPosition = interceptPoint?.Position ?? targetPosition;

            var direction = aimingPointPosition - ownPos;
            var compensatedTargetVector = CompensateSideVelocity(velocity, direction);

            var axis = GetAxis(remoteControl.WorldMatrix.Forward, compensatedTargetVector);
            SetGyroByAxis(axis, gyroList, factor);

            return interceptPoint?.Position;
        }

        public bool InterceptShot(MyDetectedEntityInfo target, double bulletSpeed)
        {
            var ownPos = remoteControl.GetPosition();
            var ownVelocity = remoteControl.GetShipVelocities().LinearVelocity;

            var relativeTargetVelocity = target.Velocity - ownVelocity;

            var point = Helpers.CalculateInterceptPoint(ownPos, bulletSpeed, target.Position, relativeTargetVelocity);

            var targetVector = point == null
                ? (target.Position - ownPos) // если снаряд не может догнать цель, то целимся в текущую позицию цели 
                : (point.Position - ownPos); // иначе курс на точку перехвата

            var axis = GetAxis(remoteControl.WorldMatrix.Forward, targetVector);

            SetGyroByAxis(axis, gyroList, factor);

            return point != null;
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

        public static Vector3D GetAxis(Vector3D currentDirection, Vector3D targetDirection)
        {
            // получить ось вращения для совмещения векторов
            // длина зависит от угла между векторами

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

        public static void SetGyroByAxis(Vector3D axis, IEnumerable<IMyGyro> gyroList, float factor = DEFAULT_FACTOR)
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
