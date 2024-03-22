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
        // TODO: динамическое формирование маршрута с учетом изменения массы
        // TODO: собрать новый принтер
        //   - соединить через merge block, чтобы печатать принтер вместе
        //   - убрать звигатель, на который попадает пламя, заменить двигатели на большие
        //   - добавить две полосы брони, чтобы не проваривались роторы и наклонные блоки

        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Grid.cs
        // import:Lib\DirectionController2.cs

        class Settings
        {
            public int width;
            public int height;
            public Action<int, int> start;

            int index = 0;

            public Settings(Action<int, int> start, int height = 50, int width = 22)
            {
                this.start = start;
                this.height = height;
                this.width = width;
            }

            public void Prev()
            {
                if (index > 0)
                {
                    index--;
                }
            }
            public void Next()
            {
                if (index < 5)
                {
                    index++;
                }
            }

            public void Apply()
            {
                switch (index)
                {
                    case 0:
                        height++;
                        break;
                    case 1:
                        height--;
                        break;
                    case 2:
                        width++;
                        break;
                    case 3:
                        width--;
                        break;
                    case 4:
                        start(width, height);
                        break;
                }
            }

            private string Cur(int i)
            {
                return index == i ? "> " : "";

            }

            public override string ToString()
            {
                var sb = new StringBuilder();

                sb.AppendLine("Parameters:\n--");
                sb.AppendLine($"{Cur(0)}Height ++ {height}");
                sb.AppendLine($"{Cur(1)}Height --");
                sb.AppendLine($"{Cur(2)}Width ++ {width}");
                sb.AppendLine($"{Cur(3)}Width --");
                sb.AppendLine($"{Cur(4)}Start");

                return sb.ToString();
            }
        }

        class PrintState
        {
            public int index = 0;
            public Vector3D position;
            public Vector3D[] points;
            public DateTime timestamp;
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
            enum ThrustDirection
            {
                None,
                Forward,
                Backward,
            }


            private readonly IMyThrust[] forward;
            private readonly IMyThrust[] back;
            private readonly float vMax;

            public OneDimensionMovementController(IMyThrust[] forward, IMyThrust[] back, float vMax)
            {
                this.forward = forward;
                this.back = back;
                this.vMax = vMax;
            }

            public static void SetThrust(IMyThrust[] thrusters, float mass, float a)
            {
                var totalThrust = mass * a;

                foreach (var t in thrusters)
                {
                    var thrust = Math.Max(0, Math.Min(t.MaxThrust, totalThrust));

                    t.ThrustOverride = thrust;
                    totalThrust -= thrust;
                }
            }

            public static void ControlThrusters(
                float mass,
                IMyThrust[] forward,
                IMyThrust[] back,
                double velocity,
                double distance,
                float vMax)
            {
                var direction = ThrustDirection.None;

                const float aMax = 1f;

                var thrustForward = forward.Sum(t => t.MaxThrust);
                var thrustBack = back.Sum(t => t.MaxThrust);

                // ускорение
                var fa = Math.Min(thrustForward / mass, aMax);
                var ba = Math.Min(thrustBack / mass, aMax);

                // формула: a = (vT - v0) / t
                // формула: t = (vT - v0) / a
                // формула: S = v0 * t + (a * t * t) / 2

                if (velocity < 0)
                {
                    // если движемся в обратную сторону, то сначала тормозим
                    direction = ThrustDirection.Forward;
                }
                else
                {
                    // время и дистанция остановки с текущей скорости
                    var t = velocity / ba;
                    var s = (velocity * t) - ba * t * t / 2;

                    if (s >= distance)
                    {
                        // если дистанция не достаточна для остановки с текущей скорости, то тормозим
                        direction = ThrustDirection.Backward;
                    }
                    else if (velocity < vMax)
                    {
                        // если дистанция позволяет разогнаться и затормозить и скорость меньше заданной, то разгоняемся
                        direction = ThrustDirection.Forward;
                    }
                }


                // включаем двигатели
                SetThrust(forward, mass, direction == ThrustDirection.Forward ? fa : 0);
                SetThrust(back, mass, direction == ThrustDirection.Backward ? ba : 0);
            }

            public void Update(float mass, double velocity, double currentPos, double targetPos)
            {
                if (targetPos > currentPos)
                {
                    // движение вперед
                    ControlThrusters(mass, forward, back, velocity, targetPos - currentPos, vMax);
                }
                else
                {
                    // движение назад
                    ControlThrusters(mass, back, forward, -velocity, currentPos - targetPos, vMax);
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
        readonly IMyTextSurface lcdDebug;

        private readonly Grid grid;
        private readonly X<IMyThrust> thrusters;
        private readonly OneDimensionMovementController moveX;
        private readonly OneDimensionMovementController moveY;
        private readonly OneDimensionMovementController moveZ;
        private readonly Settings settings;

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

            moveX = new OneDimensionMovementController(thrusters.right, thrusters.left, 0.75f);
            moveY = new OneDimensionMovementController(thrusters.up, thrusters.down, 0.2f);
            moveZ = new OneDimensionMovementController(thrusters.back, thrusters.forward, 0.2f);
            settings = new Settings(Start);

            lcdStatus = cockpit.GetSurface(0);
            lcdDebug = cockpit.GetSurface(2);

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
            if (printState == null) { return; }

            var invalidState = printState.index < 0 || printState.index >= printState.points.Length;
            var pause = DateTime.UtcNow < printState.timestamp;

            if (invalidState || pause)
            {
                // если закончили печать или на паузе, то останавливаемся
                cockpit.DampenersOverride = true;

                foreach (var t in thrusters.all)
                {
                    t.ThrustOverride = 0;
                }

                return;
            }

            cockpit.DampenersOverride = false;

            var mass = cockpit.CalculateShipMass().TotalMass;
            var velocity = cockpit.GetShipVelocities().LinearVelocity;
            var matrix = MatrixD.Invert(cockpit.WorldMatrix.GetOrientation());

            var target = printState.points[printState.index] - printState.position;
            var offset = cockpit.GetPosition() - printState.position;

            var targetLocal = Vector3D.Transform(target, matrix);
            var offsetLocal = Vector3D.Transform(offset, matrix);
            var velocityLocal = Vector3D.Transform(velocity, matrix);

            // control
            moveX.Update(mass, velocityLocal.X, offsetLocal.X, targetLocal.X);
            moveY.Update(mass, velocityLocal.Y, offsetLocal.Y, targetLocal.Y);
            moveZ.Update(mass, velocityLocal.Z, offsetLocal.Z, targetLocal.Z);

            // status
            var sb = new StringBuilder();
            sb.AppendLine("Print state:\n--");
            sb.AppendLine($"Height: {offsetLocal.Y:0.00} / {targetLocal.Y:0.00}");
            sb.AppendLine($"Offset: {offsetLocal.X:0.00} / {targetLocal.X:0.00}");
            sb.AppendLine($"Diff: {(target - offset).Length():0.00}");
            sb.AppendLine($"Velocity: X {velocityLocal.X:0.00} / Y {velocityLocal.Y:0.00}");
            sb.AppendLine($"Point: {printState.index + 1} / {printState.points.Length}");
            sb.AppendLine("--\n> Stop");
            lcdDebug.WriteText(sb);

            // check next point
            if ((target - offset).Length() < 0.5 && velocityLocal.X < 0.5 && velocityLocal.Y < 0.5)
            {
                printState.index++;
                printState.timestamp = DateTime.UtcNow.AddSeconds(4);
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

            Align();

            if (printState == null)
            {
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
                    case "up":
                        settings.Prev();
                        break;
                    case "down":
                        settings.Next();
                        break;
                    case "apply":
                        settings.Apply();
                        break;
                }

                lcdDebug.WriteText(settings.ToString());
            }
            else
            {
                switch (argument)
                {
                    case "apply":
                        printState = null;
                        cockpit.DampenersOverride = true;
                        foreach (var t in thrusters.all)
                        {
                            t.ThrustOverride = 0;
                        }
                        break;
                    default:
                        Move();
                        break;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {directionDown.HasValue && directionForward.HasValue}");
            lcdStatus.WriteText(sb);

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        private void Start(
            int width, // ширина вбок (в блоках)
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

            list.Add(pos + left);

            for (var level = 0; level < length; level++)
            {
                var pos1 = pos + level * up;
                list.Add(pos1 + right);
                list.Add(pos1 + left);
                //list.Add(pos1 + right);
                //list.Add(pos1 + left);

                // важно, чтобы изменение высоты было вне области действия сварщиков
                // т.к. изменение массы корабля может помешать позиционированию
                list.Add(pos1 + left+ up);
            }

            directionDown = m.Down;
            directionForward = m.Forward;
            printState = new PrintState
            {
                position = pos,
                points = list.ToArray(),
                timestamp = DateTime.UtcNow
            };

            foreach (var t in thrusters.all)
            {
                t.Enabled = true;
            }
        }

        private string FormatGPS(Vector3D point, string label)
        {
            return $"GPS:{label}:{point.X:0.00}:{point.Y:0.00}:{point.Z:0.00}:#FF89F175:";
        }

        #endregion
    }
}
