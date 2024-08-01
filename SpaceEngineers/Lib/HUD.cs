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
        public Vector3D[] ActiveTorpedos { get; set; }
        public int TorpedosCount { get; set; }
        public int RalgunsCount { get; set; }
        public float RalgunsСharge { get; set; }
        public int RalgunsReadyCount { get; set; }
        public int TurretsCount { get; set; }
        public FiringMode TurretsFiringMode { get; set; }
    }

    public class HudState
    {
        public Vector3D? AiTarget { get; set; }
        public TargetInfo Target { get; set; }
        public ForwardWeapon? Aimbot { get; set; }
        public WeaponState Weapon { get; set; }
        public bool EnemyLock { get; set; }
    }

    public class HUD
    {
        const int BEACON_RADIUS = 70;
        static readonly Vector3D[] empty = new Vector3D[0];

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

        private static string Cut(string text, int length = 21)
        {
            return text.Length > length ? text.Substring(0, length - 1) + "…" : text;
        }

        public void Update(DateTime now, Vector3D selfPos)
        {
            if ((now - lastUpdateHUD).TotalMilliseconds < 100)
            {
                return;
            }

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
                var size = t.Type == MyDetectedEntityType.SmallGrid ? "S" : "L";
                var name = TargetTracker.GetName(t.EntityId);

                targetName = friends.Contains(t.Relationship)
                    ? $"{size} ∙ {Cut(t.Name)}"
                    : $"{size} ∙ {name}";

                dist = d.ToString("0m");
            }

            var tm = w.TurretsFiringMode == FiringMode.Forward ? "Fwd" : "Auto";
            var turrets = w.TurretsCount > 0 ? $"{w.TurretsCount} ∙ {tm}" : tm;

            var tpos = w.ActiveTorpedos ?? empty;
            var tCount = w.TorpedosCount;
            var tCountActive = tpos.Length;
            var torpedos = tCountActive > 0 ? $"{tCountActive} ∙ {tCount}" : tCount.ToString();

            var aimbot = GetAimbotText(state.Aimbot);

            // ai target
            var ai = "empty";
            if (state.AiTarget.HasValue)
            {
                var aiDist = (state.AiTarget.Value - selfPos).Length();
                ai = $"{aiDist:0}m";
            }

            var sprites = GetSprites(
                targetName, dist, aimbot, turrets,
                torpedos, state.EnemyLock,
                w.RalgunsCount, w.RalgunsReadyCount, w.RalgunsСharge);

            foreach (var lcd in displays)
            {
                // lcd.ContentType = ContentType.TEXT_AND_IMAGE;

                using (var frame = lcd.DrawFrame())
                {
                    frame.AddRange(sprites);

                    if (state.AiTarget.HasValue)
                    {
                        frame.AddRange(GetTargetSprite(selfPos, lcd, state.AiTarget.Value));
                    }

                    foreach (var pos in tpos)
                    {
                        frame.AddRange(GetTargetSprite(state.Target?.Entity.Position, lcd, pos, Color.LimeGreen, "CircleHollow", "CircleHollow", "Circle"));
                    }
                }

                // lcd.ContentType = ContentType.SCRIPT;
            }

            // beacon
            var ti = targetName == null ? "NO TARGET" : $"{targetName} ∙ {dist}";
            var p = w.RalgunsСharge * 100;
            var rp = p > 0 ? $" ∙ {p:0}%" : "";
            beacon.HudText = $"{ti} | AI: {ai}\n{aimbot} | {tm} | T: {torpedos} | R: {w.RalgunsReadyCount}{rp}";
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

        private MySprite[] GetTargetSprite(
            Vector3D? position, IMyTextPanel screen, Vector3D targetPos, Color? color = null,
            string shape = "SquareHollow", string shapeOutside = "SquareSimple", string shapeOutsideBack = "Triangle")
        {
            var c = color ?? Color.Orange;
            var m = screen.WorldMatrix;

            var screenPos = screen.GetPosition();
            var camPos = screenPos + m.Backward * CAM_DISTANCE;

            var targetVector = targetPos - camPos;
            var rate = CAM_DISTANCE / targetVector.Length();
            var v = targetVector.Dot(m.Down) * rate;
            var h = targetVector.Dot(m.Right) * rate;

            var d = targetVector.Dot(m.Forward);
            var dist = position.HasValue ? (targetPos - position.Value).Length().ToString("0") : string.Empty;

            var maxPos = Math.Max(Math.Abs(h), Math.Abs(v));

            if (d > 0 && maxPos < LCD_HALF_WIDTH)
            {
                // цель спереди и в пределах экрана
                var x = Convert.ToInt32(256 * (1 + h / LCD_HALF_WIDTH)) - 16;
                var y = Convert.ToInt32(256 * (1 + v / LCD_HALF_WIDTH));

                return new MySprite[] {
                        RelativeText(dist, x, y - 16, 32, 42, c),
                        new MySprite
                        {
                            Type = SpriteType.TEXTURE,
                            Data = shape,
                            Position = new Vector2(x, y),
                            Size = new Vector2(32, 32),
                            Color = c
                        }
                    };
            }
            else
            {
                // цель за пределами экрана или сзади
                var x = Convert.ToInt32(256 * (1 + h / maxPos));
                var y = Convert.ToInt32(256 * (1 + v / maxPos));

                // если цель сзади, то вместо квадрата рисуем треугольник
                var data = d < 0 ? shapeOutsideBack : shapeOutside;

                var tx = x - 6;
                var ty = y - 6;

                tx = (tx < 0) ? 0 : (tx > 500) ? 500 : tx;
                ty = (ty < 6) ? 6 : (ty > 500) ? 500 : ty;

                var outline = new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = data,
                    Position = new Vector2(tx, ty),
                    Size = new Vector2(12, 12),
                    Color = c
                };

                if (string.IsNullOrEmpty(dist))
                {
                    return new MySprite[] { outline };
                }

                var text = RelativeText(dist, tx, ty, 12, 42, c);

                return new MySprite[] { text, outline };
            }
        }



        private MySprite[] GetSprites(
            string targetName, string dist, string aimbot, string turrets, string torpedos, bool enemyLock,
            int rgTotal, int rgReady, float rgChargeLevel)
        {
            var list = new List<MySprite>();

            // target

            list.AddRange(Text("target", targetName ?? "NO TARGET", TextAlignment.CENTER, TOP));

            if (dist != null)
            {
                list.AddRange(Text("dist", dist, TextAlignment.LEFT, TOP));
            }

            // torpedo count
            list.AddRange(Text("torpedos", torpedos, TextAlignment.RIGHT, TOP));

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

        private MySprite RelativeText(string text, int x, int y, int s, int offset, Color? color = null)
        {
            TextAlignment alignment = TextAlignment.LEFT;

            var tx = x;
            var ty = y + s;

            if (tx > 512 - offset)
            {
                alignment = TextAlignment.RIGHT;
                tx = x + s;
            }

            if (ty > 512 - offset)
            {
                ty = y - 32; // переносим текст наверх, 24 - высота строки
            }

            return new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(tx, ty),
                RotationOrScale = 0.8f,
                Color = color ?? Color.White,
                Alignment = alignment,
                FontId = "White"
            };
        }

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
