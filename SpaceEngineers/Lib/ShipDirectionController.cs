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
using static VRage.Game.MyObjectBuilder_ControllerSchemaDefinition;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:DirectionController2.cs

    public class ShipDirectionController
    {
        const float ROTATION_RATIO = 10f;

        private readonly IMyShipController controller;
        private readonly IEnumerable<IMyGyro> gyroList;

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
            MyShipVelocities velocities = controller.GetShipVelocities();
            Vector3D worldAngularVelocity = velocities.AngularVelocity;

            Vector3D rot = worldAngularVelocity * 100 * worldAngularVelocity.LengthSquared();
            rot += controller.WorldMatrix.Right * controller.RotationIndicator.X * ROTATION_RATIO;
            rot += controller.WorldMatrix.Up * controller.RotationIndicator.Y * ROTATION_RATIO;
            rot += controller.WorldMatrix.Backward * controller.RollIndicator * ROTATION_RATIO;

            foreach (var gyro in gyroList)
            {
                gyro.Yaw = (float)rot.Dot(gyro.WorldMatrix.Up);
                gyro.Pitch = (float)rot.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)rot.Dot(gyro.WorldMatrix.Backward);
            }
        }

        /*
        public AimbotState Aim(TargetInfo targetInfo, double bulletSpeed, DateTime now)
        {
            var target = targetInfo.Entity;

            var ownPos = remoteControl.CenterOfMass;
            var ownVelocity = remoteControl.GetShipVelocities().LinearVelocity;

            var targetPos = targetInfo.GetHitPosWorld();
            var relativeTargetVelocity = target.Velocity - ownVelocity;

            var point = Helpers.CalculateInterceptPoint(ownPos, bulletSpeed, targetPos, relativeTargetVelocity);

            var targetVector = point == null
                ? (targetPos - ownPos) // если снаряд не может догнать цель, то целимся в текущую позицию цели 
                : (point.Position - ownPos); // иначе курс на точку перехвата

            var axis = DirectionController2.GetAxis(remoteControl.WorldMatrix.Forward, targetVector);

            // fix error
            var proportionalMode = axis.Length() > PROPORTIONAL_MODE_LIMIT;
            axis = pid.Control(axis, now, proportionalMode);

            DirectionController2.SetGyroByAxis(axis, gyroList);

            var distance = targetVector.Length();

            if (distance < MIN_DISTANCE)
            {
                return AimbotState.TOO_CLOSE;
            }
            else if (distance > MAX_DISTANCE)
            {
                return AimbotState.TOO_FAR;
            }
            else if (!proportionalMode && pid.LastError.Length() < ACCURACY_LIMIT)
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

        */
    }

    #endregion
}
