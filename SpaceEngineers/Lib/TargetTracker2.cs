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

namespace SpaceEngineers.Lib
{
    #region Copy

    // import:../BlockArray.cs
    // import:Lib/TargetInfo.cs

    public class TargetTracker2
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

        const double DISTANCE_RESERVE = 50;
        const double DISTANCE_SCAN_DEFAULT = 15000;

        private BlockArray<IMyCameraBlock> camArray;
        private Dictionary<long, TargetInfo> targets = new Dictionary<long, TargetInfo>();

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

        static readonly HashSet<MyDetectedEntityType> targetTypes = new HashSet<MyDetectedEntityType> {
            MyDetectedEntityType.SmallGrid,
            MyDetectedEntityType.LargeGrid
        };

        public TargetTracker2(MyGridProgram program)
        {
            camArray = new BlockArray<IMyCameraBlock>(program, cam =>
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            });
        }

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

            return TargetInfo.CreateTargetInfo(target, DateTime.UtcNow, camPos);
        }

        static Vector3D CalculateTargetLocation(TargetInfo info, TimeSpan timePassed)
        {
            var target = info.Entity;

            // прежние координаты + вектор скорости (м/с) * время с последнего захвата (с)
            return target.Position +
                (target.Velocity * Convert.ToSingle(timePassed.TotalSeconds)) +
                Vector3D.Transform(info.HitPos, target.Orientation);
        }

        public void UpdateCamArray()
        {
            camArray.UpdateBlocks();
        }

        public void LockTarget(TargetInfo target)
        {
            var id = target.Entity.EntityId;

            if (!targets.ContainsKey(id) || target.Timestamp > targets[id].Timestamp)
            {
                targets[id] = target;
            }
        }

        public void ReleaseTarget(long targetId)
        {
            targets.Remove(targetId);
        }


        /*
            хранит данные о списке целей
            отображает информацию о целях: читаемое имя, тип, скорость, расстояние
            умеет сериализовать цели для отправки и мержить списки с более новыми данными
            если цель давно не обновлялась, подсвечивает ее
            api: 1) отсканировать цель 2) обновить очредную цель
            предоставляет инфо о целях, отсортированных по id
            
                

        */
    }

    #endregion
}
