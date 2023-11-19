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

namespace SpaceEngineers2
{
    public class RotorTurret
    {
        public double MinElevationRad { get; set; } = 0;
        public double MaxElevationRad { get; set; } = Math.PI;

        public RotorTurret()
        {
        }

        public void Update()
        {

        }
    }

    internal class Program : MyGridProgram
    {
        #region Copy

        IMyMotorStator RotorAzimuth;
        IMyMotorStator RotorElevationL;
        IMyMotorStator RotorElevationR;
        IMyTerminalBlock Container;
        IMyCockpit Cockpit;

        IMyLargeTurretBase Turret;

        int delay = 0;
        DateTime nextShot = DateTime.MinValue;
        int nextLauncher = 0;

        List<IMySmallMissileLauncher> Launchers = new List<IMySmallMissileLauncher>();

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

        double min_aaa = -20 * Math.PI / 180;
        double max_aaa = Math.PI;

        public Program()
        {
            Turret = GridTerminalSystem.GetBlockWithName("TTT") as IMyLargeTurretBase;

            RotorAzimuth = GridTerminalSystem.GetBlockWithName("AAA") as IMyMotorStator;
            RotorElevationL = GridTerminalSystem.GetBlockWithName("LLL") as IMyMotorStator;
            RotorElevationR = GridTerminalSystem.GetBlockWithName("RRR") as IMyMotorStator;
            Container = GridTerminalSystem.GetBlockWithName("CCC");
            Cockpit = GridTerminalSystem.GetBlockWithName("PPP") as IMyCockpit;

            GridTerminalSystem.GetBlocksOfType(Launchers);

            if (Launchers.Any())
            {
                delay = 1000 / Launchers.Count;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType uType)
        {
            // todo: останавливать вращение, если нет цели
            // todo: сектор стрельбы
            // todo: расчет упреждения
            // todo: собственный радар
            // todo: определять ориентацию вертикальных роторов

            var target = Turret.GetTargetedEntity();

            var pi2 = Math.PI * 2;

            var angleL = (RotorElevationL.Angle + pi2 + Math.PI) % pi2 - Math.PI;
            var angleR = (-RotorElevationR.Angle + pi2 + +Math.PI) % pi2 - Math.PI;

            Cockpit.GetSurface(0).WriteText($"L: {angleL:0.00}\n R: {angleR:0.00}");


            if (!target.IsEmpty())
            {
                var rotorPos = Container.GetPosition();
                var targetVector = target.Position - rotorPos;

                Turn(targetVector);
            }
            else
            {
                RotorAzimuth.TargetVelocityRad = 0;
                RotorElevationL.TargetVelocityRad = 0;
                RotorElevationR.TargetVelocityRad = 0;
            }
        }

        void Turn(Vector3D targetVector)
        {
            var invertedMatrix = MatrixD.Invert(RotorAzimuth.WorldMatrix.GetOrientation());
            var relativePos = Vector3D.Transform(targetVector, invertedMatrix);

            float azimuth = (float)Math.Atan2(-relativePos.X, relativePos.Z);
            float elevation = (float)Math.Asin(relativePos.Y / relativePos.Length());

            float azimuthDiff = CutTurn(azimuth - RotorAzimuth.Angle);
            float elevationLDiff = CutTurn(elevation - RotorElevationL.Angle);
            float elevationRDiff = CutTurn(-elevation - RotorElevationR.Angle);

            RotorAzimuth.TargetVelocityRad = azimuthDiff * 5;
            RotorElevationL.TargetVelocityRad = elevationLDiff * 5;
            RotorElevationR.TargetVelocityRad = elevationRDiff * 5;

            var pi2 = Math.PI * 2;

            var angleL = (RotorElevationL.Angle + pi2 + Math.PI) % pi2 - Math.PI;
            var angleR = (-RotorElevationR.Angle + pi2 + +Math.PI) % pi2 - Math.PI;

            if (Launchers.Any() && targetVector.Length() < 800)
            {
                var diffSum = Math.Abs(azimuthDiff) + Math.Abs(elevationLDiff) + Math.Abs(elevationRDiff);
                var isInSector = (angleL > min_aaa && angleL < max_aaa) && (angleR > min_aaa && angleR < max_aaa);

                if (diffSum < 0.1 && isInSector)
                {
                    var now = DateTime.UtcNow;
                    if (now > nextShot)
                    {
                        Launchers[nextLauncher].ShootOnce();
                        nextShot = now.AddMilliseconds(delay);
                        nextLauncher = (nextLauncher + 1) % Launchers.Count;
                    }
                }
            }


        }

        private float CutTurn(float Turn)
        {
            if (float.IsNaN(Turn)) Turn = 0;
            if (Turn < -Math.PI) Turn += 2 * (float)Math.PI;
            else if (Turn > Math.PI) Turn -= 2 * (float)Math.PI;
            return Turn;
        }
    }
}
