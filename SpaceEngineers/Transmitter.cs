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
        public const string TAG_ICBM_STATE = "TAG_ICBM_STATE";
        public const string TAG_TARGET_POSITION = "TAG_TARGET_POSITION";
        public const string TAG_ICBM_CONNECT = "TAG_ICBM_CONNECT";

        public const int MIN_RANGE = 10;
        public const int MAX_RANGE = 50000;
        private DateTime limit = DateTime.MinValue;

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

            blocks.ForEach(a => a.Radius = MAX_RANGE);
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

                    if (messageQueue.Any()) {
                        messageQueue.Dequeue()();
                        limit = now.AddMilliseconds(500);
                    }

                    if (now > limit) {
                        blocks.ForEach(a => a.Radius = MIN_RANGE);
                    }

                    break;
            }
        }
    }

    #endregion
}
