using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        #region Copy

        readonly TargetTracker tt;
        readonly IMyRemoteControl remote;
        readonly IMyShipMergeBlock slot;
        readonly IMySoundBlock sound;


        public Program()
        {
            tt = new TargetTracker(this);
        }
        public void Main(string argument, UpdateType updateSource)
        {
            var vel = remote.GetShipVelocities().LinearVelocity;
            sound.SelectedSound = "";
            sound.Play();

        }
        public void Save()
        {

        }
        #endregion
    }
}
