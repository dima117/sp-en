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
                    var entity = TargetTracker.Scan(cam, 10000);

                    if (entity.HasValue)
                    {
                        tt.LockOn(entity.Value);
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

                    torpedo?.Update(tt.Current);

                    break;
            }

            UpdateSystemLcd();
            UpdateTargetLcd();
            UpdateTorpedoLcd(torpedo);
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
            sb.AppendLine($"Locked: {tt.Current.HasValue}");

            if (tt.Current.HasValue)
            {
                var target = tt.Current.Value.Entity;
                var distance = Vector3D.Distance(cam.GetPosition(), target.Position);

                sb.AppendLine($"- type: {target.Type}");
                sb.AppendLine($"- position: {target.Position}");
                sb.AppendLine($"- speed: {target.Velocity.Length():0.000}");
                sb.AppendLine($"- distance: {distance:0.000}");
            }

            lcd2.WriteText(sb.ToString());
        }

        void UpdateTorpedoLcd(Torpedo torpedo)
        {
            if (torpedo == null)
            {
                lcd3.WriteText(string.Empty);
                return;
            }

            var sb = new StringBuilder();
            var myPos = torpedo.Position;

            sb.AppendLine("Missile:");
            sb.AppendLine($"- speed: {torpedo.Speed:0.00}");
            sb.AppendLine($"- position: {myPos}");

            if (tt.Current.HasValue)
            {
                var target = tt.Current.Value.Entity;
                var distance = Vector3D.Distance(myPos, target.Position);

                sb.AppendLine($"- target distance: {distance:0}");
            }

            lcd3.WriteText(sb.ToString());
        }
        #endregion
    }
}
