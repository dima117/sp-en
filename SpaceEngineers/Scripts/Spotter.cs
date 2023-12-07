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

        // import:Transmitter.cs
        // import:Serializer.cs
        // import:Lib/TargetInfo.cs

        private static readonly HashSet<MyDetectedEntityType> gridTypes =
            new HashSet<MyDetectedEntityType> {
                MyDetectedEntityType.SmallGrid,
                MyDetectedEntityType.LargeGrid
            };

        IMyCameraBlock cam;
        IMyTextSurface lcdTarget;
        IMyTextSurface lcdIcbm;
        Transmitter tsm;

        MyDetectedEntityInfo? target;

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
            lcdIcbm = control?.GetSurface(3);

            // антенна
            tsm = new Transmitter(this);
            tsm.Subscribe(Transmitter.TAG_ICBM_STATE, UpdateIcbmState);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void UpdateIcbmState(MyIGCMessage message)
        {
            var text = message.Data.ToString();

            Echo(text);

            lcdIcbm?.WriteText(text);
        }

        public void UpdateTargetState()
        {
            if (lcdTarget != null)
            {
                if (target.HasValue)
                {
                    var dist = (target.Value.Position - cam.GetPosition()).Length();
                    lcdTarget.WriteText($"{target.Value.Type}\ndist: {dist:0}m");
                }
                else
                {
                    lcdTarget.WriteText("NO TARGET");
                }
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // обрабатываем полученные сообщения
            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "scan":
                    var entity = cam.Raycast(15000);

                    if (gridTypes.Contains(entity.Type))
                    {
                        target = entity;

                        var obj = TargetInfo.CreateTargetInfo(entity, DateTime.UtcNow, cam.GetPosition());

                        var sb = new StringBuilder();

                        Serializer.SerializeTargetInfo(obj, sb);
                        var message = sb.ToString();

                        tsm.Send(Transmitter.TAG_TARGET_POSITION, message);

                        Me.CustomData = message;
                    }

                    break;
                case "reset":
                    target = null;

                    break;
            }

            // обновляем информацию о цели
            UpdateTargetState();
        }

        #endregion
    }
}
