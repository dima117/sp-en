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
using static Sandbox.Game.Weapons.MyDrillBase;

namespace SpaceEngineers.Scripts.Fighter
{
    public class Program : MyGridProgram
    {
        #region Copy

        // import:RuntimeTracker.cs
        // import:Lib\Grid.cs
        // import:Lib\TargetTracker2.cs
        // import:Lib\Transmitter2.cs
        // import:Lib\Serializer.cs
        // import:Lib\TargetInfo.cs

        readonly RuntimeTracker tracker;
        readonly IMyTextSurface lcd;

        readonly Grid grid;
        readonly TargetTracker2 tt;
        readonly Transmitter2 tsm;

        readonly IMyCameraBlock cam;
        readonly IMyCockpit cockpit;
        readonly IMySoundBlock sound;
        readonly IMyTextPanel lcdTargets;

        public Program()
        {
            tracker = new RuntimeTracker(this);
            lcd = Me.GetSurface(1);
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            grid = new Grid(GridTerminalSystem);

            // камера
            cam = grid.GetByFilterOrAny<IMyCameraBlock>();
            if (cam != null)
            {
                cam.Enabled = true;
                cam.EnableRaycast = true;
            }

            // кокпит
            cockpit = grid.GetByFilterOrAny<IMyCockpit>();
            lcdTargets = grid.GetByFilterOrAny<IMyTextPanel>();

            // динамик
            sound = grid.GetByFilterOrAny<IMySoundBlock>();
            if (sound != null)
            {
                sound.Enabled = true;
                sound.SelectedSound = "ArcSoundBlockAlert2";
                sound.Volume = 1;
                sound.Range = 100;
            }

            // трекер целей
            var turrets = grid.GetBlocksOfType<IMyLargeTurretBase>();
            tt = new TargetTracker2(turrets: turrets);
            tt.TargetListChanged += TargetListChanged;

            // антенна
            var antennas = grid.GetBlocksOfType<IMyRadioAntenna>();
            tsm = new Transmitter2(IGC, antennas);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private void TargetListChanged()
        {
            sound?.Play();

            var msg = new StringBuilder();

            Serializer.SerializeTargetInfoArray(tt.GetTargets(), msg);

            tsm.Send(MsgTags.SYNC_TARGETS, msg.ToString());
        }

        private void UpdateTargetsLcd()
        {
            var pos = cockpit.GetPosition();
            var info = tt.GetDisplayState(pos);
            var text = string.IsNullOrEmpty(info) ? "NO TARGET" : info;

            lcdTargets?.WriteText(text);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tracker.AddRuntime();

            tsm.Update(argument, updateSource);
            tt.Update();

            switch (argument)
            {
                case "scan":
                    var target = TargetTracker2.Scan(cam);

                    if (target != null)
                    {
                        tt.LockTarget(target.Value);
                    }

                    break;
            }

            UpdateTargetsLcd();


            tracker.AddInstructions();
            lcd.WriteText(tracker.ToString());
        }

        #endregion
    }
}
