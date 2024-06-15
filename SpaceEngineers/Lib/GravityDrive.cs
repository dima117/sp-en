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
        readonly IMyShipController controller;
        readonly List<IMyArtificialMassBlock> massBlocks = new List<IMyArtificialMassBlock>();

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


        bool isActive;

        public GravityDrive(
            IMyShipController cockpit,
            IMyBlockGroup group)
        {
            controller = cockpit;

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

            // init
            SetActiveState(false, forceUpdate: true);

            foreach (var b in allGens)
            {
                b.Enabled = false;
                b.GravityAcceleration = 0f;
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

        public bool DampenersOverride => controller.DampenersOverride;

        public void Update()
        {
            MyShipVelocities velocities = controller.GetShipVelocities();
            Vector3D worldVelocity = velocities.LinearVelocity;

            Vector3 input = controller.MoveIndicator;
            MatrixD matrix = MatrixD.Transpose(controller.WorldMatrix);

            Vector3 localVelocity = Vector3D.TransformNormal(worldVelocity, matrix);

            var isInUse = SetGravityAcceleration(input.X, localVelocity.X, rightGens, leftGens);
            isInUse |= SetGravityAcceleration(input.Y, localVelocity.Y, upGens, downGens);
            isInUse |= SetGravityAcceleration(input.Z, localVelocity.Z, backwardGens, forwardGens);

            SetActiveState(isInUse);
        }

        private void SetActiveState(bool isActive, bool forceUpdate = false)
        {
            if (isActive != this.isActive || forceUpdate)
            {
                this.isActive = isActive;
                foreach (IMyArtificialMassBlock b in massBlocks)
                {
                    b.Enabled = isActive;
                }
            }
        }

        private bool IsZero(float value) => Math.Abs(value) < 0.00001;

        private bool SetGravityAcceleration(float input, float velocity, IList<IMyGravityGeneratorBase> positive, IList<IMyGravityGeneratorBase> negative)
        {
            var value = IsZero(input) && DampenersOverride ? -velocity * DAMPENERS_RATIO : input;
            var isInUse = !IsZero(value);
            var acceleration = isInUse ? value * GRAVITY_RATIO : 0;

            foreach (var x in positive)
            {
                x.GravityAcceleration = acceleration;
                x.Enabled = isInUse;
            }

            foreach (var x in negative)
            {
                x.GravityAcceleration = -acceleration;
                x.Enabled = isInUse;
            }

            return isInUse;
        }
    }

    #endregion
}
