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

        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\DirectionController2.cs

        const float FACTOR = 2;

        readonly IMyCockpit cockpit;
        readonly IMyGyro[] gyros;
        readonly IMyShipConnector connector;
        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        readonly IMyTextSurface lcdStatus;

        private Vector3D? direction;

        private IEnumerable<T> GetBlocksOfType<T>(
            Func<T, bool> filter = null) where T : class
        {
            Func<T, bool> f = filter == null ? (t => true) : filter;
            var list = new List<T>();

            GridTerminalSystem.GetBlocksOfType(list, f);

            return list;
        }

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            connector = GetBlocksOfType<IMyShipConnector>(w => w.CubeGrid == Me.CubeGrid).First();
            cockpit = GetBlocksOfType<IMyCockpit>(w => w.CubeGrid == Me.CubeGrid).First();
            gyros = GetBlocksOfType<IMyGyro>(w => w.CubeGrid == Me.CubeGrid).ToArray();

            lcdStatus = cockpit.GetSurface(0);

            var x = Vector3D.Zero;
            if (!Vector3D.TryParse(Me.CustomData, out x))
            {
                direction = x;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        private void SetDirection()
        {
            if (connector.Status == MyShipConnectorStatus.Connected) {
                var grid = connector.OtherConnector.CubeGrid;

                direction = GetBlocksOfType<IMyShipController>(w => w.CubeGrid == grid)
                            .FirstOrDefault()?.WorldMatrix.Down;

                Me.CustomData = direction.HasValue ? direction.ToString() : string.Empty;
            }
        }

        private void Lock()
        {
            if (direction.HasValue)
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

        private void Align()
        {
            if (direction.HasValue)
            {
                var axis = DirectionController2.GetAxis(cockpit.WorldMatrix.Down, direction.Value);

                foreach (var gyro in gyros)
                {
                    gyro.Yaw = FACTOR * cockpit.RollIndicator;
                    gyro.Pitch = FACTOR * Convert.ToSingle(axis.Dot(gyro.WorldMatrix.Right));
                    gyro.Roll = FACTOR * Convert.ToSingle(axis.Dot(gyro.WorldMatrix.Backward));

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
                    direction = null;
                    Me.CustomData = string.Empty;
                    Unlock();
                    break;

                case "lock":
                    Lock();
                    break;
                case "unlock":
                    Unlock();
                    break;
            }

            Align();

            lcdStatus.WriteText($"Locked: {direction.HasValue}");

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
