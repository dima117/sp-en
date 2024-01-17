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
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;

namespace SpaceEngineers.Scripts.Spotter
{
    public sealed class Program : MyGridProgram
    {
        #region Copy

        // import:Lib\Transmitter2.cs
        // import:Lib\Serializer.cs
        // import:Lib\TargetInfo.cs

        const double DISTANCE_RESERVE = 50;
        const double DISTANCE_DISPERSION = 25;
        const double DISTANCE_SCAN_DEFAULT = 10000;

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

        private readonly HashSet<MyDetectedEntityType> gridTypes =
            new HashSet<MyDetectedEntityType> {
                MyDetectedEntityType.SmallGrid,
                MyDetectedEntityType.LargeGrid
            };

        readonly IMyCameraBlock cam;
        readonly Transmitter2 tsm;
        readonly IMySoundBlock sound;

        readonly IMyTextSurface lcdTarget;
        readonly IMyTextSurface lcdStatus;

        private bool onlyEnemies = false;

        public Program()
        {
            // главная камера
            var list1 = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType(list1);
            cam = list1.First();
            cam.EnableRaycast = true;

            // экран
            var list2 = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(list2);

            var control = list2.FirstOrDefault(x => x.CubeGrid.EntityId == Me.CubeGrid.EntityId);
            lcdTarget = control?.GetSurface(0);
            lcdTarget.WriteText(string.Empty);

            lcdStatus = control?.GetSurface(3);
            lcdStatus.WriteText(string.Empty);

            // антенна
            tsm = new Transmitter2(this);
            tsm.Subscribe(MsgTags.REMOTE_STATUS, UpdateStatus, true);

            // динамик
            var sounds = new List<IMySoundBlock>();
            GridTerminalSystem.GetBlocksOfType(sounds);
            sound = sounds.FirstOrDefault();

            if (sound != null)
            {
                sound.SelectedSound = "ArcSoundBlockAlert2";
                sound.Range = 50;
                sound.Enabled = true;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private void UpdateStatus(MyIGCMessage message)
        {
            lcdStatus.WriteText(message.Data.ToString());
        }

        public void ShowTargetState(MyDetectedEntityInfo? target = null)
        {
            if (lcdTarget != null)
            {
                if (target.HasValue)
                {
                    var name = GetName(target.Value.EntityId);
                    var dist = (target.Value.Position - cam.GetPosition()).Length();
                    lcdTarget.WriteText($"{target.Value.Type}\n{name}\ndist: {dist:0}m");
                }
                else
                {
                    lcdTarget.WriteText("NO TARGET");
                }
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "status":
                    tsm.Send(MsgTags.GET_STATUS);
                    break;
                case "start":
                    tsm.Send(MsgTags.REMOTE_START);
                    break;
                case "scan":
                    var target = ScanArea();

                    if (target != null)
                    {
                        var msg = new StringBuilder();

                        Serializer.SerializeTargetInfo(target.Value, msg);

                        tsm.Send(MsgTags.REMOTE_LOCK_TARGET, msg.ToString());

                        ShowTargetState(target.Value.Entity);

                        sound?.Play();
                    }

                    break;
                case "reset":
                    ShowTargetState();

                    break;
            }
        }

        public TargetInfo? ScanArea(double distance = DISTANCE_SCAN_DEFAULT)
        {
            if (cam == null)
            {
                return null;
            }

            var up = cam.WorldMatrix.Up * DISTANCE_DISPERSION;
            var left = cam.WorldMatrix.Left * DISTANCE_DISPERSION;
            var camPos = cam.GetPosition();

            var targetPos = camPos + distance * cam.WorldMatrix.Forward;

            return Scan(targetPos)
                ?? Scan(targetPos + left)
                ?? Scan(targetPos - left)
                ?? Scan(targetPos + up)
                ?? Scan(targetPos - up);
        }

        private TargetInfo? Scan(Vector3D targetPos)
        {
            Me.CustomData = $"GPS:SCAN #1:{targetPos.X:0.00}:{targetPos.Y:0.00}:{targetPos.Z:0.00}:#FF75C9F1:";
            var entity = cam.Raycast(targetPos);

            if (entity.IsEmpty())
            {
                return null;
            }

            if (!gridTypes.Contains(entity.Type))
            {
                return null;
            }

            if (onlyEnemies && entity.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies)
            {
                return null;
            }

            return TargetInfo.CreateTargetInfo(entity, DateTime.UtcNow, cam.GetPosition());
        }

        #endregion
    }
}
