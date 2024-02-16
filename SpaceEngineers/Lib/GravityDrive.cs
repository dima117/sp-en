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
using Sandbox.Game.GameSystems;
using static VRage.Game.MyObjectBuilder_ControllerSchemaDefinition;

namespace SpaceEngineers.Lib
{
    #region Copy

    public class GravityDrive
    {
        private bool enabled;

        readonly IMyShipController controller;
        readonly List<IMyArtificialMassBlock> massBlocks = new List<IMyArtificialMassBlock>();
        readonly List<IMyGyro> gyroBlocks = new List<IMyGyro>();

        readonly List<IMyGravityGenerator> allGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> upGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> downGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> leftGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> rightGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> fowardGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> backwardGens = new List<IMyGravityGenerator>();

        const float GRAVITY_RATIO = 9.8f;
        const float DAMPENERS_RATIO = 0.1f;
        const float ROTATION_RATIO = 10f;

        public GravityDrive(
            IMyShipController controller,
            IMyBlockGroup group)
        {
            this.controller = controller;

            group.GetBlocksOfType(gyroBlocks, b => b.IsSameConstructAs(controller));
            group.GetBlocksOfType(massBlocks, b => b.IsSameConstructAs(controller));
            group.GetBlocksOfType(allGens, b => b.IsSameConstructAs(controller));

            foreach (var block in allGens)
            {
                if (controller.WorldMatrix.Forward == block.WorldMatrix.Down)
                    fowardGens.Add(block);
                else if (controller.WorldMatrix.Backward == block.WorldMatrix.Down)
                    backwardGens.Add(block);
                else if (controller.WorldMatrix.Left == block.WorldMatrix.Down)
                    leftGens.Add(block);
                else if (controller.WorldMatrix.Right == block.WorldMatrix.Down)
                    rightGens.Add(block);
                else if (controller.WorldMatrix.Up == block.WorldMatrix.Down)
                    upGens.Add(block);
                else if (controller.WorldMatrix.Down == block.WorldMatrix.Down)
                    downGens.Add(block);
            }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { ToggleEngine(value); }
        }

        public bool DampenersOverride => controller.DampenersOverride;

        public void Update()
        {
            MyShipVelocities velocities = controller.GetShipVelocities();

            UpdateGenerators(velocities.LinearVelocity);
            UpdateGyro(velocities.AngularVelocity);
        }

        private void UpdateGyro(Vector3D worldAngularVelocity)
        {
            Vector3D rot = worldAngularVelocity * 100 * worldAngularVelocity.LengthSquared();
            rot += controller.WorldMatrix.Right * controller.RotationIndicator.X * ROTATION_RATIO;
            rot += controller.WorldMatrix.Up * controller.RotationIndicator.Y * ROTATION_RATIO;
            rot += controller.WorldMatrix.Backward * controller.RollIndicator * ROTATION_RATIO;

            foreach (var gyro in gyroBlocks)
            {
                gyro.Yaw = (float)rot.Dot(gyro.WorldMatrix.Up);
                gyro.Pitch = (float)rot.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)rot.Dot(gyro.WorldMatrix.Backward);
            }
        }

        private void UpdateGenerators(Vector3D worldVelocity)
        {
            Vector3 input = controller.MoveIndicator;
            MatrixD matrix = MatrixD.Transpose(controller.WorldMatrix);

            Vector3 localVelocity = Vector3D.TransformNormal(worldVelocity, matrix);

            SetGravityAcceleration(input.X, localVelocity.X, rightGens, leftGens);
            SetGravityAcceleration(input.Y, localVelocity.Y, upGens, downGens);
            SetGravityAcceleration(input.Z, localVelocity.Z, backwardGens, fowardGens);
        }

        private bool IsZero(float value) => Math.Abs(value) < 0.00001;

        private void SetGravityAcceleration(float input, float velocity, IList<IMyGravityGenerator> positive, IList<IMyGravityGenerator> negative)
        {
            var value = IsZero(input) && DampenersOverride ? -velocity * DAMPENERS_RATIO : input;
            var enabled = Enabled && !IsZero(value);

            var acceleration = value * GRAVITY_RATIO;

            foreach (var x in positive)
            {
                x.GravityAcceleration = acceleration;
                x.Enabled = enabled;
            }

            foreach (var x in negative)
            {
                x.GravityAcceleration = -acceleration;
                x.Enabled = enabled;
            }
        }

        void ToggleEngine(bool enabled)
        {
            this.enabled = enabled;

            foreach (IMyArtificialMassBlock b in massBlocks)
            {
                b.Enabled = enabled;
            }

            foreach (IMyGravityGenerator b in allGens)
            {
                b.Enabled = enabled;
                b.GravityAcceleration = 0f;
            }

            foreach (IMyGyro b in gyroBlocks)
            {
                b.GyroOverride = enabled;
                b.Yaw = 0f;
                b.Pitch = 0f;
                b.Roll = 0f;
            }
        }
    }

    #endregion
}
