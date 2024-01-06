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

        private IMyShipWelder[] welders;
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

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            switch (argument)
            {
                case "lock":
                    welders = GetBlocksOfType<IMyShipWelder>(w => w.CubeGrid != Me.CubeGrid).ToArray();
                    direction = GetBlocksOfType<IMyShipController>(w => w.CubeGrid != Me.CubeGrid)
                        .FirstOrDefault()?.WorldMatrix.Backward;

                    foreach (var w in welders)
                    {
                        w.Enabled = false;
                    };

                    connector.Disconnect();

                    foreach (var gyro in gyros)
                    {
                        gyro.GyroOverride = true;
                    }

                    break;
                case "unlock":
                    direction = null;

                    foreach (var gyro in gyros)
                    {
                        gyro.GyroOverride = false;
                    }

                    break;
                case "on":
                    if (welders != null)
                    {
                        foreach (var w in welders)
                        {
                            w.Enabled = true;
                        };
                    }

                    break;
                case "off":
                    if (welders != null)
                    {
                        foreach (var w in welders)
                        {
                            w.Enabled = false;
                        };
                    }

                    break;
            }

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

            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {direction.HasValue}");
            sb.AppendLine($"Welders: {welders?.Count() ?? 0}");

            lcdStatus.WriteText(sb);

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
