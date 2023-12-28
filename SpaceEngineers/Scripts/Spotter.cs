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

        const int DISTANCE = 60000;

        private static readonly HashSet<MyDetectedEntityType> gridTypes =
            new HashSet<MyDetectedEntityType> {
                MyDetectedEntityType.SmallGrid,
                MyDetectedEntityType.LargeGrid
            };

        IMyCameraBlock cam;

        IMyTextSurface lcdTarget;

        Transmitter2 tsm;

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

            // антенна
            tsm = new Transmitter2(this);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void ShowTargetState(MyDetectedEntityInfo? target = null)
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
            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "scan":
                    var entity = cam.Raycast(DISTANCE);

                    if (gridTypes.Contains(entity.Type))
                    {
                        var obj = TargetInfo.CreateTargetInfo(entity, DateTime.UtcNow, cam.GetPosition());

                        var msg = new StringBuilder();

                        Serializer.SerializeTargetInfo(obj, msg);

                        tsm.Send(MsgTags.LOCK_TARGET, msg.ToString());

                        ShowTargetState(entity);
                    }

                    break;
                case "reset":
                    ShowTargetState();

                    break;
            }
        }

        #endregion
    }
}
