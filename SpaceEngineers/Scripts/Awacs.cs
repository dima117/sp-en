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

namespace SpaceEngineers.Scripts.Awacs
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Transmitter.cs
        // import:Lib\TargetInfo.cs
        // import:Lib\Serializer.cs
        // import:TargetTracker.cs

        const int DISTANCE = 15000;

        bool onlyEnemies = false;

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;
        IMyTextSurface lcdTarget;
        IMyTextSurface lcdSystem;

        readonly Transmitter tsm;
        readonly TargetTracker tt;
        readonly IMyCameraBlock cam;

        readonly IMySoundBlock sound;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            // антенна
            tsm = new Transmitter(this);
            tsm.Subscribe(MsgTags.LOCK_TARGET, RemoteLock, true);

            // массив камер радара
            tt = new TargetTracker(this);

            // главная камера
            cam = GridTerminalSystem.GetBlockWithName("CAMERA") as IMyCameraBlock;
            cam.EnableRaycast = true;

            // lcd
            var list2 = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(list2);

            var control = list2.FirstOrDefault(x => x.CubeGrid.EntityId == Me.CubeGrid.EntityId && x.IsMainCockpit);
            lcdTarget = control?.GetSurface(1);
            lcdSystem = control?.GetSurface(2);

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
        private void RemoteLock(MyIGCMessage message)
        {
            try
            {
                var data = message.Data.ToString();
                var reader = new Serializer.StringReader(data);

                TargetInfo target;
                if (Serializer.TryParseTargetInfo(reader, out target))
                {
                    tt.LockOn(target);

                    if (tt.Current.HasValue)
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

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            tsm.Update(argument, updateSource);

            switch (argument)
            {
                case "filter":
                    onlyEnemies = !onlyEnemies;

                    break;
                case "lock":
                    var target = TargetTracker.Scan(cam, DISTANCE, onlyEnemies);

                    if (target.HasValue)
                    {
                        tt.LockOn(target.Value);

                        sound?.Play();
                    }

                    break;

                case "reset":
                    tt.Clear();

                    break;
                default:
                    tt.Update();

                    if (tt.Current.HasValue)
                    {
                        var msg = new StringBuilder();

                        Serializer.SerializeTargetInfo(tt.Current.Value, msg);
                        tsm.Send(MsgTags.UPDATE_TARGET_POS, msg.ToString());
                    }
                    else
                    {
                        tsm.Send(MsgTags.CLEAR_TARGET_POS);

                        if (sound?.IsWorking == true)
                        {
                            sound?.Stop();
                        }
                    }

                    break;
            }

            UpdateTargetLcd();
            UpdateSystemLcd();

            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        void UpdateTargetLcd()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Locked: {tt.Current.HasValue}");

            if (tt.Current.HasValue)
            {
                var target = tt.Current.Value.Entity;
                var distance = Vector3D.Distance(cam.GetPosition(), target.Position);

                sb.AppendLine($"{target.Type}\nV={target.Velocity.Length():0.0}\nS={distance:0.0}");
            }

            lcdTarget?.WriteText(sb.ToString());
        }

        void UpdateSystemLcd()
        {
            var filter = onlyEnemies ? "Enemies" : "All";

            var sb = new StringBuilder();
            sb.AppendLine($"Range: {cam.AvailableScanRange:0.0}");
            sb.AppendLine($"Total range: {tt.TotalRange:0.0}");
            sb.AppendLine($"Cam count: {tt.Count}");
            sb.AppendLine($"Filter: {filter}");

            lcdSystem.WriteText(sb.ToString());
        }

        #endregion
    }
}
