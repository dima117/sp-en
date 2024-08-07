﻿using System;
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
using SpaceEngineers.Lib;

namespace SpaceEngineers.Examples
{
    public sealed class Program : MyGridProgram
    {
        GravityDrive drive;

        public Program()
        {
            var control = GridTerminalSystem.GetBlockWithName("CONTROL") as IMyShipController;
            var group = GridTerminalSystem.GetBlockGroupWithName("GDRIVE");

            drive = new GravityDrive(control, group);


            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            drive.Update();
        }
    }
}
