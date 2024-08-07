﻿using System;
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
using SpaceEngineers.Lib;
using SpaceEngineers.Scripts.Torpedos;

namespace SpaceEngineers.Lib
{
    // торпеда может работать с ионными и водородными двигателями

    #region Copy

    // import:DirectionController2.cs
    // import:TargetInfo.cs

    public class Torpedo
    {
        readonly int delay;
        readonly int lifespan;
        readonly string name;

        readonly DirectionController2 tControl;

        readonly List<IMyGyro> listGyro = new List<IMyGyro>();
        readonly List<IMyThrust> listEngine = new List<IMyThrust>();
        readonly List<IMyGasGenerator> listH2Gen = new List<IMyGasGenerator>();
        readonly List<IMyWarhead> listWarhead = new List<IMyWarhead>();
        readonly IMyRemoteControl tRemote;
        readonly IMyShipMergeBlock tClamp;

        DateTime startTime = DateTime.MaxValue;
        DateTime deathTime = DateTime.MaxValue;

        public string Name => name;
        public Vector3D Position => tRemote.GetPosition();
        public double GetSpeed(DateTime now) => Started && IsAlive(now) ? tRemote.GetShipSpeed() : 0;
        public bool IsReady => listEngine.Any() && listGyro.Any() && tRemote != null && tClamp != null;
        public bool Started { get; private set; }

        public LaunchStage GetStage(DateTime now) =>
            !IsAlive(now) ? LaunchStage.Dead :
            Started ? LaunchStage.Started :
            IsReady ? LaunchStage.Ready : LaunchStage.Invalid;

        public long EntityId => (tRemote?.EntityId).GetValueOrDefault();

        public bool IsAlive(DateTime now) =>
            tRemote.IsFunctional &&
            listEngine.All(e => e.IsFunctional && e.CubeGrid.EntityId == tRemote.CubeGrid.EntityId) &&
            listGyro.All(g => g.IsFunctional && g.CubeGrid.EntityId == tRemote.CubeGrid.EntityId) &&
            now < deathTime;

        public Torpedo(
            IMyBlockGroup group,
            int delay = 2000,  // задержка при старте
            float factor = 7,   // коэффициент мощности гироскопа
            int lifespan = 360) // длительность жизни в секундах
        {
            name = group.Name;
            this.delay = delay;
            this.lifespan = lifespan;

            group.GetBlocksOfType(listGyro);
            group.GetBlocksOfType(listEngine);
            group.GetBlocksOfType(listH2Gen);
            group.GetBlocksOfType(listWarhead);

            var tmp = new List<IMyTerminalBlock>();
            group.GetBlocks(tmp);

            tClamp = tmp.FirstOrDefault(b => b is IMyShipMergeBlock) as IMyShipMergeBlock;
            tRemote = tmp.FirstOrDefault(b => b is IMyRemoteControl) as IMyRemoteControl;
            tControl = new DirectionController2(tRemote, listGyro, factor);
        }

        public void Start(DateTime now)
        {
            startTime = now;
            deathTime = startTime.AddSeconds(lifespan);

            tClamp.Enabled = false;

            listGyro.ForEach(g =>
            {
                g.Enabled = true;
                g.GyroOverride = true;
            });

            listH2Gen.ForEach(g => { g.Enabled = true; });

            listEngine.ForEach(e =>
            {
                e.Enabled = true;
                e.ThrustOverridePercentage = 1;
            });

            Started = true;
        }

        public TorpedoStatus Update(DateTime now, TargetInfo target)
        {
            double distance = 0;

            if ((now - startTime).TotalMilliseconds > delay)
            {
                if (target != null)
                {
                    var targetPos = target.GetHitPosWorld();

                    // tControl.ICBM(target);
                    tControl.Intercept(targetPos, target.Entity.Velocity);
                    // tControl.Aim(target.Position);

                    distance = (targetPos - Position).Length();

                    if (distance < 30)
                    {
                        listWarhead.ForEach(h => h.IsArmed = true);
                    }
                }
            }

            return new TorpedoStatus
            {
                Name = name,
                Stage = GetStage(now),
                Distance = distance,
            };
        }
    }

    #endregion
}
