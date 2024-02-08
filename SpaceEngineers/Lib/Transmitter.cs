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
using System.Collections.Generic;
using System.Linq;
using System;

namespace SpaceEngineers.Lib
{
    #region Copy

    public class MsgTags
    {
        public const string REMOTE_LOCK_TARGET = "@REMOTE_LOCK_TARGET";
        public const string REMOTE_START = "@REMOTE_START";
        public const string GET_STATUS = "@GET_STATUS";
        public const string REMOTE_STATUS = "@REMOTE_STATUS";
        public const string SYNC_TARGETS = "@SYNC_TARGETS";
    }

    public class Transmitter
    {
        private int seq = 0;

        protected IMyIntergridCommunicationSystem igc;

        protected IMyRadioAntenna[] antennas;
        private Dictionary<string, Action<MyIGCMessage>> actions =
            new Dictionary<string, Action<MyIGCMessage>>();
        private Dictionary<string, IMyBroadcastListener> listeners =
            new Dictionary<string, IMyBroadcastListener>();

        public Transmitter(IMyIntergridCommunicationSystem igc, IMyRadioAntenna[] antennas)
        {
            this.igc = igc;
            igc.UnicastListener.SetMessageCallback();

            this.antennas = antennas;
            antennas.ForEach(a =>
            {
                a.EnableBroadcasting = true;
                a.Enabled = true;
            });
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

        public virtual void Send(string tag, string data = "", long? destination = null)
        {
            if (destination.HasValue)
            {
                igc.SendUnicastMessage(destination.Value, tag, data);
            }
            else
            {
                igc.SendBroadcastMessage(tag, data);
            }
        }

        public virtual void Update(string listenerId, UpdateType updateSource)
        {
            if (updateSource == UpdateType.IGC)
            {
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
            }
        }
    }

    #endregion
}
