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

namespace SpaceEngineers.Examples.GravDrive3
{
    internal class Program : MyGridProgram
    {
        //---------
        List<IMyGravityGenerator> gravs;
        List<IMyArtificialMassBlock> mass;
        List<IMyGyro> gyros;
        List<IMyGyro> stab_gyros;
        List<IMyTerminalBlock> temp;
        IMyShipController controller;

        bool Damp;
        bool Test;

        Program()
        {
            gravs = new List<IMyGravityGenerator>();
            mass = new List<IMyArtificialMassBlock>();
            gyros = new List<IMyGyro>();
            stab_gyros = new List<IMyGyro>();
            temp = new List<IMyTerminalBlock>();

            GridTerminalSystem.GetBlocksOfType<IMyShipController>(temp, b => (b.IsSameConstructAs(Me) && b.CustomName.Contains("MainCockpit")));
            if (temp.Count > 0) controller = temp[0] as IMyShipController;

            GridTerminalSystem.GetBlocksOfType<IMyGravityGenerator>(gravs, b => (b.IsSameConstructAs(Me) && !b.CustomName.Contains("Stop")));
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(stab_gyros, b => (b.IsSameConstructAs(Me) && b.CustomName.Contains("Stab")));
            GridTerminalSystem.GetBlocksOfType<IMyArtificialMassBlock>(mass, b => (b.IsSameConstructAs(Me)));
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, b => (b.IsSameConstructAs(Me)));

        }

        void Main(string arg, UpdateType uType)
        {
            if (uType == UpdateType.Update1)
            {
                Update();
            }
            else
            {
                switch (arg)
                {
                    case ("Start"):
                        EngineON(true);
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        break;
                    case ("Stop"):
                        EngineON(false);
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        break;
                    case ("Damp"):
                        Damp = !Damp;
                        break;
                    case ("Test"):
                        Test = !Test;
                        break;
                    default:
                        break;
                }
            }

        }

        void Update()
        {
            Vector3D input = Vector3D.Transform(controller.MoveIndicator, controller.WorldMatrix.GetOrientation());
            if (controller.DampenersOverride || Damp)
                input -= controller.GetShipVelocities().LinearVelocity * 0.005;
            if (Test) input += controller.WorldMatrix.Right;
            foreach (IMyGravityGenerator g in gravs)
            {
                g.GravityAcceleration = (float)input.Dot(g.WorldMatrix.Down) * 10;
            }

            Vector3D rot = controller.GetShipVelocities().AngularVelocity * 100 * controller.GetShipVelocities().AngularVelocity.LengthSquared();
            rot += controller.WorldMatrix.Right * controller.RotationIndicator.X * 10;
            rot += controller.WorldMatrix.Up * controller.RotationIndicator.Y * 10;
            rot += controller.WorldMatrix.Backward * controller.RollIndicator * 10;

            foreach (IMyGyro gyro in gyros)
            {
                gyro.Yaw = (float)rot.Dot(gyro.WorldMatrix.Up);
                gyro.Pitch = (float)rot.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)rot.Dot(gyro.WorldMatrix.Backward);
                // (controller as IMyCockpit).GetSurface(0).WriteText("Y:" + (float)rot.Dot(gyro.WorldMatrix.Up));
            }

        }

        void EngineON(bool On)
        {

            foreach (IMyArtificialMassBlock b in mass)
            {
                b.Enabled = On;
            }
            foreach (IMyGravityGenerator b in gravs)
            {
                b.Enabled = On;
                b.GravityAcceleration = 0f;
            }
            foreach (IMyGyro b in gyros)
            {
                b.GyroOverride = On;
                b.Yaw = 0f;
                b.Pitch = 0f;
                b.Roll = 0f;
            }
            foreach (IMyGyro b in stab_gyros)
            {
                b.GyroOverride = On;
                b.Yaw = 0f;
                b.Pitch = 0f;
                b.Roll = 0f;
            }
        }

        //---------
    }
}
