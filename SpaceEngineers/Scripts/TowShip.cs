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

namespace SpaceEngineers.Scripts.TowShip
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs
        // import:Transmitter.cs
        // import:Torpedo.cs
        // import:TargetTracker.cs
        // import:Serializer.cs

        const int LIFESPAN = 540;

        bool onlyEnemies = false;

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;

        readonly Transmitter tsm;
        readonly TargetTracker tt;
        readonly IMyCameraBlock cam;
        readonly List<Torpedo> torpedos = new List<Torpedo>();

        readonly IMySoundBlock sound;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            // антенна
            tsm = new Transmitter(this);
            tsm.Subscribe(Transmitter.TAG_TARGET_POSITION, RemoteLock, true);

            // массив камер радара
            tt = new TargetTracker(this);

            // главная камера
            cam = GridTerminalSystem.GetBlockWithName("CAMERA") as IMyCameraBlock;
            cam.EnableRaycast = true;

            // динамик
            sound = GridTerminalSystem.GetBlockWithName("SOUND") as IMySoundBlock;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        private void RemoteLock(MyIGCMessage message)
        {
            var strData = message.Data.ToString().Split('\n');
            
            MyDetectedEntityInfo entity;
            if (Serializer.TryParseMyDetectedEntityInfo(strData, out entity))
            {
                var target = new TargetTracker.TargetInfo(entity, )

                tt.LockOn(target);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            switch (argument)
            {
                case "filter":
                    onlyEnemies = !onlyEnemies;

                    break;
                case "lock":
                    var entity = TargetTracker.Scan(cam, 15000, onlyEnemies);

                    if (entity.HasValue)
                    {
                        tt.LockOn(entity.Value);

                        sound?.Play();
                    }

                    break;

                case "reset":
                    tt.Clear();

                    break;
                case "init":
                    var ids = new HashSet<long>(torpedos.Select(t => t.EntityId));

                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("TORPEDO"));

                    torpedos.AddRange(groups
                        .Select(gr => new Torpedo(gr, lifespan: LIFESPAN))
                        .Where(t => !ids.Contains(t.EntityId)));

                    torpedos.RemoveAll(t => !t.IsAlive);

                    break;
                case "start":
                    // запускаем одну из торпед
                    torpedos.FirstOrDefault(t => !t.Started)?.Start();

                    break;
                default:
                    tt.Update();

                    if (sound?.IsWorking == true && !tt.Current.HasValue)
                    {
                        sound?.Stop();
                    }

                    // обновляем параметры цели на всех торпедах
                    torpedos?.ForEach(t => t.Update(tt.Current));

                    break;
            }


            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
