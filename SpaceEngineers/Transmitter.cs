﻿using VRageMath;
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

    public class Transmitter
    {
        public const string TAG_ICBM_STATE = "TAG_ICBM_STATE";
        public const string TAG_TARGET_POSITION = "TAG_TARGET_POSITION";
        public const string TAG_ICBM_CONNECT = "TAG_ICBM_CONNECT";

        public const int MIN_RANGE = 10;
        public const int MAX_RANGE = 50000;

        private BlockArray<IMyRadioAntenna> blocks;
        private IMyIntergridCommunicationSystem igc;
        private Dictionary<string, Action<MyIGCMessage>> actions =
            new Dictionary<string, Action<MyIGCMessage>>();
        private Dictionary<string, IMyBroadcastListener> listeners =
            new Dictionary<string, IMyBroadcastListener>();

        public Transmitter(MyGridProgram program)
        {
            igc = program.IGC;
            blocks = new BlockArray<IMyRadioAntenna>(program, a =>
            {
                a.Radius = MIN_RANGE;
                a.EnableBroadcasting = true;
                a.Enabled = true;
            });
        }

        public void Subscribe(string tag, Action<MyIGCMessage> fn, bool broadcast = false)
        {
            if (broadcast) {
                var listener = igc.RegisterBroadcastListener(tag);
                var listenerId = Guid.NewGuid().ToString();

                listener.SetMessageCallback(listenerId);
                listeners.Add(listenerId, listener);
            }

            actions.Add(tag, fn);
        }

        public void Send(string tag, object data, long? destination = null)
        {
            blocks.ForEach(a => a.Radius = MAX_RANGE);

            if (destination.HasValue)
            {
                igc.SendUnicastMessage(destination.Value, tag, data);
            }
            else
            {
                igc.SendBroadcastMessage(tag, data);
            }


            blocks.ForEach(a => a.Radius = MIN_RANGE);
        }

        public void Update(string listenerId, UpdateType updateSource)
        {
            if (updateSource == UpdateType.IGC) {
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
