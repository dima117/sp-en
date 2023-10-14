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
using static Sandbox.Game.Weapons.MyDrillBase;

namespace SpaceEngineers.Examples.IcbmExample
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:Icbm.cs

        Vector3D target;
        IMyTextPanel lcd1;
        IMyTextPanel lcd2;
        Icbm missile;

        public Program()
        {
            lcd1 = GridTerminalSystem.GetBlockWithName("LCD1") as IMyTextPanel; // система
            lcd2 = GridTerminalSystem.GetBlockWithName("LCD2") as IMyTextPanel; // статус ракеты

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "init":
                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("ICBM"));

                    missile = new Icbm(groups.First());
                    break;
                case "start":
                    if (missile != null && !target.IsZero())
                    {
                        missile.Start(target);
                    }

                    break;
                default:

                    if (missile?.Started == true)
                    {
                        missile.Update();
                        lcd2.WriteText(missile.ToString());
                    }
                    else {
                        var text = Me.CustomData;
                        Vector3D pos;
                        if (!string.IsNullOrEmpty(text) && Vector3D.TryParse(text, out pos)) { 
                            target = pos;
                        }

                        lcd1.WriteText(target.ToString());
                    }

                    break;
            }
        }

        #endregion
    }
}
