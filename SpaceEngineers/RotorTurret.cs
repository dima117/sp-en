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
using VRage.Noise.Modifiers;
using System.ComponentModel;

namespace SpaceEngineers2
{
    #region Copy

    public class RotorTurret
    {
        const int LAUNCHER_RELOAD_TIME = 1000;
        const int ROTATION_RATIO = 1000;
        const int MAX_DISTANCE = 800;
        const float THRESHOLD = 0.1f;

        private IMyMotorStator rotorAzimuth;
        private IMyMotorStator rotorElevationL;
        private IMyMotorStator rotorElevationR;
        private IMyTerminalBlock container;

        private IMyLargeTurretBase designator;

        private int delay = 0;
        private DateTime nextShot = DateTime.MinValue;
        private int nextLauncher = 0;

        private List<IMySmallMissileLauncher> launchers = new List<IMySmallMissileLauncher>();

        private double pi2 = Math.PI * 2;

        public double AngleL => NormalizeAngle(rotorElevationL.Angle);
        public double AngleR => NormalizeAngle(-rotorElevationR.Angle);

        public bool Enabled { get; set; }
        public double MinElevationRad { get; set; } = 0;
        public double MaxElevationRad { get; set; } = Math.PI;

        private T GetBlock<T>(IEnumerable<IMyTerminalBlock> list, string prefix = null) where T : class, IMyTerminalBlock
        {
            return list.FirstOrDefault(b => b is T && (prefix == null || b.CustomName.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))) as T;
        }

        public RotorTurret(IMyBlockGroup group)
        {
            group.GetBlocksOfType(launchers);
            if (launchers.Any())
            {
                delay = LAUNCHER_RELOAD_TIME / launchers.Count;
            }

            var tmp = new List<IMyTerminalBlock>();
            group.GetBlocks(tmp);

            rotorAzimuth = GetBlock<IMyMotorStator>(tmp, "AZIMUTH");
            rotorElevationL = GetBlock<IMyMotorStator>(tmp, "LEFT");
            rotorElevationR = GetBlock<IMyMotorStator>(tmp, "RIGHT");
            container = GetBlock<IMyTerminalBlock>(tmp, "BODY");
            designator = GetBlock<IMyLargeTurretBase>(tmp, "DESIGNATOR");
        }

        public void Update()
        {
            // todo: останавливать вращение, если нет цели
            // todo: сектор стрельбы
            // todo: расчет упреждения
            // todo: собственный радар
            // todo: определять ориентацию вертикальных роторов

            var target = designator.GetTargetedEntity();

            if (target.IsEmpty())
            {
                rotorAzimuth.TargetVelocityRad = 0;
                rotorElevationL.TargetVelocityRad = 0;
                rotorElevationR.TargetVelocityRad = 0;
            }
            else
            {
                var rotorPos = container.GetPosition();
                var targetVector = target.Position - rotorPos;

                SetDirection(targetVector);
            }
        }

        private void SetDirection(Vector3 targetVector)
        {
            var invertedMatrix = MatrixD.Invert(rotorAzimuth.WorldMatrix.GetOrientation());
            var relativePos = Vector3D.Transform(targetVector, invertedMatrix);

            var azimuth = Math.Atan2(-relativePos.X, relativePos.Z);
            var elevation = Math.Asin(relativePos.Y / relativePos.Length());

            var azimuthDiff = NormalizeAngle(azimuth - rotorAzimuth.Angle);
            var elevationLDiff = NormalizeAngle(elevation - rotorElevationL.Angle);
            var elevationRDiff = NormalizeAngle(-elevation - rotorElevationR.Angle);

            rotorAzimuth.TargetVelocityRad = (float)(azimuthDiff * ROTATION_RATIO);
            rotorElevationL.TargetVelocityRad = (float)(elevationLDiff * ROTATION_RATIO);
            rotorElevationR.TargetVelocityRad = (float)(elevationRDiff * ROTATION_RATIO);

            var angleL = AngleL;
            var angleR = AngleR;

            if (launchers.Any() && targetVector.Length() < MAX_DISTANCE)
            {
                // вынести наружу
                var sameDirection = (Math.Abs(azimuthDiff) + Math.Abs(elevationLDiff) + Math.Abs(elevationRDiff)) < THRESHOLD;
                var isInSector = (angleL >= MinElevationRad && angleL < MaxElevationRad) && (angleR >= MinElevationRad && angleR < MaxElevationRad);

                if (sameDirection && isInSector)
                {
                    var now = DateTime.UtcNow;

                    if (now > nextShot)
                    {
                        launchers[nextLauncher].ShootOnce();
                        nextShot = now.AddMilliseconds(delay);
                        nextLauncher = (nextLauncher + 1) % launchers.Count;
                    }
                }
            }
        }

        private double NormalizeAngle(double angle)
        {
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
