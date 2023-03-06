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
        private MyGridProgram program;
        private readonly string prefix;

        private BlockArray<IMyCameraBlock> camArray;

        public MyDetectedEntityInfo CurrentTarget; // последняя захваченная цель
        public DateTime LastLock; // время последнего обновления захвата

        const int DISTANCE_RESERVE = 50;
        const int DISTANCE_SCAN_DEFAULT = 10000;

        static Vector3D CalculateTargetLocation(MyDetectedEntityInfo target, TimeSpan timePassed)
        {
            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return target.Position + (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds));
        }

        public TargetTracker(MyGridProgram program, string prefix = "Camera")
        {
            this.program = program;
            this.prefix = prefix;
            
            camArray = new BlockArray<IMyCameraBlock>(program, prefix, cam => cam.EnableRaycast = true);
        }

        public void UpdateCamArray()
        {
            camArray.UpdateBlocks();
        }

        // TODO: возможна ошибка из-за того, что сканирует не та камера, через которую прицеливаемся
        public bool LockOn(long distance = DISTANCE_SCAN_DEFAULT)
        {
            // TODO: вместо boolean возвращать инфу о параметрах сканирования
            // TODO: сделать захват заданной позиции
            var camera = camArray.GetNext(cam => cam.CanScan(distance));

            if (camera != null)
            {
                var target = camera.Raycast(distance);

                if (!target.IsEmpty())
                {
                    CurrentTarget = target;
                    LastLock = DateTime.UtcNow;
                    return true;
                }
            }

            return false;
        }

        public bool Update(Vector3D myPos)
        {
            // если цель не захвачена, то ничего не делаем
            if (!CurrentTarget.IsEmpty())
            {
                // вычисляем новую позицию цели
                var timePassed = DateTime.UtcNow - LastLock;
                var calculatedTargetPos = CalculateTargetLocation(CurrentTarget, timePassed);

                var distance = (calculatedTargetPos - myPos).Length() + DISTANCE_RESERVE;

                // считаем, сколько мс нужно для накопления камерами дистанции до цели
                // камера накапливает дистанцию 2 м/мс
                var delayMs = distance / camArray.Count / 2;

                if (timePassed.TotalMilliseconds > delayMs)
                {
                    var camera = camArray.GetNext(cam => cam.CanScan(calculatedTargetPos));

                    if (camera != null)
                    {
                        var target = camera.Raycast(calculatedTargetPos);

                        if (!target.IsEmpty())
                        {
                            CurrentTarget = target;
                            LastLock = DateTime.UtcNow;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
