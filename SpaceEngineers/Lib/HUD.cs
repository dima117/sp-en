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
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities.Cube;
using SpaceEngineers.Scripts.Torpedos;
using System.Diagnostics;

namespace SpaceEngineers.Lib
{
    #region Copy

    public enum FiringMode
    {
        Auto,
        Forward
    }

    public enum ForwardWeapon
    {
        Artillery,
        Railgun,
    }

    public class WeaponState
    {
        public int TorpedosCount { get; set; }
        public int RalgunsCount { get; set; }
        public float RalgunsСharge { get; set; }
        public int RalgunsReadyCount { get; set; }
        public int TurretsCount { get; set; }
        public FiringMode TurretsFiringMode { get; set; }
    }

    public class HudState
    {
        public Vector3D? AITarget { get; set; }
        public TargetInfo Target { get; set; }
        public ForwardWeapon? Aimbot { get; set; }
        public WeaponState Weapon { get; set; }
        public bool EnemyLock { get; set; }
    }

    public class HUD
    {
        const int BEACON_RADIUS = 70;

        private static readonly HashSet<MyRelationsBetweenPlayerAndBlock> friends =
            new HashSet<MyRelationsBetweenPlayerAndBlock> {
                MyRelationsBetweenPlayerAndBlock.Owner,
                MyRelationsBetweenPlayerAndBlock.FactionShare,
                MyRelationsBetweenPlayerAndBlock.Friends,
                MyRelationsBetweenPlayerAndBlock.Neutral,
            };

        private readonly IMyTextPanel[] displays;
        private readonly IMyBeacon beacon;
        private readonly Func<DateTime, HudState> getState;
        private DateTime lastUpdateHUD;

        public HUD(IMyTextPanel[] displays, IMyBeacon beacon, Func<DateTime, HudState> getState)
        {
            this.getState = getState;

            this.beacon = beacon;
            this.beacon.Enabled = true;
            this.beacon.Radius = BEACON_RADIUS;

            this.displays = displays;

            foreach (var d in displays)
            {
                d.Enabled = true;
                d.ContentType = ContentType.SCRIPT;
                d.BackgroundColor = Color.Black;
                d.BackgroundAlpha = 0;
            }
        }

        public void Update(DateTime now, Vector3D selfPos)
        {
            lastUpdateHUD = now;

            var state = getState(now);

            var w = state.Weapon;

            string targetName = null;
            string dist = null;

            if (state.Target != null)
            {
                var t = state.Target.Entity;
                var d = (t.Position - selfPos).Length();

                // todo: сделать мапинг
                var size = t.Type == MyDetectedEntityType.SmallGrid ? "SM" : "LG";
                var name = TargetTracker.GetName(t.EntityId);

                targetName = friends.Contains(t.Relationship)
                    ? $"{size} ∙ {t.Name}"
                    : $"{size} ∙ {name}";

                dist = d.ToString("0m");
            }

            var tm = w.TurretsFiringMode == FiringMode.Forward ? "Fwd" : "Auto";
            var turrets = w.TurretsCount > 0 ? $"{w.TurretsCount} ∙ {tm}" : tm;

            var aimbot = GetAimbotText(state.Aimbot);

            var sprites = GetSprites(
                state.AITarget,
                targetName, dist, aimbot, turrets,
                w.TorpedosCount, state.EnemyLock,
                w.RalgunsCount, w.RalgunsReadyCount, w.RalgunsСharge);

            foreach (var lcd in displays)
            {
                using (var frame = lcd.DrawFrame())
                {
                    frame.AddRange(sprites);

                    if (state.AITarget.HasValue)
                    {
                        lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                        frame.AddRange(GetTargetSprite(lcd, state.AITarget.Value));
                        lcd.ContentType = ContentType.SCRIPT;
                    }
                }
            }

            // beacon
            var ti = targetName == null ? "NO TARGET" : $"{targetName} | {dist}";
            var p = w.RalgunsСharge * 100;
            var rp = p > 0 ? $" ∙ {p:0}%" : "";
            beacon.HudText = $"{ti}\n{aimbot} | {tm} | Rail: {w.RalgunsReadyCount}{rp}";
        }

        private string GetAimbotText(ForwardWeapon? aimbot)
        {
            switch (aimbot)
            {
                case ForwardWeapon.Railgun:
                    return "Rail";
                case ForwardWeapon.Artillery:
                    return "Art";
                default:
                    return "Off";
            }
        }

        const float CAM_DISTANCE = 3.75f;
        const float LCD_HALF_WIDTH = 1.1f; // половина ширины видимой области экрана

        private MySprite[] GetTargetSprite(IMyTextPanel screen, Vector3D targetPos)
        {
            var m = screen.WorldMatrix;

            var screenPos = screen.GetPosition();
            var camPos = screenPos + m.Backward * CAM_DISTANCE;

            var targetVector = targetPos - camPos;
            var rate = CAM_DISTANCE / targetVector.Length();

            var d = targetVector.Dot(m.Forward);
            if (d > 0)
            { // если цель спереди
                var v = targetVector.Dot(m.Down) * rate;
                var h = targetVector.Dot(m.Right) * rate;

                if (Math.Abs(h) < LCD_HALF_WIDTH && Math.Abs(v) < LCD_HALF_WIDTH)
                {
                    // цель за экраном
                    var x = Convert.ToInt32(256 * (1 + h / LCD_HALF_WIDTH));
                    var y = Convert.ToInt32(256 * (1 + v / LCD_HALF_WIDTH));

                    return new MySprite[] {
                        new MySprite
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareHollow",
                            Position = new Vector2(x - 16, y),
                            Size = new Vector2(32, 32),
                            Color = Color.White
                        }
                    };
                }
            }

            return new MySprite[0];
        }

        private MySprite[] GetSprites(
            Vector3D? aiTarget,
            string targetName, string dist, string aimbot, string turrets, int tCount, bool enemyLock,
            int rgTotal, int rgReady, float rgChargeLevel)
        {
            var list = new List<MySprite>();

            // ai atrget
            list.AddRange(Text(null, aiTarget.HasValue ? "AI: Locked" : "AI: Idle", TextAlignment.CENTER, TOP + 1));

            // target

            list.AddRange(Text("target", targetName ?? "NO TARGET", TextAlignment.CENTER, TOP));

            if (dist != null)
            {
                list.AddRange(Text("dist", dist, TextAlignment.LEFT, TOP));
            }


            // torpedo count
            list.AddRange(Text("torpedos", tCount.ToString(), TextAlignment.RIGHT, TOP));


            // aimbot
            list.AddRange(Text("aimbot", aimbot, TextAlignment.CENTER, BOTTOM));
            list.AddRange(Text("turrets", turrets, TextAlignment.LEFT, BOTTOM));


            // enemy lock
            if (enemyLock)
            {
                list.AddRange(Text(null, "ENEMY LOCK", TextAlignment.CENTER, BOTTOM - 1, color: Color.OrangeRed));
            }

            // railguns
            if (rgTotal > 0)
            {
                list.AddRange(Text("railgun", $"{rgReady} / {rgTotal}", TextAlignment.RIGHT, BOTTOM));

                list.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(452, 509),
                    Size = new Vector2(Convert.ToInt32(60 * rgChargeLevel), 3),
                    Color = Color.Teal,
                });
            }

            return list.ToArray();
        }

        const int TOP = 0;
        const int BOTTOM = 9;
        const int LABEL_HEIGHT = 20;
        const int VALUE_HEIGHT = 30;
        const int LINE_HEIGHT = 51; // label + value + space
        const int CELL_WIDTH = 128;

        private MySprite[] Text(
            string label,
            string text,
            TextAlignment alignment,
            byte line = TOP,
            byte offset = 0,
            Color? color = null)
        {
            int x = 0;

            switch (alignment)
            {
                case TextAlignment.LEFT:
                    x = 0 + offset * CELL_WIDTH;
                    break;
                case TextAlignment.RIGHT:
                    x = 512 - offset * CELL_WIDTH;
                    break;
                case TextAlignment.CENTER:
                    x = 256;
                    break;
            }

            int y = line * LINE_HEIGHT;

            var valueSprite = new MySprite() // text
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(x, y + LABEL_HEIGHT),
                RotationOrScale = 1f,
                Color = color ?? Color.White,
                Alignment = alignment,
                FontId = "White"
            };

            if (string.IsNullOrEmpty(label))
            {
                return new[] { valueSprite };
            }

            return new[] {
                new MySprite // label
                {
                    Type = SpriteType.TEXT,
                    Data = label,
                    Position = new Vector2(x, y),
                    RotationOrScale = 0.8f,
                    Color = Color.Teal,
                    Alignment = alignment,
                    FontId = "White"
                },
                valueSprite
            };
        }
    }

    #endregion
}
