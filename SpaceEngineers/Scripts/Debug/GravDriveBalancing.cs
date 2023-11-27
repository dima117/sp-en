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

namespace SpaceEngineers.Scripts.GravDriveBalancing
{
    public class Program : MyGridProgram
    {
        #region Copy

        readonly IMyTextPanel lcd;
        readonly IMyShipController controller;

        public Program()
        {
            
            controller = GridTerminalSystem.GetBlockWithName("CONTROL") as IMyShipController;
            lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;

            //Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var invertedMatrix = MatrixD.Invert(controller.WorldMatrix.GetOrientation());
            var centerOfMassLocal = Vector3D.Transform(controller.CenterOfMass, invertedMatrix);

            var massBlocks = new List<IMyArtificialMassBlock>();
            GridTerminalSystem.GetBlocksOfType(massBlocks);

            var sb = new StringBuilder();

            sb.AppendLine($"center: {controller.CenterOfMass}");
            sb.AppendLine($"local center: {centerOfMassLocal}");

            sb.AppendLine($"---\n{controller.CustomName}");
            sb.AppendLine($"grid pos: {controller.Position}");

            var pos1 = controller.GetPosition();
            var localPos1 = Vector3D.Transform(pos1, invertedMatrix);
            sb.AppendLine($"global pos: {pos1}");
            sb.AppendLine($"local pos: {localPos1}");


            var sum = Vector3D.Zero;
            var localSum = Vector3D.Zero;
            double totalMass = 0;

            foreach ( var block in massBlocks )
            {
                sb.AppendLine($"---\n{block.CustomName}");
                sb.AppendLine($"virtual mass: {block.VirtualMass}");
                sb.AppendLine($"grid pos: {block.Position}");

                var pos = block.GetPosition();
                var localPos = Vector3D.Transform(pos, invertedMatrix);
                sb.AppendLine($"global pos: {pos}");
                sb.AppendLine($"local pos: {localPos}");

                sum += pos;
                localSum += localPos;
                totalMass += block.VirtualMass;
            }

            var center2 = sum / massBlocks.Count;
            var center2local = localSum / massBlocks.Count;
            var center2local2 = Vector3D.Transform(center2, invertedMatrix);

            sb.AppendLine($"\n\ncenter2: {center2}");
            sb.AppendLine($"\n\ncenter2 local: {center2local}");
            sb.AppendLine($"\n\ncenter2 local2: {center2local2}");

            lcd.WriteText(sb.ToString());
        }

        #endregion
    }
}
