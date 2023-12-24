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
using SpaceEngineers.Lib;

namespace SpaceEngineers.Scripts.TowShip
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Transmitter.cs
        // import:Lib\TargetInfo.cs
        // import:Lib\Serializer.cs
        // import:Lib\Torpedo.cs

        const int LIFESPAN = 540;

        TargetInfo? target = null;

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        IMyTextSurface lcdTarget;
        IMyTextSurface lcdTorpedos;

        readonly Transmitter tsm;
        readonly List<Torpedo> torpedos = new List<Torpedo>();

        readonly IMySoundBlock sound;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            // антенна
            tsm = new Transmitter(this);
            tsm.Subscribe(MsgTags.UPDATE_TARGET_POS, UpdateTarget, true);
            tsm.Subscribe(MsgTags.CLEAR_TARGET_POS, ClearTarget, true);

            // lcd
            var list2 = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(list2);

            var control = list2.FirstOrDefault(x => x.CubeGrid.EntityId == Me.CubeGrid.EntityId);
            lcdTorpedos = control?.GetSurface(0);
            lcdTarget = control?.GetSurface(1);

            // динамик
            sound = GridTerminalSystem.GetBlockWithName("SOUND") as IMySoundBlock;

            if (sound != null)
            {
                sound.SelectedSound = "ArcSoundBlockAlert2";
                sound.LoopPeriod = 30;
                sound.Range = 20;
                sound.Enabled = true;
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        private void UpdateTarget(MyIGCMessage message)
        {
            try
            {
                var data = message.Data.ToString();
                var reader = new Serializer.StringReader(data);

                TargetInfo tmp;

                if (Serializer.TryParseTargetInfo(reader, out tmp))
                {
                    target = tmp;

                    if (sound?.IsWorking != true)
                    {
                        sound?.Play();
                    }
                }

                Me.CustomData = data;
            }
            catch (Exception ex)
            {
                Me.CustomData = ex.Message + "\n" + ex.StackTrace;
            }
        }

        private void ClearTarget(MyIGCMessage message)
        {
            try
            {
                target = null;
                Me.CustomData = null;

                if (sound?.IsWorking == true)
                {
                    sound?.Stop();
                }
            }
            catch (Exception ex)
            {
                Me.CustomData = ex.Message + "\n" + ex.StackTrace;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "init":
                    var ids = new HashSet<long>(torpedos.Select(t => t.EntityId));

                    var groups = new List<IMyBlockGroup>();
                    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.StartsWith("TORPEDO"));

                    torpedos.AddRange(groups
                        .Select(gr => new Torpedo(gr, factor: 3f, lifespan: LIFESPAN))
                        .Where(t => !ids.Contains(t.EntityId)));

                    torpedos.RemoveAll(t => !t.IsAlive);

                    break;
                case "start":
                    // запускаем одну из торпед
                    torpedos.FirstOrDefault(t => !t.Started)?.Start();

                    break;
                default:

                    // обновляем параметры цели на всех торпедах
                    var state = torpedos?.Select(t => t.Update(target));
                    var text = String.Join("\n", state?.Select(s => s.ToString()));

                    lcdTorpedos.WriteText(text);

                    break;
            }

            UpdateTargetLcd();

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        void UpdateTargetLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {target.HasValue}");

            if (target.HasValue)
            {
                var entity = target.Value.Entity;
                var distance = Vector3D.Distance(Me.GetPosition(), entity.Position);

                sb.AppendLine($"{entity.Type}\nV={entity.Velocity.Length():0.0}\nS={distance:0.0}");
            }

            lcdTarget?.WriteText(sb.ToString());
        }

        #endregion
    }
}
