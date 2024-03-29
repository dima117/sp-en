﻿using System;
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
using static VRage.Game.MyObjectBuilder_ControllerSchemaDefinition;

namespace SpaceEngineers.Scripts.GravDriveBalancing
{
    public class Program : MyGridProgram
    {
        #region Copy

        readonly IMyTextPanel lcd;
        readonly IMyShipController remote;
        readonly List<IMyGravityGenerator> generators = new List<IMyGravityGenerator>();
        readonly List<IMyArtificialMassBlock> massBlocks = new List<IMyArtificialMassBlock>();

        public Program()
        {

            remote = GridTerminalSystem.GetBlockWithName("CONTROL") as IMyShipController;
            lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;

            GridTerminalSystem.GetBlocksOfType(generators);
            GridTerminalSystem.GetBlocksOfType(massBlocks);

            //Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        // матрица преобразования в локальные координаты
        private MatrixD invertedMatrix;

        private Vector3D ToLocal(Vector3D value)
        {
            return Vector3D.Transform(value, invertedMatrix);
        }

        private Vector3D GetVirtualCenterOfMass(IMyArtificialMassBlock[] massBlocks)
        {
            var sumPos = Vector3D.Zero;

            foreach (var b in massBlocks)
            {
                sumPos += b.GetPosition();
            }

            return sumPos / massBlocks.Length;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // обновляем кэш матрицы преобразования в локальные координаты
            invertedMatrix = MatrixD.Invert(remote.WorldMatrix.GetOrientation());

            // включенные блоки искусственной массы
            var tmp = new List<IMyArtificialMassBlock>();
            GridTerminalSystem.GetBlocksOfType(tmp);

            var massBlocks = tmp.Where(b => b.Enabled && b.IsFunctional).ToArray();

            MyShipVelocities velocities = remote.GetShipVelocities();


            var centerOfMass = remote.CenterOfMass;
            var centerOfMassLocal = ToLocal(remote.CenterOfMass);

            // gravity generators
            Vector3 localLinearVelocity = ToLocal(velocities.LinearVelocity);




            var sb = new StringBuilder();

            sb.AppendLine($"center: {remote.CenterOfMass}");
            sb.AppendLine($"local center: {centerOfMassLocal}");

            sb.AppendLine($"---\n{remote.CustomName}");
            sb.AppendLine($"grid pos: {remote.Position}");

            var pos1 = remote.GetPosition();
            var localPos1 = Vector3D.Transform(pos1, invertedMatrix);
            sb.AppendLine($"global pos: {pos1}");
            sb.AppendLine($"local pos: {localPos1}");


            var sum = Vector3D.Zero;
            var localSum = Vector3D.Zero;
            double totalMass = 0;

            foreach (var block in massBlocks)
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

            var center2 = sum / massBlocks.Count();
            var center2local = localSum / massBlocks.Count();
            var center2local2 = Vector3D.Transform(center2, invertedMatrix);

            sb.AppendLine($"\n\ncenter2: {center2}");
            sb.AppendLine($"\n\ncenter2 local: {center2local}");
            sb.AppendLine($"\n\ncenter2 local2: {center2local2}");

            lcd.WriteText(sb.ToString());
        }

        #endregion
    }
}
