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

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        #region Copy

        TargetTracker tt;
        IMyTextPanel lcd1; // система
        IMyTextPanel lcd2; // цель
        IMyTextPanel lcd3; // торпеда
        IMyCameraBlock cam;

        IMyShipMergeBlock slot;
        Torpedo torpedo;

        public Program()
        {
            slot = GridTerminalSystem.GetBlockWithName("SLOT") as IMyShipMergeBlock;

            tt = new TargetTracker(this, "Камера");
            lcd1 = GridTerminalSystem.GetBlockWithName("LCD1") as IMyTextPanel;
            lcd2 = GridTerminalSystem.GetBlockWithName("LCD2") as IMyTextPanel;
            lcd3 = GridTerminalSystem.GetBlockWithName("LCD3") as IMyTextPanel;

            cam = GridTerminalSystem.GetBlockWithName("MAIN_CAM") as IMyCameraBlock;
            cam.EnableRaycast = true;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "lock":
                    var target = cam.Raycast(20000);
                    if (!target.IsEmpty())
                    {
                        tt.LockOn(target.Position);
                    }
                    break;
                case "reload":
                    torpedo = null;
                    slot.Enabled = true;
                    break;
                case "prepare":
                    torpedo = new Torpedo(this);
                    break;
                case "start":
                    slot.Enabled = false;
                    torpedo.Start();

                    break;
                default:
                    tt.Update();

                    var info = torpedo?.Update(tt.CurrentTarget);
                    lcd3.WriteText(info ?? "--");
                    break;
            }

            UpdateSystemLcd();
            UpdateTargetLcd();
        }

        void UpdateSystemLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Range: {cam.AvailableScanRange}");
            sb.AppendLine($"Cam count: {tt.Count}");
            lcd1.WriteText(sb.ToString());
        }

        void UpdateTargetLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {tt.CurrentTarget.IsEmpty()}");

            if (!tt.CurrentTarget.IsEmpty())
            {
                sb.AppendLine($"Type: {tt.CurrentTarget.Type}");
                sb.AppendLine($"Pos X: {tt.CurrentTarget.Position.X:0.000}");
                sb.AppendLine($"Pos Y: {tt.CurrentTarget.Position.Y:0.000}");
                sb.AppendLine($"Pos Z: {tt.CurrentTarget.Position.Z:0.000}");
                sb.AppendLine($"Speed: {tt.CurrentTarget.Velocity.Length():0.000}");
                sb.AppendLine($"Distance: {Vector3D.Distance(cam.GetPosition(), tt.CurrentTarget.Position):0.000}");
            }

            lcd2.WriteText(sb.ToString());
        }
        #endregion
    }
}
