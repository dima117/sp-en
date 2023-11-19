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

namespace SpaceEngineers.Scripts.Draft
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();


            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
