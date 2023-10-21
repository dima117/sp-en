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

namespace SpaceEngineers.Scripts.Spotter
{
    public sealed class Program : MyGridProgram
    {
        #region Copy

        IMyCameraBlock cam;
        IMyTextSurface lcd;
        IMyRadioAntenna antenna;

        MyDetectedEntityInfo? target;
        DateTime stopBroadcast = DateTime.MinValue;
        Stack<MyDetectedEntityInfo> messages = new Stack<MyDetectedEntityInfo>();
        IMyBroadcastListener listener;

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
            lcd = list2.FirstOrDefault()?.GetSurface(3);

            // антенна
            var list3 = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType(list3);
            antenna = list3.FirstOrDefault();
            if (antenna != null)
            {
                antenna.Radius = 10;
                antenna.Enabled = true;

                listener = IGC.RegisterBroadcastListener("icbm_was_launched");
                listener.SetMessageCallback("icbm_was_launched");
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // устанавливаем радиус антенны
            antenna.Radius = DateTime.UtcNow > stopBroadcast ? 10 : 50000;

            while (messages.Any())
            {
                var message = messages.Pop();

                IGC.SendBroadcastMessage("TARGET_POSITION", message.Position);
            }

            switch (argument)
            {
                case "icbm_was_launched":

                    break;
                case "shot":
                    var entity = cam.Raycast(5000);

                    if (!entity.IsEmpty())
                    {
                        var type = entity.Type;

                        var isGrid = type == MyDetectedEntityType.LargeGrid || type == MyDetectedEntityType.SmallGrid;

                        if (isGrid)
                        {
                            target = entity;
                            Me.CustomData = entity.Position.ToString();

                            // update lcd
                            if (lcd != null)
                            {
                                var dist = (entity.Position - cam.GetPosition()).Length();

                                lcd.WriteText($"{type}\ndist: {dist:0}m");
                            }
                        }
                    }

                    break;
                case "reset":
                    target = null;
                    Me.CustomData = "";
                    lcd?.WriteText("NO TARGET");
                    
                    break;
                case "start":
                    if (target.HasValue)
                    {
                        stopBroadcast = DateTime.UtcNow.AddSeconds(1);
                        messages.Push(target.Value);
                    }

                    break;
            }
        }

        #endregion
    }
}
