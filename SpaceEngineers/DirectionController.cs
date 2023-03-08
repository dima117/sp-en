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

        public static Directions GetNavAngle(MatrixD orientation, Vector3D directionForward, Vector3D directionDown = default(Vector3D))

        {
            var forward = orientation.Forward;
            var up = orientation.Up;
            var left = orientation.Left;

            var pitch = Math.Acos(
                Vector3D.Dot(up, Vector3D.Normalize(Vector3D.Reject(directionForward, left)))
            ) - Math.PI / 2;

            var yaw = Math.Acos(
                Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(directionForward, up)))
            ) - Math.PI / 2;

            var roll = Math.Acos(
                Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(directionDown, forward)))
            ) - Math.PI / 2;

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
