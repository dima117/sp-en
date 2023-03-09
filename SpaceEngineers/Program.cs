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

        readonly TargetTracker tt;
        readonly IMyTextPanel lcd;
        readonly IMyCameraBlock cam;
        readonly IMyShipMergeBlock slot;

        Torpedo torpedo;
       
        public Program()
        {
            tt = new TargetTracker(this, "Камера");
            lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
            slot = GridTerminalSystem.GetBlockWithName("SLOT") as IMyShipMergeBlock;
            cam = GridTerminalSystem.GetBlockWithName("MAIN_CAM") as IMyCameraBlock;
            cam.EnableRaycast = true;

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)

            {
                case "capture":
                    var target = cam.Raycast(20000);
                    if (!target.IsEmpty())
                    {
                        tt.LockOn(target.Position);
                    }
                    break;
                case "reload":
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

                    if (torpedo != null)
                    {
                        torpedo.Update(tt.CurrentTarget);
                    }
                    break;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Torpedo: {torpedo?.Id}");
            sb.AppendLine($"------------------------");
            sb.AppendLine($"Range: {cam.AvailableScanRange}");
            sb.AppendLine($"Cam count: {tt.Count}");
            sb.AppendLine($"------------------------");
            sb.AppendLine($"Locked: {!tt.CurrentTarget.IsEmpty()}");

            if (!tt.CurrentTarget.IsEmpty())
            {
                sb.AppendLine($"Pos X: {tt.CurrentTarget.Position.X}");
                sb.AppendLine($"Pos Y: {tt.CurrentTarget.Position.Y}");
                sb.AppendLine($"Pos Z: {tt.CurrentTarget.Position.Z}");
                sb.AppendLine($"Speed: {tt.CurrentTarget.Velocity.Length():0.000}");
                sb.AppendLine($"Distance: {Vector3D.Distance(cam.GetPosition(), tt.CurrentTarget.Position):0.000}");
            }

            lcd.WriteText(sb.ToString());
        }
        public void Save()
        {

        }
        #endregion
    }
}
