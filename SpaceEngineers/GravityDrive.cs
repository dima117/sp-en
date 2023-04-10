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
        readonly IMyGyro gyro;
        readonly IMyShipController controller;
        readonly List<IMyArtificialMassBlock> massBlocks = new List<IMyArtificialMassBlock>();

        readonly List<IMyGravityGenerator> upGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> downGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> leftGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> rightGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> fowardGens = new List<IMyGravityGenerator>();
        readonly List<IMyGravityGenerator> backwardGens = new List<IMyGravityGenerator>();

        const float GRAVITY_RATIO = 9.8f;
        const float DAMPENERS_RATIO = 0.1f;

        public GravityDrive(
            IMyShipController controller,
            IMyBlockGroup group,
            IMyGyro gyro,
            IMyTextPanel lcd)
        {
            this.controller = controller;
            this.gyro = gyro;
            this.lcd = lcd;

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
            var matrix = MatrixD.Transpose(controller.WorldMatrix);

            MatrixD orientation = controller.WorldMatrix.GetOrientation();
            //controller.Orientation.GetMatrix(out orientation);
            var invertedMatrix = MatrixD.Invert(orientation);

            Vector3 input = controller.MoveIndicator;
            var velocities = controller.GetShipVelocities();

            //var localAngularVelocity = Vector3D.Transform(
            //    velocities.AngularVelocity,
            //    invertedMatrix
            //);

            var localAngularVelocity = orientation.Forward;

            lcd.WriteText($"X: {localAngularVelocity.X}\nY: {localAngularVelocity.Y}\nZ: {localAngularVelocity.Z}");

            Vector3D worldVelocity = velocities.LinearVelocity;
            Vector3 localVelocity = Vector3D.TransformNormal(worldVelocity, matrix);

            SetGravityAcceleration(input.X, localVelocity.X, rightGens, leftGens);
            SetGravityAcceleration(input.Y, localVelocity.Y, upGens, downGens);
            SetGravityAcceleration(input.Z, localVelocity.Z, backwardGens, fowardGens);
        }

        private bool IsNone(float value) => Math.Abs(value) < 0.00001;

        private void SetGravityAcceleration(float input, float velocity, IList<IMyGravityGenerator> positive, IList<IMyGravityGenerator> negative)
        {
            var value = IsNone(input) && DampenersOverride ? -velocity * DAMPENERS_RATIO : input;
            var enabled = Enabled && !IsNone(value);

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
