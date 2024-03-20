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

namespace SpaceEngineers.Scripts.Printer
{
    public class Program : MyGridProgram
    {
        // скрипт для выравнивания челнока в принтере кораблей
        // гироскоп должен быть сориентирован по направлению кабины
        // remote control а принтере должен быть сориентирован вверх

        // TODO: выводить на экран текущие скорость и остаток расстояния по осям
        // TODO: передавать максимальные скорости, размеры
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
            public int index = 0;
            public Vector3D position;
            public Vector3D[] points;
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

        class OneDimensionMovementController
        {

            private readonly IMyThrust[] forward;
            private readonly IMyThrust[] back;

            public OneDimensionMovementController(IMyThrust[] forward, IMyThrust[] back)
            {
                this.forward = forward;
                this.back = back;
            }

            public static bool ControlThrusters(
                float mass,
                IMyThrust[] forward,
                IMyThrust[] back,
                double velocity,
                double distance)
            {
                var fPercent = 0f;
                var bPercent = 0f;

                const float vT = 1.5f;

                // ускорение
                var fa = forward.Sum(t => t.MaxThrust) / mass;
                var ba = back.Sum(t => t.MaxThrust) / mass;

                // формула: a = (vT - v0) / t
                // формула: t = (vT - v0) / a
                // формула: S = v0 * t + (a * t * t) / 2

                if (velocity < 0)
                {
                    // если движемся в обратную сторону, то сначала тормозим
                    fPercent = 1;
                }
                else
                {
                    // время и дистанция остановки с текущей скорости
                    var t = velocity / ba;
                    var s = (velocity * t) - ba * t * t / 2;

                    if (s >= distance)
                    {
                        // если дистанция не достаточна для остановки с текущей скорости, то тормозим
                        bPercent = 1;
                    }
                    else if (velocity < vT)
                    {
                        // если дистанция позволяет разогнаться и затормозить и скорость меньше заданной, то разгоняемся
                        fPercent = 1;
                    }
                }


                // включаем двигатели
                foreach (var t in forward)
                {
                    t.ThrustOverridePercentage = fPercent;
                }

                foreach (var t in back)
                {
                    t.ThrustOverridePercentage = bPercent;
                }

                // возвращаем true, если было движение
                return distance > 0.2f && Math.Abs(velocity) > 0.2f;
            }

            public bool Update(float mass, double velocity, double currentPos, double targetPos)
            {
                if (targetPos > currentPos)
                {
                    // движение вперед
                    return ControlThrusters(mass, forward, back, velocity, targetPos - currentPos);
                }
                else
                {
                    // движение назад
                    return ControlThrusters(mass, back, forward, -velocity, currentPos - targetPos);
                }
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
        private readonly OneDimensionMovementController moveV;
        private readonly OneDimensionMovementController moveH;

        private Vector3D? directionDown;
        private Vector3D? directionForward;
        private PrintState printState;

        private bool sameGrid<T>(T b) where T : IMyTerminalBlock
        {
            return b.CubeGrid == Me.CubeGrid;
        }

        public Program()
        {
            grid = new Grid(GridTerminalSystem);

            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            connector = grid.GetBlocksOfType<IMyShipConnector>(sameGrid).First();
            cockpit = grid.GetBlocksOfType<IMyCockpit>(sameGrid).First();
            gyros = grid.GetBlocksOfType<IMyGyro>(sameGrid);

            var t = grid.GetBlocksOfType<IMyThrust>(sameGrid);
            thrusters = new X<IMyThrust>(cockpit.WorldMatrix, t, m => m.Backward);

            moveV = new OneDimensionMovementController(thrusters.up, thrusters.down);
            moveH = new OneDimensionMovementController(thrusters.right, thrusters.left);

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

        private void Move()
        {
            if (printState == null) { return; }

            if (printState.index < 0 || printState.index >= printState.points.Length) { return; }

            var mass = cockpit.CalculateShipMass().TotalMass;
            var velocity = cockpit.GetShipVelocities().LinearVelocity;
            var matrix = MatrixD.Invert(cockpit.WorldMatrix.GetOrientation());

            var target = printState.points[printState.index];
            var offset = cockpit.GetPosition() - printState.position;

            var targetLocal = Vector3D.Transform(target, matrix);
            var offsetLocal = Vector3D.Transform(offset, matrix);
            var velocityLocal = Vector3D.Transform(velocity, matrix);
            //var v0 = printState.dir.Dot(velocity);

            if (moveV.Update(mass, velocityLocal.Y, offsetLocal.Y, targetLocal.Y))
            {
                return;
            }

            if (moveH.Update(mass, velocityLocal.X, offsetLocal.X, targetLocal.X))
            {
                return;
            }

            printState.index++;
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
                    directionForward = null;
                    Me.CustomData = string.Empty;
                    Unlock();
                    break;
                case "start":
                    Start(20, 50);
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
            }

            Align();
            Move();

            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {directionDown.HasValue && directionForward.HasValue}");
            sb.AppendLine($"Move: {printState != null}");
            sb.AppendLine($"Point:\n{printState?.index ?? 0:0}");

            lcdStatus.WriteText(sb);

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        private void Start(
            int width, // смещение вбок (в блоках)
            int length) // длина (в блоках)
        {
            var STEP = 2.5;
            var offset = Math.Ceiling(width / 2f) * STEP;

            var pos = cockpit.GetPosition();
            var m = cockpit.WorldMatrix;

            var left = m.Left * offset;
            var right = m.Right * offset;
            var up = m.Up * STEP;

            // формируем траекторию движения
            var list = new List<Vector3D>();

            for (var level = 0; level < length; level++)
            {
                var pos1 = pos + level * up;
                list.Add(pos1 + right);
                list.Add(pos1 + left);
                list.Add(pos1 + right);
                list.Add(pos1 + left);
                list.Add(pos1);
                list.Add(pos1 + up);
            }

            directionDown = m.Down;
            directionForward = m.Forward;
            printState = new PrintState
            {
                position = pos,
                points = list.ToArray()
            };
        }

        private string FormatGPS(Vector3D point, string label)
        {
            return $"GPS:{label}:{point.X:0.00}:{point.Y:0.00}:{point.Z:0.00}:#FF89F175:";
        }

        #endregion
    }
}
