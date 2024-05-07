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
using System.Net;

namespace SpaceEngineers.Scripts.Debug
{
    public class Program : MyGridProgram
    {
        #region Copy

        public Program()
        {
            //Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var art = GridTerminalSystem.GetBlockWithName("xxx");

            Echo(art.GetType().Name);
            Echo(art.BlockDefinition.SubtypeId);
            Me.CustomData = art.BlockDefinition.SubtypeName;
        }

        #endregion
    }
}


/*

 */