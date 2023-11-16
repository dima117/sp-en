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
    //internal class RotorTurret
    internal class Program : MyGridProgram
    {
        IMyMotorStator RotorAzimuth;
        IMyMotorStator RotorElevationL;
        IMyMotorStator RotorElevationR;
        IMyTerminalBlock Container;

        IMyLargeMissileTurret Turret;

        int delay = 0;
        DateTime nextShot = DateTime.MinValue;
        int nextLauncher = 0;

        List<IMySmallMissileLauncher> Launchers = new List<IMySmallMissileLauncher>();

        public Program()
        {
            Turret = GridTerminalSystem.GetBlockWithName("TTT") as IMyLargeMissileTurret;

            RotorAzimuth = GridTerminalSystem.GetBlockWithName("AAA") as IMyMotorStator;
            RotorElevationL = GridTerminalSystem.GetBlockWithName("LLL") as IMyMotorStator;
            RotorElevationR = GridTerminalSystem.GetBlockWithName("RRR") as IMyMotorStator;
            Container = GridTerminalSystem.GetBlockWithName("CCC");

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

            if (!target.IsEmpty()) {
                var rotorPos = Container.GetPosition();
                var targetVector = target.Position - rotorPos;

                Turn(targetVector);
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
 
            if (Launchers.Any() && targetVector.Length() < 800)
            {
                if (Math.Abs(azimuthDiff) + Math.Abs(elevationLDiff) + Math.Abs(elevationRDiff) < 0.1)
                {
                    var now = DateTime.UtcNow;
                    if (now > nextShot) {
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
