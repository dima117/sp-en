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

namespace SpaceEngineers
{
    #region Copy

    // import:BlockArray.cs

    public class Transmitter
    {
        // 1. предоставляет лаконичный API для отправки и получения сообщений
        // 2. включает антенну на короткое время при отправке сообщений
        public const string TAG_ICBM_STATE = "TAG_ICBM_STATE";
        public const string TAG_TARGET_POSITION = "TAG_TARGET_POSITION";
        public const string TAG_ICBM_CONNECT = "TAG_ICBM_CONNECT";

        const int TIMEOUT_SWITCH_ON = 200;
        const int TIMEOUT_SWITCH_OFF = 200;

        public const int MIN_RANGE = 10;
        public const int MAX_RANGE = 50000;

        private DateTime? timestampSwitchOn = null;
        private DateTime? timestampSwitchOff = null;

        private int seq = 0;

        private BlockArray<IMyRadioAntenna> blocks;
        private IMyIntergridCommunicationSystem igc;
        private Dictionary<string, Action<MyIGCMessage>> actions =
            new Dictionary<string, Action<MyIGCMessage>>();
        private Dictionary<string, IMyBroadcastListener> listeners =
            new Dictionary<string, IMyBroadcastListener>();

        private readonly Queue<Action> messageQueue = new Queue<Action>();

        public Transmitter(MyGridProgram program)
        {
            igc = program.IGC;
            blocks = new BlockArray<IMyRadioAntenna>(program, a =>
            {
                a.Radius = MIN_RANGE;
                a.EnableBroadcasting = true;
                a.Enabled = true;
            });

            igc.UnicastListener.SetMessageCallback();
        }

        public void Subscribe(string tag, Action<MyIGCMessage> fn, bool broadcast = false)
        {
            if (broadcast)
            {
                var listener = igc.RegisterBroadcastListener(tag);
                var listenerId = (DateTime.UtcNow.Ticks - (seq++)).ToString();

                listener.SetMessageCallback(listenerId);
                listeners[listenerId] = listener;
            }

            actions[tag] = fn;
        }

        public void Send<T>(string tag, T data, long? destination = null)
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
                } else
                {
                    // если всё было выключено, то включаем
                    blocks.ForEach(a => a.Radius = MAX_RANGE);
                    timestampSwitchOn = DateTime.UtcNow.AddMilliseconds(TIMEOUT_SWITCH_ON);
                }                
            }
        }

        public void Update(string listenerId, UpdateType updateSource)
        {
            switch (updateSource)
            {
                case UpdateType.IGC:
                    IMyMessageProvider listener = igc.UnicastListener;

                    if (listeners.ContainsKey(listenerId))
                    {
                        listener = listeners[listenerId];
                    }

                    while (listener.HasPendingMessage)
                    {
                        var message = listener.AcceptMessage();
                        if (actions.ContainsKey(message.Tag))
                        {
                            actions[message.Tag](message);
                        }
                    }
                    break;
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
                        blocks.ForEach(a => a.Radius = MIN_RANGE);
                    }

                    break;
            }
        }
    }

    #endregion
}
