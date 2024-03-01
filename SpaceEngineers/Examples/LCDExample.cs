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
using VRageRender;

namespace xxx
{
    public sealed class Program : MyGridProgram
    {
        /*
         
Debug
Red
Green
Blue
White
DarkBlue
UrlNormal
UrlHighlight
ErrorMessageBoxCaption
ErrorMessageBoxText
InfoMessageBoxCaption
InfoMessageBoxText
ScreenCaption
GameCredits
LoadingScreen
BuildInfo
BuildInfoHighlight
Monospace
         */

        readonly IMyTextPanel lcd;
        readonly RectangleF viewport;

        public Program()
        {
            lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
            lcd.ContentType = ContentType.SCRIPT;
            lcd.Script = "";


            viewport = new RectangleF((lcd.TextureSize - lcd.SurfaceSize) / 2f, lcd.SurfaceSize);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var fonts = new List<string>();
            lcd.GetFonts(fonts);

            lcd.CustomData = string.Join(Environment.NewLine, fonts);

            var frame = lcd.DrawFrame();

            DrawSprites(ref frame);

            frame.Dispose();
        }

        public void DrawSprites(ref MySpriteDrawFrame frame)
        {
            // Set up the initial position - and remember to add our viewport offset
            var position = new Vector2(viewport.Width, 20) + viewport.Position;

            // Create our first line
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "target",
                Position = position,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Orange,
                Alignment = TextAlignment.RIGHT /* Center the text on the position */,
                FontId = "Debug"
            };
            // Add the sprite to the frame
            frame.Add(sprite);

            // Move our position 20 pixels down in the viewport for the next line
            position += new Vector2(0, 20);

            // Create our second line, we'll just reuse our previous sprite variable - this is not necessary, just
            // a simplification in this case.
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "Конь-73",
                Position = position,
                RotationOrScale = 1f,
                Color = Color.White,
                Alignment = TextAlignment.RIGHT,
                FontId = "Debug"
            };
            // Add the sprite to the frame
            frame.Add(sprite);
        }
    }
}
