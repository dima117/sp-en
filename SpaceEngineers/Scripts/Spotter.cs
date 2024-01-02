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

        readonly IMyCameraBlock cam;
        readonly Transmitter2 tsm;
        readonly IMySoundBlock sound;

        readonly IMyTextSurface lcdTarget;
        readonly IMyTextSurface lcdStatus;

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
                case "status":
                    tsm.Send(MsgTags.GET_STATUS);
                    break;
                case "start":
                    tsm.Send(MsgTags.REMOTE_START);
                    break;
                case "scan":
                    var entity = cam.Raycast(DISTANCE);

                    if (gridTypes.Contains(entity.Type))
                    {
                        var obj = TargetInfo.CreateTargetInfo(entity, DateTime.UtcNow, cam.GetPosition());

                        var msg = new StringBuilder();

                        Serializer.SerializeTargetInfo(obj, msg);

                        tsm.Send(MsgTags.REMOTE_LOCK_TARGET, msg.ToString());

                        ShowTargetState(entity);

                        sound?.Play();
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
