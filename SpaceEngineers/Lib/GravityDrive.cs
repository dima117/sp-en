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

    public class CenterOfMassPosition
    {
        public CenterOfMassPosition(Vector3D local, Vector3D world)
        {
            Local = local;
            World = world;
        }

        public readonly Vector3D Local;
        public readonly Vector3D World;
    }

    public class CenterOfMass
    {
        public CenterOfMass(CenterOfMassPosition physicalValue, CenterOfMassPosition virtualValue)
        {
            Physical = physicalValue;
            Virtual = virtualValue;
        }

        public readonly CenterOfMassPosition Physical;
        public readonly CenterOfMassPosition Virtual;
    }

    public class GravityDrive
    {
        private bool enabled;

        readonly IMyShipController controller;
        readonly List<IMyArtificialMassBlock> massBlocks = new List<IMyArtificialMassBlock>();
        readonly List<IMyGyro> gyroBlocks = new List<IMyGyro>();

        // сферические генерации нужно ставить по краям, направляя верх в сторону блоков массы
        // например, генераторы сзади, верх которых направлен вперед, будут усиливать движение вперед/назад
        readonly List<IMyGravityGeneratorBase> allGens = new List<IMyGravityGeneratorBase>();
        readonly List<IMyGravityGeneratorBase> upGens = new List<IMyGravityGeneratorBase>();
        readonly List<IMyGravityGeneratorBase> downGens = new List<IMyGravityGeneratorBase>();
        readonly List<IMyGravityGeneratorBase> leftGens = new List<IMyGravityGeneratorBase>();
        readonly List<IMyGravityGeneratorBase> rightGens = new List<IMyGravityGeneratorBase>();
        readonly List<IMyGravityGeneratorBase> forwardGens = new List<IMyGravityGeneratorBase>();
        readonly List<IMyGravityGeneratorBase> backwardGens = new List<IMyGravityGeneratorBase>();

        const float GRAVITY_RATIO = 9.8f;
        const float DAMPENERS_RATIO = 0.1f;
        const float ROTATION_RATIO = 10f;

        public GravityDrive(
            IMyShipController cockpit,
            IMyBlockGroup group)
        {
            this.controller = cockpit;

            group.GetBlocksOfType(gyroBlocks, b => b.IsSameConstructAs(cockpit));
            group.GetBlocksOfType(massBlocks, b => b.IsSameConstructAs(cockpit));
            group.GetBlocksOfType(allGens, b => b.IsSameConstructAs(cockpit));

            foreach (var block in allGens)
            {
                if (cockpit.WorldMatrix.Forward == block.WorldMatrix.Down)
                    forwardGens.Add(block);
                else if (cockpit.WorldMatrix.Backward == block.WorldMatrix.Down)
                    backwardGens.Add(block);
                else if (cockpit.WorldMatrix.Left == block.WorldMatrix.Down)
                    leftGens.Add(block);
                else if (cockpit.WorldMatrix.Right == block.WorldMatrix.Down)
                    rightGens.Add(block);
                else if (cockpit.WorldMatrix.Up == block.WorldMatrix.Down)
                    upGens.Add(block);
                else if (cockpit.WorldMatrix.Down == block.WorldMatrix.Down)
                    downGens.Add(block);
            }
        }

        public CenterOfMass CalculateCenterOfMass()
        {
            // матрица для преобразования в локальные координаты
            var invertedMatrix = MatrixD.Invert(controller.WorldMatrix.GetOrientation());

            // physical
            var centerOfMass = controller.CenterOfMass;
            var localCenterOfMass = Vector3D.Transform(centerOfMass, invertedMatrix);

            // virtual
            var virtualMassPositions = massBlocks.Aggregate(Vector3D.Zero, (a, b) => a + b.GetPosition());
            var virtualCenterOfMass = virtualMassPositions / massBlocks.Count;
            var localVirtualCenterOfMass = Vector3D.Transform(virtualCenterOfMass, invertedMatrix);

            return new CenterOfMass(
                physicalValue: new CenterOfMassPosition(localCenterOfMass, centerOfMass),
                virtualValue: new CenterOfMassPosition(localVirtualCenterOfMass, virtualCenterOfMass)
            );
        }

        public bool Enabled
        {
            get { return enabled; }
            set { ToggleEngine(value); }
        }

        public bool DampenersOverride => controller.DampenersOverride;

        public void Update(bool controlGyros = true)
        {
            MyShipVelocities velocities = controller.GetShipVelocities();

            UpdateGenerators(velocities.LinearVelocity);

            if (controlGyros)
            {
                UpdateGyro(velocities.AngularVelocity);
            }
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
            SetGravityAcceleration(input.Z, localVelocity.Z, backwardGens, forwardGens);
        }

        private bool IsZero(float value) => Math.Abs(value) < 0.00001;

        private void SetGravityAcceleration(float input, float velocity, IList<IMyGravityGeneratorBase> positive, IList<IMyGravityGeneratorBase> negative)
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
