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

namespace SpaceEngineers
{
    public class TargetTracker
    {
        private BlockArray<IMyCameraBlock> camArray;

        public MyDetectedEntityInfo CurrentTarget; // последняя захваченная цель
        public Vector3D Offset; // смещение точки прицеливания относительно геометрического центра цели
        public DateTime LastLock; // время последнего обновления захвата
        public double ScanDelayMs; // время следующего сканирования

        const double DISTANCE_RESERVE = 50;
        const double DISTANCE_SCAN_DEFAULT = 10000;

        static Vector3D CalculateTargetLocation(
            MyDetectedEntityInfo target, TimeSpan timePassed, Vector3D relativeOffset)
        {
            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return target.Position +
                (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds)) +
                Vector3D.Transform(relativeOffset, target.Orientation);
        }

        public int Count
        {
            get { return camArray.Count; }
        }

        public TargetTracker(MyGridProgram program, string prefix = "Camera")
        {
            camArray = new BlockArray<IMyCameraBlock>(program, prefix, cam => cam.EnableRaycast = true);
        }

        public void UpdateCamArray()
        {
            camArray.UpdateBlocks();
        }

        public void Clear()
        {
            CurrentTarget = default(MyDetectedEntityInfo);
            LastLock = default(DateTime);
            Offset = default(Vector3D);
            ScanDelayMs = 0;
        }

        private void UpdateTarget(IMyCameraBlock camera, MyDetectedEntityInfo target, bool resetTarget = false)
        {
            var now = DateTime.UtcNow;

            if (!target.IsEmpty())
            {
                CurrentTarget = target;
                LastLock = now;

                var camPos = camera.GetPosition();

                var distance = (target.Position - camPos).Length() + DISTANCE_RESERVE;

                // считаем, сколько мс нужно для накопления камерами дистанции до цели
                // камера накапливает дистанцию 2 м/мс
                ScanDelayMs = distance / camArray.Count / 2;

                // hitpos
                if (resetTarget && target.HitPosition.HasValue)
                {
                    var hitPos = target.HitPosition.Value;

                    // ставим метку на 2 метра вперед от точки пересечения
                    var relativeHitPos = hitPos - target.Position + Vector3D.Normalize(hitPos - camPos) * 2;
                    var invertedMatrix = MatrixD.Invert(target.Orientation);

                    Offset = Vector3D.Transform(relativeHitPos, invertedMatrix);
                } else
                {
                    Offset = default(Vector3D);
                }
            }
            else
            {
                // если была смена цели или последнее успешное сканирование было больше 2 секунд назад
                if (resetTarget || (now - LastLock).TotalSeconds > 2)
                {
                    Clear();
                }
            }
        }

        public void LockOn(double distance = DISTANCE_SCAN_DEFAULT)
        {
            var camera = camArray.GetNext(cam => cam.CanScan(distance));

            var target = camera?.Raycast(distance) ?? default(MyDetectedEntityInfo);

            UpdateTarget(camera, target, true);
        }

        public void LockOn(Vector3D pos)
        {
            var camera = camArray.GetNext(cam => cam.CanScan(pos));

            var target = camera?.Raycast(pos) ?? default(MyDetectedEntityInfo);

            UpdateTarget(camera, target, true);
        }

        public void Update()
        {
            var timePassed = DateTime.UtcNow - LastLock;

            // если цель не захвачена или прошло мало времени, то ничего не делаем
            if (CurrentTarget.IsEmpty() || timePassed.TotalMilliseconds < ScanDelayMs)
            {
                return;
            }

            // вычисляем новую позицию цели
            var calculatedTargetPos = CalculateTargetLocation(CurrentTarget, timePassed, Offset);

            var camera = camArray.GetNext(cam => cam.CanScan(calculatedTargetPos));

            var target = camera?.Raycast(calculatedTargetPos) ?? default(MyDetectedEntityInfo);

            UpdateTarget(camera, target);
        }
    }
}
