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
                return string.Format("Pitch: {0:0.00000}\nYaw: {1:0.00000}\nRoll: {2:0.00000}", Pitch, Yaw, Roll);
            }
        }

        const double pi2 = Math.PI / 2;

        // вектора должны быть нормализованы
        public static double GetAngle(Vector3D direction, Vector3D forward, Vector3D up) { 
            var prj2up = Vector3D.Dot(direction, up);
            var prj2forward = Vector3D.Dot(direction, forward);

            var angle = pi2 - Math.Acos(Math.Abs(prj2up));
            if (prj2forward < 0) {
                angle += pi2;
            }

            return Math.Sign(prj2up) * angle;
        }

        public static Directions GetNavAngle(MatrixD orientation, Vector3D directionForward, Vector3D directionDown = default(Vector3D))
        {
            var dir = Vector3D.Normalize(directionForward);
            var dir2 = Vector3D.Normalize(directionDown.IsZero() ? orientation.Down : directionDown);

            var pitch = GetAngle(dir, orientation.Forward, orientation.Up);
            var yaw = GetAngle(dir, orientation.Forward, orientation.Right);
            var roll = GetAngle(dir2, orientation.Down, orientation.Left);

            return new Directions
            {
                Pitch = pitch,
                Yaw = yaw,
                Roll = roll,
            };
        }

        public Directions GetTargetAngle(Vector3D targetPos)
        {
            var ownPos = remoteControl.GetPosition();
            var gravity = remoteControl.GetNaturalGravity();
            var orientation = remoteControl.WorldMatrix;
            var targetVector = targetPos - ownPos;

            return GetNavAngle(orientation, targetVector, gravity);
        }

        public Directions GetInterceptAngle(double ownSpeed, MyDetectedEntityInfo target)
        {
            var ownPos = remoteControl.GetPosition();
            var orientation = remoteControl.WorldMatrix;

            var point = Helpers.CalculateInterceptPoint(ownPos, ownSpeed, target.Position, target.Velocity);

            var direction = point == null
                ? Vector3D.Normalize(target.Velocity) // если ракета не может догнать цель, то двигаемся параллельно 
                : Vector3D.Normalize(point.Position - ownPos); // иначе крс на точку перехвата

            return GetNavAngle(orientation, direction);
        }
    }
}
