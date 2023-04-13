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

namespace SpaceEngineers
{
    public class GravityDrive
    {
        readonly IMyTextPanel lcd;

        readonly IMyShipController controller;
        readonly List<IMyArtificialMassBlock> massBlocks = new List<IMyArtificialMassBlock>();
        readonly List<IMyGyro> gyroBlocks = new List<IMyGyro>();

        readonly List<IMyGravityGenerator> upGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> downGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> leftGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> rightGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> fowardGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> backwardGens = new List<IMyGravityGenerator>();

        const float GRAVITY_RATIO = 9.8f;
        const float DAMPENERS_RATIO = 0.1f;
        const float ROTATION_RATIO = 100f;

        public GravityDrive(
            IMyShipController controller,
            IMyBlockGroup group,
            IMyTextPanel lcd)
        {
            this.controller = controller;
            this.lcd = lcd;

            group.GetBlocksOfType(gyroBlocks);
            group.GetBlocksOfType(massBlocks);

            var gravGens = new List<IMyGravityGenerator>();
            group.GetBlocksOfType(gravGens);

            foreach (var block in gravGens)
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

        public bool Enabled { get; set; }

        public bool DampenersOverride => controller.DampenersOverride;

        public void Update()
        {
            Vector3 input = controller.MoveIndicator;
            MyShipVelocities velocities = controller.GetShipVelocities();

            var matrix = MatrixD.Transpose(controller.WorldMatrix);

            UpdateGenerators(input, velocities.LinearVelocity, matrix);
            UpdateGyro(velocities.AngularVelocity, matrix);
        }

        private void UpdateGyro(Vector3D worldAngularVelocity, MatrixD matrix)
        {
            var localAngularVelocity = Vector3D.TransformNormal(worldAngularVelocity, matrix);

            foreach ( var gyro in gyroBlocks)
            {
                gyro.Enabled = Enabled;
                gyro.Pitch = -Convert.ToSingle(localAngularVelocity.X * ROTATION_RATIO);
                gyro.Yaw = Convert.ToSingle(localAngularVelocity.Y * ROTATION_RATIO);
                gyro.Roll = Convert.ToSingle(localAngularVelocity.Z * ROTATION_RATIO);
            }
        }

        private void UpdateGenerators(Vector3 input, Vector3D worldVelocity, MatrixD matrix)
        {
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
    }
}
