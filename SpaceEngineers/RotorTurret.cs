﻿using System;
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
using VRage.Noise.Modifiers;
using System.ComponentModel;
using static VRageMath.Base6Directions;
using SpaceEngineers.Lib;

namespace SpaceEngineers
{
    #region Copy

    // турель на роторах
    // - поворот по двум осям в сторону цели
    // - учет собственного движения и движения цели
    // - учет собственной угловой скорости

    // import:Helpers.cs

    public class RotorTurret
    {
        const int LAUNCHER_RELOAD_TIME = 1000;
        const int ROTATION_RATIO = 5;
        const int MAX_DISTANCE = 800;
        const float THRESHOLD = 0.2f;

        private IMyMotorStator rotorAzimuth;
        private IMyMotorStator rotorElevationL;
        private IMyMotorStator rotorElevationR;
        private IMyTerminalBlock container;

        private IMyLargeTurretBase designator;
        private IMyShipController control;

        private int nextShotDelay;
        private DateTime nextShotTime = DateTime.MinValue;
        private int nextLauncher = 0;

        private List<IMySmallMissileLauncher> launchers = new List<IMySmallMissileLauncher>();
        private List<IMySmallGatlingGun> gatlings = new List<IMySmallGatlingGun>();

        private double pi2 = Math.PI * 2;

        public bool Enabled { get; set; }
        public bool ShootingEnabled { get; set; }
        public double MinElevationRad { get; set; } = 0;
        public double MaxElevationRad { get; set; } = Math.PI;

        private T GetBlock<T>(IEnumerable<IMyTerminalBlock> list, string prefix = null) where T : class, IMyTerminalBlock
        {
            return list.FirstOrDefault(b => b is T &&
                (string.IsNullOrEmpty(prefix) || b.CustomName.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))) as T;
        }

        public RotorTurret(IMyBlockGroup group)
        {
            // turret base
            var tmp = new List<IMyTerminalBlock>();
            group.GetBlocks(tmp);

            rotorAzimuth = GetBlock<IMyMotorStator>(tmp, "AZIMUTH");
            rotorElevationL = GetBlock<IMyMotorStator>(tmp, "LEFT");
            rotorElevationR = GetBlock<IMyMotorStator>(tmp, "RIGHT");
            container = GetBlock<IMyTerminalBlock>(tmp, "BODY");
            designator = GetBlock<IMyLargeTurretBase>(tmp, "DESIGNATOR");
            control = GetBlock<IMyShipController>(tmp, "PPP");

            // launchers
            group.GetBlocksOfType(launchers);
            group.GetBlocksOfType(gatlings);

            nextShotDelay = launchers.Any() ? (LAUNCHER_RELOAD_TIME / launchers.Count) : 0;
        }

        public void Update()
        {
            // todo: останавливать вращение, если нет цели
            // todo: расчет упреждения
            // todo: собственный радар
            // todo: компенсация угловой скорости
            // todo: определять ориентацию вертикальных роторов

            var invertedMatrix = MatrixD.Invert(rotorAzimuth.WorldMatrix.GetOrientation());

            var target = designator.GetTargetedEntity();

            if (Enabled && !target.IsEmpty())
            {
                var myPos = container.GetPosition();
				// XXX: вычисляем точку упреждения с учетом собственной скорости
                var interceptPos = Helpers.CalculateInterceptPoint(
                    myPos, 198, target.Position, target.Velocity - control.GetShipVelocities().LinearVelocity);

                var targetVector = interceptPos.Position - myPos;
                var targetVectorLocal = Vector3D.Transform(targetVector, invertedMatrix);
                var targetVelocityLocal = Vector3D.Transform(target.Velocity, invertedMatrix);

                bool isAimed = SetDirection(targetVectorLocal, targetVelocityLocal);
                bool isInRange = targetVectorLocal.Length() < MAX_DISTANCE;
                bool isInSector = CheckSector();

                if (ShootingEnabled && isAimed && isInRange && isInSector)
                {
                    TryToShoot();
					// XXX: переделать улеметы на стрельбу по очереди
					// починить равномерность стрельбы ракетами
                    gatlings.ForEach(x => x.Shoot = true);
                }
                else
                {
                    gatlings.ForEach(x => x.Shoot = false);
                }
            }
            else
            {
                rotorAzimuth.TargetVelocityRad = 0;
                rotorElevationL.TargetVelocityRad = 0;
                rotorElevationR.TargetVelocityRad = 0;

                //SetDirection(new Vector3D(0, 0, -1));
            }
        }

        private bool SetDirection(Vector3D targetVectorLocal, Vector3D targetVelocityLocal)
        {
            var invertedMatrix = MatrixD.Invert(rotorAzimuth.WorldMatrix.GetOrientation());

            var azimuth = Math.Atan2(-targetVectorLocal.X, targetVectorLocal.Z);
            var elevation = Math.Asin(targetVectorLocal.Y / targetVectorLocal.Length());

            var azimuthDiff = NormalizeAngle(azimuth - rotorAzimuth.Angle);
            var elevationLDiff = NormalizeAngle(elevation - rotorElevationL.Angle);
            var elevationRDiff = NormalizeAngle(-elevation - rotorElevationR.Angle);

			// XXX: компенсация угловой скорости не сработала, нужо разобраться, почему
            var v = control.GetShipVelocities();
            var myLinearVelocityLocal = Vector3D.Transform(v.LinearVelocity, invertedMatrix);
            var myAngularVelocityLocal = Vector3D.Transform(v.AngularVelocity, invertedMatrix);
            //var targetAngularVelocity = v.AngularVelocity;
            var targetAngularVelocity = GetAngularVelocities(myLinearVelocityLocal, myAngularVelocityLocal, targetVelocityLocal, targetVectorLocal);

            rotorAzimuth.TargetVelocityRad =
                (float)(azimuthDiff * ROTATION_RATIO) +
                Convert.ToSingle(targetAngularVelocity.Dot(rotorAzimuth.WorldMatrix.Up));

            rotorElevationL.TargetVelocityRad =
                (float)(elevationLDiff * ROTATION_RATIO) +
                Convert.ToSingle(targetAngularVelocity.Dot(rotorElevationL.WorldMatrix.Up));

            rotorElevationR.TargetVelocityRad =
                (float)(elevationRDiff * ROTATION_RATIO) +
                Convert.ToSingle(targetAngularVelocity.Dot(rotorElevationR.WorldMatrix.Up));

            var sameDirection = (Math.Abs(azimuthDiff) + Math.Abs(elevationLDiff) + Math.Abs(elevationRDiff)) < THRESHOLD;

            return sameDirection;
        }

        private void TryToShoot()
        {
            var now = DateTime.UtcNow;

            if (now > nextShotTime)
            {
                launchers[nextLauncher].ShootOnce();
                nextShotTime = now.AddMilliseconds(nextShotDelay);
                nextLauncher = (nextLauncher + 1) % launchers.Count;
            }
        }

        private bool CheckSector()
        {
            var angleL = NormalizeAngle(rotorElevationL?.Angle);
            var angleR = NormalizeAngle(rotorElevationR?.Angle, true);

            var isInSector =
                angleL >= MinElevationRad && angleL < MaxElevationRad &&
                angleR >= MinElevationRad && angleR < MaxElevationRad;

            return isInSector;
        }

        private double NormalizeAngle(double? value, bool invert = false)
        {
            var angle = invert ? -value.GetValueOrDefault() : value.GetValueOrDefault();

            if (double.IsNaN(angle))
            {
                return 0;
            }
            else if (angle < -Math.PI)
            {
                return angle + pi2;
            }
            else if (angle > Math.PI)
            {
                return angle - pi2;
            }

            return angle;
        }

        public static Vector3D GetAngularVelocities(
            Vector3D myLinearSpeed, // линейная скорость своего грида
            Vector3D myAngularSpeed, // угловая скорость своего грида
            Vector3D targetLinearSpeed, // линейная скорость цели
            Vector3D targetVector) // направление до цели
        {
            double sqR = Vector3D.Dot(targetVector, targetVector);

            if (sqR == 0) return Vector3D.Zero;

            return myAngularSpeed - Vector3D.Cross(targetVector, targetLinearSpeed - myLinearSpeed) / sqR;
        }
    }

    #endregion
}
