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

namespace SpaceEngineers.Scripts.NukeTest
{
    public class Program : MyGridProgram
    {
        #region Copy

        // скрипт для отладки учета угловой скорости
        // нужен сотиентированный гироскоп и ротор с тем же направлением up

        // отметка 180 == вперед
        // отметка 90 == влево


        private IMyCargoContainer container;
        private IMyWarhead warhead;

        public Program()
        {
            container = GridTerminalSystem.GetBlockWithName("STORE") as IMyCargoContainer;
            warhead = GridTerminalSystem.GetBlockWithName("WARHEAD") as IMyWarhead;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (container.Closed) {
                warhead.IsArmed = true;
                warhead.Detonate();
                Me.GetSurface(0).WriteText("detonate");
            }           
        }

        #endregion
    }
}


/*

 */