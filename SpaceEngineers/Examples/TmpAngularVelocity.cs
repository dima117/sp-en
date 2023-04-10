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

        bool hasVector = false;
        Vector3D lastForwardVector;

        public void Main(string argument, UpdateType updateSource)
        {
            if (!hasVector)
            {
                hasVector = true;
                lastForwardVector = control.WorldMatrix.Forward;
            }

            double pitch = 0, yaw = 0, roll = 0;
            GetRotationAngles(lastForwardVector, control.WorldMatrix.Forward, control.WorldMatrix.Left, control.WorldMatrix.Up, out yaw, out pitch);

            Vector2 mouseInput = control.RotationIndicator;
            float rollInput = control.RollIndicator;

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!
            var worldAngularVelocity = control.GetShipVelocities().AngularVelocity;
            var localAngularVelocity = Vector3D.TransformNormal(worldAngularVelocity, MatrixD.Transpose(control.WorldMatrix));
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!

            var sb = new StringBuilder();

            sb.AppendLine($"world\npitch: {worldAngularVelocity.X:0.00}\nyaw: {worldAngularVelocity.Y:0.00}\nroll: {worldAngularVelocity.Z:0.00}\n---");
            sb.AppendLine($"local\npitch: {localAngularVelocity.X:0.00}\nyaw: {localAngularVelocity.Y:0.00}\nroll: {localAngularVelocity.Z:0.00}\n---");
            sb.AppendLine($"new\npitch: {pitch}\nyaw: {yaw}\nroll: {roll}\n---");
            sb.AppendLine($"input\npitch: {mouseInput.X}\nyaw: {mouseInput.Y}\nroll: {rollInput}\n---");

            lcd.WriteText(sb);

            lastForwardVector = control.WorldMatrix.Forward;

            var xxx = control.MoveIndicator;

            if (Vector3D.IsZero(xxx, 1E-3))
            {
                gyro.GyroOverride = false;
            }
            else {
                gyro.GyroOverride = true;
                gyro.Yaw = xxx.X*10;
            }

            
        }

        void GetRotationAngles(Vector3D v_target, Vector3D v_front, Vector3D v_left, Vector3D v_up, out double yaw, out double pitch)
        {
            //Dependencies: VectorProjection() | VectorAngleBetween()
            var projectTargetUp = VectorProjection(v_target, v_up);
            var projTargetFrontLeft = v_target - projectTargetUp;

            yaw = VectorAngleBetween(v_front, projTargetFrontLeft);

            if (Vector3D.IsZero(projTargetFrontLeft) && !Vector3D.IsZero(projectTargetUp)) //check for straight up case
                pitch = MathHelper.PiOver2;
            else
                pitch = VectorAngleBetween(v_target, projTargetFrontLeft); //pitch should not exceed 90 degrees by nature of this definition

            //---Check if yaw angle is left or right  
            //multiplied by -1 to convert from right hand rule to left hand rule
            yaw = -1 * Math.Sign(v_left.Dot(v_target)) * yaw;

            //---Check if pitch angle is up or down    
            pitch = Math.Sign(v_up.Dot(v_target)) * pitch;

            //---Check if target vector is pointing opposite the front vector
            if (Math.Abs(yaw) <= 1E-6 && v_target.Dot(v_front) < 0)
            {
                yaw = Math.PI;
            }
        }

        Vector3D VectorProjection(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a.Dot(b) / b.LengthSquared() * b;
        }

        double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians 
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return 0;
            else
                return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }
    }
}
