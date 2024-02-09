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

    // import:Transmitter.cs

    public class Transmitter2 : Transmitter
    {
        // отправляет сообщения с коротким включением антенны

        const int TIMEOUT_SWITCH_ON = 200;
        const int TIMEOUT_SWITCH_OFF = 200;

        public const int MIN_RANGE = 10;
        public const int MAX_RANGE = 50000;

        private DateTime? timestampSwitchOn = null;
        private DateTime? timestampSwitchOff = null;

        private readonly Queue<Action> messageQueue = new Queue<Action>();

        public Transmitter2(IMyIntergridCommunicationSystem igc, IMyRadioAntenna[] antennas) : base(igc, antennas)
        {
            foreach (var a in antennas)
            {
                a.Radius = MIN_RANGE;
            }
        }

        public override void Send(string tag, string data = "", long? destination = null)
        {
            if (destination.HasValue)
            {
                messageQueue.Enqueue(() => igc.SendUnicastMessage(destination.Value, tag, data));
            }
            else
            {
                messageQueue.Enqueue(() => igc.SendBroadcastMessage(tag, data));
            }

            // если сейчас идет включение, то ничего не делаем
            if (!timestampSwitchOn.HasValue)
            {
                // если сейчас идет выключение, то обнуляем период выключения
                if (timestampSwitchOff.HasValue)
                {
                    timestampSwitchOff = DateTime.UtcNow.AddMilliseconds(TIMEOUT_SWITCH_OFF);
                }
                else
                {
                    // если всё было выключено, то включаем
                    foreach (var a in antennas)
                    {
                        a.Radius = MAX_RANGE;
                    }

                    timestampSwitchOn = DateTime.UtcNow.AddMilliseconds(TIMEOUT_SWITCH_ON);
                }
            }
        }

        public override void Update(string listenerId, UpdateType updateSource)
        {
            base.Update(listenerId, updateSource);

            switch (updateSource)
            {
                case UpdateType.Update1:
                case UpdateType.Update10:
                case UpdateType.Update100:
                    var now = DateTime.UtcNow;

                    if (timestampSwitchOn.HasValue && now > timestampSwitchOn)
                    {
                        // switch on
                        timestampSwitchOn = null;
                        timestampSwitchOff = now.AddMilliseconds(TIMEOUT_SWITCH_OFF);

                        while (messageQueue.Any())
                        {
                            messageQueue.Dequeue()();
                        }
                    }
                    else if (timestampSwitchOff.HasValue && now > timestampSwitchOff)
                    {
                        timestampSwitchOff = null;
                        foreach (var a in antennas)
                        {
                            a.Radius = MIN_RANGE;
                        }
                    }

                    break;
            }
        }
    }

    #endregion
}
