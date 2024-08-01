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
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Collections;
using System.Reflection;

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:TargetInfo.cs

    // одиночный трекер
    public class TargetTracker
    {
        public static readonly string[] names = new[] {
            "корова",
            "пёс",
            "кролик",
            "конь",
            "медвед",
            "кот",
            "болт",
            "кабан",
            "волк",
            "бобр",
            "жук",
            "zombie",
            "сом",
        };

        public static string GetName(long entityId)
        {
            var name = names[entityId % names.Length];
            var index = entityId % 89;

            return $"{name}-{index}";
        }

        const int SCAN_DELAY_MS = 20;
        const double DISTANCE_SCAN_DEFAULT = 7500;
        const int TARGET_RELEASE_TIMEOUT = 2;
        const int AI_INTERCEPT_PATTERN_ID = 3;

        static readonly HashSet<MyDetectedEntityType> targetTypes = new HashSet<MyDetectedEntityType> {
            MyDetectedEntityType.SmallGrid,
            MyDetectedEntityType.LargeGrid
        };

        readonly IMyCameraBlock[] cameras;
        readonly IMyOffensiveCombatBlock ai;
        readonly IMyFlightMovementBlock flight;

        private int camIndex = 0;

        public TargetInfo Current;

        public Vector3D? CurrentAiTarget => flight?.CurrentWaypoint == null ? null as Vector3D? : new Vector3D(flight.CurrentWaypoint.Matrix.GetRow(3));

        public event Action TargetLocked;
        public event Action TargetReleased;

        public int Count
        {
            get { return cameras.Length; }
        }

        public double TotalRange
        {
            get
            {
                return cameras.Aggregate(0d, (a, c) => a + c.AvailableScanRange);
            }
        }

        public TargetTracker(IMyCameraBlock[] cameras, IMyOffensiveCombatBlock ai = null, IMyFlightMovementBlock flight = null)
        {
            this.cameras = cameras;

            foreach (var cam in this.cameras)
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            }

            this.ai = ai;
            this.flight = flight;

            if (ai != null && flight != null)
            {
                ai.Enabled = true;
                ai.UpdateTargetInterval = 5;
                ai.TargetPriority = OffensiveCombatTargetPriority.Largest;
                ai.SelectedAttackPattern = AI_INTERCEPT_PATTERN_ID;

                flight.Enabled = true;
                flight.PrecisionMode = false;
                flight.AlignToPGravity = false;
                flight.CollisionAvoidance = false;
                flight.FlightMode = FlightMode.OneWay;
                flight.MinimalAltitude = 0;
                flight.SpeedLimit = 100;
            }
        }

        private static TargetInfo GetTargetInfo(DateTime now, IMyCameraBlock cam, MyDetectedEntityInfo entity)
        {
            if (entity.IsEmpty())
            {
                return null;
            }

            if (!targetTypes.Contains(entity.Type))
            {
                return null;
            }

            var camPos = cam.GetPosition();

            return TargetInfo.CreateTargetInfo(entity, camPos, now);
        }

        public static TargetInfo Scan(
            DateTime now,
            IMyCameraBlock cam,
            double distance = DISTANCE_SCAN_DEFAULT)
        {
            if (cam == null)
            {
                return null;
            }

            var entity = cam.Raycast(distance);

            return GetTargetInfo(now, cam, entity);
        }

        private bool CanScan(IMyCameraBlock cam, Vector3D targetPos)
        {
            var canScan = cam.IsFunctional && cam.Enabled && cam.CanScan(targetPos);

            switch (cam.BlockDefinition.SubtypeId)
            {
                case "LargeCameraTopMounted":
                    var dir = targetPos - cam.GetPosition();
                    var isInArea = cam.WorldMatrix.Up.Dot(dir) > 0.2; // цель сверху и угол больше 10 градусов
                    return canScan && isInArea;
                default:
                    return canScan;
            }
        }

        public bool TryLockPosition(DateTime now, Vector3D targetPos)
        {
            var cam = cameras.FirstOrDefault(x => CanScan(x, targetPos));

            if (cam == null) { return false; }

            var entity = cam.Raycast(targetPos);

            var target = GetTargetInfo(now, cam, entity);

            if (target == null) { return false; }

            LockTarget(target);

            return true;
        }

        public void LockTarget(TargetInfo target)
        {
            if (target != null)
            {
                Current = target;
                TargetLocked?.Invoke();
            }
        }

        public void Clear()
        {
            if (Current != null)
            {
                Current = null;
                TargetReleased?.Invoke();
            }
        }

        public void Update(DateTime now)
        {
            // если цель не захвачена, то ничего не делаем
            if (Current == null)
            {
                return;
            }

            var prevTarget = Current;

            // если прошло мало времени, то ничего не делаем
            if (now < prevTarget.NextScan)
            {
                return;
            }

            var timePassed = now - prevTarget.Timestamp;

            // ScanNextPosition(target, timePassed);

            var entity = ScanNextPosition(prevTarget, timePassed);

            if (entity.IsEmpty())
            {
                if (timePassed.TotalSeconds > TARGET_RELEASE_TIMEOUT)
                {
                    // если последнее успешное сканирование было больше 2 секунд назад
                    Clear();
                }
            }
            else
            {
                Current.Update(entity, now, now.AddMilliseconds(SCAN_DELAY_MS));
            }
        }

        private static Vector3D CalculateTargetLocation(TargetInfo info, TimeSpan timePassed)
        {
            var target = info.Entity;

            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return info.GetHitPosWorld() + (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds));
        }

        private MyDetectedEntityInfo ScanNextPosition(TargetInfo prevTarget, TimeSpan timePassed)
        {
            // вычисляем новую позицию цели
            var calculatedTargetPos = CalculateTargetLocation(prevTarget, timePassed);

            var camera = GetNext(cameras, ref camIndex, cam => cam.CanScan(calculatedTargetPos));

            if (camera == null)
            {
                return default(MyDetectedEntityInfo);
            }

            var entity = camera.Raycast(calculatedTargetPos);

            // если не удалось повторно отсканировать ту же цель
            if (entity.EntityId != prevTarget.Entity.EntityId)
            {
                return default(MyDetectedEntityInfo);
            }

            return entity;
        }

        public static T GetNext<T>(T[] a, ref int index, Func<T, bool> filter = null)
        {
            if (a.Length == 0)
            {
                return default(T);
            }

            for (var count = 0; count < a.Length; count++)
            {
                index = (index + 1) % a.Length;

                T block = a[index];

                if (filter == null || filter(block))
                {
                    return block;
                }
            }

            return default(T);
        }
    }

    #endregion
}
