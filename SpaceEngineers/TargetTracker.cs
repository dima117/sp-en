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

namespace SpaceEngineers
{
    #region Copy

    // import:BlockArray.cs
    // import:Lib/TargetInfo.cs

    public class TargetTracker
    {
        const int SCAN_DELAY_MS = 60;
        const double DISTANCE_SCAN_DEFAULT = 8000;

        static readonly HashSet<MyDetectedEntityType> targetTypes = new HashSet<MyDetectedEntityType> {
            MyDetectedEntityType.SmallGrid,
            MyDetectedEntityType.LargeGrid
        };

        public static TargetInfo? Scan(
            IMyCameraBlock cam,
            double distance = DISTANCE_SCAN_DEFAULT,
            bool onlyEnemies = false)
        {
            if (cam == null)
            {
                return null;
            }

            var target = cam.Raycast(distance);

            if (target.IsEmpty())
            {
                return null;
            }

            if (!targetTypes.Contains(target.Type))
            {
                return null;
            }

            if (onlyEnemies && target.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies)
            {
                return null;
            }

            var camPos = cam.GetPosition();

            return TargetInfo.CreateTargetInfo(target, camPos, DateTime.UtcNow);
        }

        private BlockArray<IMyCameraBlock> camArray;

        public TargetInfo? Current; // последняя захваченная цель

        static Vector3D CalculateTargetLocation(
            TargetInfo info, TimeSpan timePassed)
        {
            var target = info.Entity;

            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return info.GetHitPosWorld() + (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds));
        }

        public TargetTracker(MyGridProgram program)
        {
            camArray = new BlockArray<IMyCameraBlock>(program, cam =>
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            });
        }

        public int Count
        {
            get { return camArray.Count; }
        }

        public double TotalRange
        {
            get
            {
                return camArray.Aggregate<double>(0, (a, c) => a + c.AvailableScanRange);
            }
        }

        public void UpdateCamArray()
        {
            camArray.UpdateBlocks();
        }

        public void LockOn(TargetInfo target)
        {
            Current = target;
        }

        public void Clear()
        {
            Current = null;
        }

        private MyDetectedEntityInfo TryGetUpdatedEntity(TargetInfo prevTarget, TimeSpan timePassed, DateTime now)
        {
            // вычисляем новую позицию цели
            var calculatedTargetPos = CalculateTargetLocation(prevTarget, timePassed);

            var camera = camArray.GetNext(cam => cam.CanScan(calculatedTargetPos));

            if (camera == null)
            {
                return default(MyDetectedEntityInfo);
            }

            var target = camera.Raycast(calculatedTargetPos);

            // если не удалось повторно отсканировать ту же цель
            if (target.EntityId != prevTarget.Entity.EntityId)
            {
                return default(MyDetectedEntityInfo);
            }

            return target;
        }

        public void Update()
        {
            // если цель не захвачена, то ничего не делаем
            if (!Current.HasValue)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var prevTarget = Current.Value;

            // если прошло мало времени, то ничего не делаем
            if (now < prevTarget.NextScan)
            {
                return;
            }

            var timePassed = now - prevTarget.Timestamp;


            var target = TryGetUpdatedEntity(prevTarget, timePassed, now);

            if (target.IsEmpty())
            {
                if (timePassed.TotalSeconds > 2)
                {
                    // если последнее успешное сканирование было больше 2 секунд назад
                    Clear();
                }
            }
            else
            {
                Current.Value.Update(target, now, now.AddMilliseconds(SCAN_DELAY_MS));
            }
        }
    }

    #endregion
}
