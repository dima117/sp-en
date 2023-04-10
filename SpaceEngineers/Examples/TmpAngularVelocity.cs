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

namespace SpaceEngineers.Examples.TmpAngularVelocity
{
    public sealed class Program : MyGridProgram
    {
        IMyShipController control;
        IMyTextPanel lcd;
        IMyGyro gyro;

        public Program()
        {
            control = GridTerminalSystem.GetBlockWithName("CONTROL_01") as IMyShipController;
            lcd = GridTerminalSystem.GetBlockWithName("LCD_01") as IMyTextPanel;
            gyro = GridTerminalSystem.GetBlockWithName("GYRO_01") as IMyGyro;
            
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Vector2 mouseInput = control.RotationIndicator;
            float rollInput = control.RollIndicator;

            var worldAngularVelocity = control.GetShipVelocities().AngularVelocity;

            var localAngularVelocity = Vector3D.TransformNormal(worldAngularVelocity, MatrixD.Transpose(control.WorldMatrix));

            var sb = new StringBuilder();

            sb.AppendLine($"world\npitch: {worldAngularVelocity.X}\nyaw: {worldAngularVelocity.Y}\nroll: {worldAngularVelocity.Z}\n---");
            sb.AppendLine($"local\npitch: {localAngularVelocity.X}\nyaw: {localAngularVelocity.Y}\nroll: {localAngularVelocity.Z}\n---");
            sb.AppendLine($"input\npitch: {mouseInput.X}\nyaw: {mouseInput.Y}\nroll: {rollInput}\n---");

            lcd.WriteText(sb);
        }
    }
}
