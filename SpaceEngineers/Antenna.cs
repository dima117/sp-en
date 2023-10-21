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
    public class Antenna
    {
        public struct Message
        {
            public object data;
            public long? address;
            public string tag;
        }

        public const string TAG_TARGET_POSITION = "TAG_TARGET_POSITION";
        public const string TAG_MISSILE_LAUNCHED = "TAG_MISSILE_LAUNCHED";

        public const int MIN_RANGE = 10;
        public const int MAX_RANGE = 50000;

        private BlockArray<IMyRadioAntenna> blocks;
        private DateTime limit = DateTime.MinValue;
        private Queue<Message> messages = new Queue<Message>();

        private IMyIntergridCommunicationSystem igc;

        public Antenna(MyGridProgram program)
        {
            igc = program.IGC;
            blocks = new BlockArray<IMyRadioAntenna>(program, a => a.Radius = MIN_RANGE);
        }

        public void Update()
        {
            if (messages.Any())
            {
                limit = DateTime.UtcNow.AddSeconds(1);
                blocks.ForEach(a => a.Radius = MAX_RANGE);

                var msg = messages.Dequeue();

                if (msg.address.HasValue)
                {
                    igc.SendUnicastMessage(msg.address.Value, msg.tag, msg.data);
                }
                else
                {
                    igc.SendBroadcastMessage(msg.tag, msg.data);
                }
            }
            else
            {
                if (DateTime.UtcNow > limit)
                {
                    blocks.ForEach(a => a.Radius = MIN_RANGE);
                }
            }
        }
    }
}
