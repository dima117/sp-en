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
using Sandbox.Game.Entities;
using SpaceEngineers.Lib;
using static VRageMath.Base6Directions;

namespace SpaceEngineers.Scripts.Printer
{
    public class Program : MyGridProgram
    {
        // скрипт для выравнивания челнока в принтере кораблей
        // гироскоп должен быть сориентирован по направлению кабины
        // remote control а принтере должен быть сориентирован вверх

        // TODO: двойной проход
        // TODO: вычислять сигнал для двигателей на основе суммарной мощности двигателей и массы
        // TODO: более точное позиционирование по высоте относительно начальной точки

        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Grid.cs
        // import:Lib\DirectionController2.cs

        const float FACTOR = 1.4f;

        readonly IMyCockpit cockpit;
        readonly IMyGyro[] gyros;
        readonly IMyShipConnector connector;
        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        readonly IMyTextSurface lcdStatus;
        readonly IMyThrust[] thrusters;
        private readonly Grid grid;

        private Vector3D? directionDown;
        private Vector3D? directionForward;
        private Vector3D[] points;
        private int index = 0;

        public Program()
        {
            grid = new Grid(GridTerminalSystem);

            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            connector = grid.GetBlocksOfType<IMyShipConnector>(w => w.CubeGrid == Me.CubeGrid).First();
            cockpit = grid.GetBlocksOfType<IMyCockpit>(w => w.CubeGrid == Me.CubeGrid).First();
            gyros = grid.GetBlocksOfType<IMyGyro>(w => w.CubeGrid == Me.CubeGrid);

            thrusters = grid.GetBlocksOfType<IMyThrust>(w => w.CubeGrid == Me.CubeGrid);

            lcdStatus = cockpit.GetSurface(0);

            var x = Vector3D.Zero;
            var lines = Me.CustomData.Split('\n');

            if (lines.Length == 2)
            {
                if (!Vector3D.TryParse(lines[0], out x))
                {
                    directionDown = x;
                }

                if (!Vector3D.TryParse(lines[1], out x))
                {
                    directionForward = x;
                }
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        private void SetDirection()
        {
            if (connector.Status == MyShipConnectorStatus.Connected)
            {
                var cubeGrid = connector.OtherConnector.CubeGrid;

                IMyShipController cockpit = grid
                    .GetBlocksOfType<IMyShipController>(w => w.CubeGrid == cubeGrid)
                    .FirstOrDefault();

                directionDown = cockpit?.WorldMatrix.Down;
                directionForward = cockpit?.WorldMatrix.Forward;

                if (directionDown.HasValue && directionForward.HasValue)
                {
                    Me.CustomData = directionDown.ToString() + "\n" + directionForward.ToString();
                }
                else
                {
                    Me.CustomData = string.Empty;
                }
            }
        }

        private void Lock()
        {
            if (directionDown.HasValue)
            {
                connector.Disconnect();

                foreach (var gyro in gyros)
                {
                    gyro.GyroOverride = true;
                    gyro.GyroPower = 1;
                }
            }

        }

        private void Unlock()
        {
            foreach (var gyro in gyros)
            {
                gyro.GyroOverride = false;
                gyro.GyroPower = 0.3f;
            }
        }

        private void Move()
        {
            if (points == null || index >= points.Length)
            {
                return;
            }

            var nextPoint = points[index];
            var pos = cockpit.GetPosition();
            var velocity = cockpit.GetShipVelocities().LinearVelocity;

            var diff = nextPoint - pos;

            if (diff.Length() < 0.3)
            {
                foreach (var t in thrusters)
                {
                    t.ThrustOverridePercentage = 0;
                }

                index++;
                return;
            }

            var dir = Vector3D.Normalize(nextPoint - pos);
            var vel = Vector3D.Normalize(velocity);

            var v = velocity.Length();

            if (v < 0.05 || (velocity.Length() < 0.5 && dir.Dot(vel) > 0.98))
            {
                cockpit.DampenersOverride = false;

                foreach (var t in thrusters)
                {
                    var power = t.WorldMatrix.Backward.Dot(dir) * 100 * 0.05;

                    if (power > 0)
                    {
                        t.ThrustOverridePercentage = Convert.ToSingle(power);
                    }
                }
            }
            else
            {
                cockpit.DampenersOverride = true;
                foreach (var t in thrusters)
                {
                    t.ThrustOverridePercentage = 0;
                }
            }
        }

        private void Align()
        {
            if (directionDown.HasValue && directionForward.HasValue)
            {
                var axisDown = DirectionController2.GetAxis(
                    cockpit.WorldMatrix.Down,
                    directionDown.Value);

                var axisForward = DirectionController2.GetAxis(
                    cockpit.WorldMatrix.Forward,
                    directionForward.Value);

                foreach (var gyro in gyros)
                {
                    gyro.Yaw = FACTOR * Convert.ToSingle(axisForward.Dot(gyro.WorldMatrix.Up));
                    gyro.Pitch = FACTOR * Convert.ToSingle(axisDown.Dot(gyro.WorldMatrix.Right));
                    gyro.Roll = FACTOR * Convert.ToSingle(axisDown.Dot(gyro.WorldMatrix.Backward));

                    gyro.GyroOverride = true;
                }
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            switch (argument)
            {
                case "init":
                    SetDirection();
                    Lock();
                    break;

                case "reset":
                    directionDown = null;
                    Me.CustomData = string.Empty;
                    Unlock();
                    break;

                case "lock":
                    Lock();
                    break;
                case "unlock":
                    Unlock();
                    break;
                case "start":
                    Start();
                    break;
                case "stop":
                    points = null;
                    break;
            }

            Align();
            Move();

            lcdStatus.WriteText($"Locked: {directionDown.HasValue && directionForward.HasValue}");
            lcdStatus.WriteText($"Move: {points != null}", true);
            lcdStatus.WriteText($"Index: {index}", true);

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        private void Start()
        {
            if (!directionDown.HasValue || !directionForward.HasValue)
            {
                return;
            }

            var OFFSET = 20;
            var LENGT = 120;
            var STEP = 2.7;

            var left = cockpit.WorldMatrix.Left * OFFSET;
            var right = cockpit.WorldMatrix.Right * OFFSET;
            var up = cockpit.WorldMatrix.Up * STEP;

            var pos = cockpit.GetPosition();

            var list = new List<Vector3D>();

            for (var level = 0; level < LENGT / STEP; level++)
            {
                var pos1 = pos + level * up;

                list.Add(pos1 + left);
                list.Add(pos1 + right);
                list.Add(pos1);
                list.Add(pos1 + up);
            }

            points = list.ToArray();
            index = 0;
        }

        private string FormatGPS(Vector3D point, string label)
        {
            return $"GPS:{label}:{point.X:0.00}:{point.Y:0.00}:{point.Z:0.00}:#FF89F175:";
        }

        #endregion
    }
}
