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
        public const string LOCK_TARGET = "@LOCK_TARGET";

        public const string UPDATE_TARGET_POS = "@UPDATE_TARGET_POS";

        public const string CLEAR_TARGET_POS = "@CLEAR_TARGET_POS";
    }

    public class Transmitter
    {
        private int seq = 0;

        protected IMyIntergridCommunicationSystem igc;

        protected List<IMyRadioAntenna> blocks =
            new List<IMyRadioAntenna>();
        private Dictionary<string, Action<MyIGCMessage>> actions =
            new Dictionary<string, Action<MyIGCMessage>>();
        private Dictionary<string, IMyBroadcastListener> listeners =
            new Dictionary<string, IMyBroadcastListener>();

        public Transmitter(MyGridProgram program)
        {
            igc = program.IGC;
            igc.UnicastListener.SetMessageCallback();

            program.GridTerminalSystem.GetBlocksOfType(blocks);
            blocks.ForEach(a =>
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
