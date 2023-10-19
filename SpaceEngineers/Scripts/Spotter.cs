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

namespace SpaceEngineers.Scripts.Spotter
{
    public sealed class Program : MyGridProgram
    {
        IMyCameraBlock cam;
        IMyCockpit cockpit;

        public Program()
        {
            // главная камера
            cam = GridTerminalSystem.GetBlockWithName("MAIN_CAM") as IMyCameraBlock;
            cam.EnableRaycast = true;

            cockpit = GridTerminalSystem.GetBlockWithName("MAIN_COCKPIT") as IMyCockpit;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "shot":
                    var target = cam.Raycast(5000);

                    if (!target.IsEmpty())
                    {
                        var type = target.Type;

                        var isGrid = type == MyDetectedEntityType.LargeGrid || type == MyDetectedEntityType.SmallGrid;

                        if (isGrid)
                        {
                            var text = target.Position.ToString();
                            var distance = (target.Position - cam.GetPosition()).Length();

                            cockpit.GetSurface(1).WriteText($"{type}\ndist: {distance:0}m\n" + text.Replace(" ", "\n"));
                            Me.CustomData = text;
                        }
                    }

                    break;
                case "reset":
                    cockpit.GetSurface(1).WriteText("");
                    Me.CustomData = "";

                    break;
            }
        }
    }
}
