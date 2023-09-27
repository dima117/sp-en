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
    public class DirectionController
    {
        public const double MIN_SPEED = 50;

        IMyShipController remoteControl;

        public DirectionController(IMyShipController remoteControl)
        {
            this.remoteControl = remoteControl;
        }

        public struct Directions
        {
            public double Pitch;
            public double Yaw;
            public double Roll;

            public override string ToString()
            {
                return string.Format("Pitch: {0:0.0}\nYaw: {1:0.0}\nRoll: {2:0.0}", Pitch, Yaw, Roll);
            }
        }

        const double pi2 = Math.PI / 2;

        static double ApplySign(double sign, double value)
        {
            return sign >= 0 ? value : -value;
        }

        //отражение вектора от заданного
        public static Vector3D CustomReflect(Vector3D V1, Vector3D V2)
        {
            var reflectedVector = V1 - 2 * Vector3D.Reject(V1, Vector3D.Normalize(V2));

            var sameDirection = Vector3D.Dot(reflectedVector, V2) > 0;

            return sameDirection ? reflectedVector : -V1;
        }

        public Directions GetNavAngle(Vector3D targetVector)
        {
            var orientation = remoteControl.WorldMatrix;
            var velocity = remoteControl.GetShipVelocities().LinearVelocity;

            // направление на цель с учетом гашения боковой скорости
            var reflectedVector = CustomReflect(velocity, targetVector);

            // нормализованное направление на цель
            var dirTarget = Vector3D.Normalize(reflectedVector);

            // проекции вектора цели на оси
            var prj2forward = Vector3D.Dot(dirTarget, orientation.Forward);

            var prj2up = Vector3D.Dot(dirTarget, orientation.Up);
            var prj2upAbs = Math.Abs(prj2up);

            var prj2right = Vector3D.Dot(dirTarget, orientation.Right);
            var prj2rightAbs = Math.Abs(prj2right);

            // вычисляем углы поворота
            var anglePitch = pi2 - Math.Acos(prj2upAbs);
            var angleYaw = pi2 - Math.Acos(prj2rightAbs);

            // если ракета развернута в обратном направлении от цели
            if (prj2forward < 0)
            {
                // корректируем угол по измерению где больше проекция на перпендикулярный вектор
                if (prj2rightAbs <= prj2upAbs)
                {
                    anglePitch = Math.PI - anglePitch;
                }
                else
                {
                    angleYaw = Math.PI - angleYaw;
                }
            }

            // определяем знак угла
            var pitch = ApplySign(prj2up, anglePitch);
            var yaw = ApplySign(prj2right, angleYaw);

            return new Directions
            {
                Pitch = pitch,
                Yaw = yaw,
            };
        }

        public Directions GetTargetAngle(Vector3D targetPos)
        {
            var ownPos = remoteControl.GetPosition();
            var targetVector = targetPos - ownPos;

            return GetNavAngle(targetVector);
        }

        public Directions GetInterceptAngle(MyDetectedEntityInfo target)
        {
            var ownPos = remoteControl.GetPosition();
            var ownSpeed = Math.Max(remoteControl.GetShipSpeed(), MIN_SPEED);

            var point = Helpers.CalculateInterceptPoint(ownPos, ownSpeed, target.Position, target.Velocity);

            var direction = point == null
                ? new Vector3D(target.Velocity) // если ракета не может догнать цель, то двигаемся параллельно 
                : (point.Position - ownPos); // иначе курс на точку перехвата

            return GetNavAngle(direction);
        }
    }
}
