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

        public Directions GetNavAngle(Vector3D targetPos)
        {
            // TODO: проверить, можно ли считать проекцию векторов без промежуточного вектора
            // TODO: сделать методы: 1) выравнивание P+Y на точку, 2) выравниание P+R по вектору гравитации
            var myPos = remoteControl.GetPosition();
            var gravity = remoteControl.GetNaturalGravity();
            var forward = remoteControl.WorldMatrix.Forward;
            var up = remoteControl.WorldMatrix.Up;
            var left = remoteControl.WorldMatrix.Left;

            var targetVector = targetPos - myPos;

            var targetPitch = Math.Acos(
                Vector3D.Dot(up, Vector3D.Normalize(Vector3D.Reject(targetVector, left)))
            ) - Math.PI / 2;

            var targetYaw = Math.Acos(
                Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(targetVector, up)))
            ) - Math.PI / 2;

            var targetRoll = Math.Acos(
                Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(gravity, forward)))
            ) - Math.PI / 2;

            return new Directions()
            {
                Pitch = targetPitch,
                Yaw = targetYaw,
                Roll = targetRoll,
            };
        }
    }
}
