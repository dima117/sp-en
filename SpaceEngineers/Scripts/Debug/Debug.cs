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
using System.Net;
using Sandbox.Game.Lights;

namespace SpaceEngineers.Scripts.Debug
{
    public class Program : MyGridProgram
    {
        #region Copy

        IMyCameraBlock camera;
        IMyTextPanel lcd;

        public Program()
        {
            lcd = GridTerminalSystem.GetBlockWithName("lcd") as IMyTextPanel;

            camera = GridTerminalSystem.GetBlockWithName("cam") as IMyCameraBlock;
            camera.Enabled = true;
            camera.EnableRaycast = true;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var pos = new Vector3D(-56.62, 21.86, 102.43);
            lcd.WriteText($"Available: {CanScan(camera, pos)}\n");
            lcd.WriteText($"Working: {camera.IsFunctional && camera.Enabled}", true);
        }

        private bool CanScan(IMyCameraBlock cam, Vector3D targetPos) { 
            var dir = targetPos - cam.GetPosition();
            var a = true;

            if (cam.BlockDefinition.SubtypeId == "LargeCameraTopMounted") { 
                a = cam.WorldMatrix.Up.Dot(dir) > 0.2;
            }

            return cam.CanScan(targetPos) && a;
        }

        private string FormatGPS(Vector4D point, string label)
        {
            return $"GPS:{label}:{point.X:0.00}:{point.Y:0.00}:{point.Z:0.00}:#FF89F175:";
        }

        #endregion
    }
}


/*

 */