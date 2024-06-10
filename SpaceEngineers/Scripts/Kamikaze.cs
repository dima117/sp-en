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
using SpaceEngineers.Lib;
using static Sandbox.Game.Weapons.MyDrillBase;
using Sandbox.Game.Entities;
using SpaceEngineers.Scripts.Torpedos;

namespace SpaceEngineers.Scripts.Kamikaze
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Grid.cs
        // import:Lib\DirectionController2.cs
        // import:Lib\TargetTracker.cs

        readonly IMyTextSurface lcd;

        readonly Grid grid;
        readonly DirectionController2 dc;
        readonly TargetTracker tt;
        readonly IMyCameraBlock cam;
        readonly IMyCockpit cockpit;
        readonly List<IMyGyro> listGyro = new List<IMyGyro>();
        readonly List<IMyThrust> listEngine = new List<IMyThrust>();


        bool started = false;

        public Program()
        {
            grid = new Grid(GridTerminalSystem);

            cam = grid.GetCamera("camera_main");

            var group = GridTerminalSystem.GetBlockGroupWithName("kamikaze");

            cockpit = grid.GetByFilterOrAny<IMyCockpit>();
            group.GetBlocksOfType(listGyro);
            group.GetBlocksOfType(listEngine);

            var cameras = grid.GetBlocksOfType<IMyCameraBlock>();
            tt = new TargetTracker(cameras);
            dc = new DirectionController2(cockpit, listGyro);

            lcd = grid.GetBlockWithName<IMyTextPanel>("lcd");
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            DateTime now = DateTime.UtcNow;

            switch (argument)
            {
                case "lock":
                    var target = TargetTracker.Scan(now, cam, 7000);

                    if (target != null)
                    {
                        tt.LockTarget(target);

                        listGyro.ForEach(g =>
                        {
                            g.Enabled = true;
                            g.GyroOverride = true;
                        });

                        listEngine.ForEach(e =>
                        {
                            e.Enabled = true;
                            e.ThrustOverridePercentage = 1;
                        });

                        started = true;
                    }

                    break;
                case "reset":
                    tt.Clear();

                    listGyro.ForEach(g => g.GyroOverride = false);
                    listEngine.ForEach(e => e.ThrustOverridePercentage = 0);
                    started = false;

                    break;
                default:
                    if (started)
                    {
                        tt.Update(now);
                        Update(tt.Current);
                    }
                    break;
            }

            var sb = new StringBuilder();
            sb.AppendLine(tt.Current == null ? "NO TARGET" : "AIMED");
            sb.AppendLine($"count: {tt.Count}");
            sb.AppendLine($"gyro: {listGyro.Count}");

            lcd.WriteText(sb);
        }

        const double INTERCEPT_DISTANCE = 1200;


        public void Update(TargetInfo target)
        {
            double distance = 0;
            if (target != null)
            {
                distance = (target.Entity.Position - cockpit.GetPosition()).Length();

                SetDirection(target, distance);
            }
        }

        void SetDirection(TargetInfo targetInfo, double distance)
        {
            var targetPos = targetInfo.GetHitPosWorld();

            if (distance < INTERCEPT_DISTANCE)
            {
                // при большом расстоянии до цели точка перехвата далеко перемещается
                // при маневрах цели, поэтому рассчитываем её только на дальности до 1 км
                dc.Intercept(targetPos, targetInfo.Entity.Velocity);
            }
            else
            {
                dc.Aim(targetPos);
            }
        }

        #endregion
    }
}
