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
using VRage.Input;
using VRage.Noise.Modifiers;

namespace SpaceEngineers.Scripts.Torpedos
{
    #region Copy

    // import:../DirectionController2.cs
    // import:../TargetInfo.cs

    public enum LaunchStage
    {
        Ready,
        Started,
        Dead,
        Invalid
    }

    public struct TorpedoStatus
    {
        public LaunchStage Stage;
        public string Name;
        public double Distance;

        public override string ToString()
        {
            return Stage == LaunchStage.Started
                ? $"{Name}: {Stage} => {Distance:0}m"
                : $"{Name}: {Stage}";
        }
    }

    public abstract class BaseTorpedo
    {
        protected readonly int delay;
        protected readonly int lifespan;
        protected readonly string name;

        protected readonly DirectionController2 tControl;
        protected readonly IMyRemoteControl tRemote;
        protected readonly IMyShipMergeBlock tClamp;

        protected readonly List<IMyGyro> listGyro = new List<IMyGyro>();
        protected readonly List<IMyThrust> listEngine = new List<IMyThrust>();
        protected readonly List<IMyGasGenerator> listH2Gen = new List<IMyGasGenerator>();

        protected Vector3D destination;
        protected DateTime startTime = DateTime.MaxValue;
        protected DateTime deathTime = DateTime.MaxValue;
        protected bool started = false;

        public long EntityId => (tRemote?.EntityId).GetValueOrDefault();
        public string Name => name;
        public Vector3D Position => tRemote.GetPosition();
        public double GetSpeed(DateTime now) => started && IsAlive(now) ? tRemote.GetShipSpeed() : 0;
        public bool IsInvalid => !listEngine.Any() || !listGyro.Any() || tRemote == null || tClamp == null;
        public LaunchStage GetStage(DateTime now) =>
            !IsAlive(now) ? LaunchStage.Dead :
            started ? LaunchStage.Started :
            IsInvalid ? LaunchStage.Invalid : LaunchStage.Ready;
        public bool IsAlive(DateTime now) =>
            tRemote.IsFunctional &&
            listEngine.All(e => e.IsFunctional && e.CubeGrid.EntityId == tRemote.CubeGrid.EntityId) &&
            listGyro.All(g => g.IsFunctional && g.CubeGrid.EntityId == tRemote.CubeGrid.EntityId) &&
            now < deathTime;

        protected BaseTorpedo(
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

            var tmp = new List<IMyTerminalBlock>();
            group.GetBlocks(tmp);

            tClamp = tmp.FirstOrDefault(b => b is IMyShipMergeBlock) as IMyShipMergeBlock;
            tRemote = tmp.FirstOrDefault(b => b is IMyRemoteControl) as IMyRemoteControl;
            tControl = new DirectionController2(tRemote, listGyro, factor);
        }

        public virtual void Start(DateTime now, Vector3D destination)
        {
            this.destination = destination;

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

            started = true;
        }

        public virtual TorpedoStatus Update(DateTime now, TargetInfo target)
        {
            double distance = 0;

            if ((now - startTime).TotalMilliseconds > delay)
            {
                // включаем управление торпедой через пару секунд,
                // чтобы она успела отлететь от корабля

                if (target != null)
                {
                    distance = (target.Entity.Position - Position).Length();
                    destination = SetDirection(target, distance);
                }
                else
                {
                    SetDirection(destination);
                }
            }

            return new TorpedoStatus
            {
                Name = name,
                Stage = GetStage(now),
                Distance = distance,
            };
        }

        protected abstract Vector3D SetDirection(TargetInfo targetInfo, double distance);
        protected abstract void SetDirection(Vector3D targetPos);
    }

    #endregion
}
