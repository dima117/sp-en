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

        // TODO: написать логику печати с двойным проходом
        //   - передавать максимальные скорости, размеры
        // TODO: собрать новый принтер
        //   - соединить через merge block, чтобы печатать принтер вместе
        //   - больше двигателей (включать последовательно)
        // TODO: выбирать настройки принтера через меню
        // TODO: автостыковка 

        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Grid.cs
        // import:Lib\DirectionController2.cs

        class PrintState
        {
            public int level = 0;
            public Vector3D pos;
            public Vector3D dir;
        }

        class X<T> where T : IMyTerminalBlock
        {
            public readonly T[] up;
            public readonly T[] down;
            public readonly T[] left;
            public readonly T[] right;
            public readonly T[] forward;
            public readonly T[] back;
            public readonly T[] all;

            public X(MatrixD anchor, IEnumerable<T> blocks, Func<MatrixD, Vector3D> fn)
            {
                all = blocks.ToArray();
                forward = all.Where(b => anchor.Forward == fn(b.WorldMatrix)).ToArray();
                back = all.Where(b => anchor.Backward == fn(b.WorldMatrix)).ToArray();
                up = all.Where(b => anchor.Up == fn(b.WorldMatrix)).ToArray();
                down = all.Where(b => anchor.Down == fn(b.WorldMatrix)).ToArray();
                left = all.Where(b => anchor.Left == fn(b.WorldMatrix)).ToArray();
                right = all.Where(b => anchor.Right == fn(b.WorldMatrix)).ToArray();
            }

            public override string ToString()
            {
                return $"- up: {up.Length}, down: {down.Length}\n" +
                    $"- left: {left.Length}, right: {right.Length}\n" +
                    $"- forward: {forward.Length}, back: {back.Length}";
            }
        }


        const float FACTOR = 1.4f;

        readonly IMyCockpit cockpit;
        readonly IMyGyro[] gyros;
        readonly IMyShipConnector connector;
        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        readonly IMyTextSurface lcdStatus;
        private readonly Grid grid;

        private readonly X<IMyThrust> thrusters;

        private Vector3D? directionDown;
        private Vector3D? directionForward;
        private PrintState printState;

        public Program()
        {
            grid = new Grid(GridTerminalSystem);

            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            connector = grid.GetBlocksOfType<IMyShipConnector>(w => w.CubeGrid == Me.CubeGrid).First();
            cockpit = grid.GetBlocksOfType<IMyCockpit>(w => w.CubeGrid == Me.CubeGrid).First();
            gyros = grid.GetBlocksOfType<IMyGyro>(w => w.CubeGrid == Me.CubeGrid);

            var t = grid.GetBlocksOfType<IMyThrust>(w => w.CubeGrid == Me.CubeGrid);
            thrusters = new X<IMyThrust>(cockpit.WorldMatrix, t, m => m.Backward);

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

        private void ControlThrusters(
            float mass,
            IMyThrust[] forward,
            IMyThrust[] back,
            double velocity,
            double distance)
        {
            var fPercent = 0f;
            var bPercent = 0f;

            const float vT = 1.5f;

            var ft = forward;
            var bt = back;
            var v0 = velocity;
            var d = distance;

            if (distance < 0)
            {
                ft = back;
                bt = forward;
                v0 = -velocity;
                d = -distance;
            }

            // ускорение
            var fa = ft.Sum(t => t.MaxThrust) / mass;
            var ba = bt.Sum(t => t.MaxThrust) / mass;

            // формула: a = (vT - v0) / t
            // формула: t = (vT - v0) / a
            // формула: S = v0 * t + (a * t * t) / 2

            if (v0 < 0)
            {
                // если движемся в обратную сторону, то сначала тормозим
                fPercent = 1;
            }
            else
            {
                // дистанция остановки с текущей скорости
                var t = v0 / ba;
                var s = (v0 * t) - ba * t * t / 2;

                if (s >= d)
                {
                    // если дистанция не достаточна для остановки с текущей скорости, то тормозим
                    bPercent = 1;
                }
                else if (v0 < vT)
                {
                    // если дистанция позволяет разогнаться и затормозить и скорость меньше заданной, то разгоняемся
                    fPercent = 1;
                }
            }


            // включаем двигатели
            foreach (var t in ft)
            {
                t.ThrustOverridePercentage = fPercent;
            }
            foreach (var t in bt)
            {
                t.ThrustOverridePercentage = bPercent;
            }
        }

        private double Move()
        {
            if (printState == null) { return 0; }

            const float STEP = 2.5f;

            var invertedMatrix = MatrixD.Invert(cockpit.WorldMatrix.GetOrientation());

            var velocity = cockpit.GetShipVelocities().LinearVelocity;
            var mass = cockpit.CalculateShipMass().TotalMass;

            var target = printState.dir * printState.level * STEP;
            var offset = cockpit.GetPosition() - printState.pos;

            var targetLocal = Vector3D.Transform(target, invertedMatrix);
            var offsetLocal = Vector3D.Transform(offset, invertedMatrix);
            var v0 = printState.dir.Dot(velocity);

            var dist = targetLocal.Y - offsetLocal.Y;

            ControlThrusters(mass, thrusters.up, thrusters.down, v0, dist);

            return offsetLocal.Y;
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
                    foreach (var t in thrusters.all)
                    {
                        t.Enabled = true;
                    }
                    break;
                case "stop":
                    printState = null;
                    foreach (var t in thrusters.all)
                    {
                        t.ThrustOverride = 0;
                    }
                    break;
                case "up":
                    if (printState != null)
                    {
                        printState.level++;
                    }
                    break;
                case "down":
                    if (printState != null && printState.level > 0)
                    {
                        printState.level--;
                    }
                    break;
            }

            Align();
            var currentHeight = Move();

            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {directionDown.HasValue && directionForward.HasValue}");
            sb.AppendLine($"Move: {printState != null}");
            sb.AppendLine($"Level:\n{printState?.level ?? 0:0}");
            sb.AppendLine($"Height:\n{currentHeight:0.0}");

            lcdStatus.WriteText(sb);

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        private void Start()
        {
            directionDown = cockpit.WorldMatrix.Down;
            directionForward = cockpit.WorldMatrix.Forward;
            printState = new PrintState
            {
                pos = cockpit.GetPosition(),
                dir = cockpit.WorldMatrix.Up
            };
        }

        private string FormatGPS(Vector3D point, string label)
        {
            return $"GPS:{label}:{point.X:0.00}:{point.Y:0.00}:{point.Z:0.00}:#FF89F175:";
        }

        #endregion
    }
}
