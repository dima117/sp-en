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

namespace SpaceEngineers.Examples1
{
    internal class Program: MyGridProgram
    {
        /* Ракетная система Р-4 || Missile R-4 system, version 1.72
  * --------------------------------------------------------
  * Сделано в Н.С.К.С || Made in N.U.C.C
  * При уменьшении частоты вызова в 10 раз уменьшите переменные в 10 раз! || If you reduce frequency in 10 times, reduce time variables in 10 times!
  * 
  */
        string RCName = "УДС"; // Name of RC of missile
        string MergeName = "Стыковочный блок ракеты"; // Name of merge of missile
        string MassName = "Масса ракеты"; // Name of artifical mass of missile
        string AcbName = "Аккумулятор ракеты"; // Name of missile batteries
        string WarheadName = "Боеголовка ракеты"; // Name of missile warheads
        string OrientName = "Ориентир для ракет"; // Name of orient name for ATGM mode
        string LCDName = "ИНФО ЦП Ракет"; // Name of LCD
        string ConnName = "Коннектор ракеты"; // Name of connector of missile
        string ThrustName = "Ускоритель ракеты"; // Name of thruster
        string IceCargoName = "Большой контейнер"; // Name of container with ice
        string IceGenName = "Генератор ракеты"; // Name of H2O gen
        string RotorName = "Ротор ракеты"; // Missile rotor name
        string GyroName = "Гироскоп ракеты"; // Name of Gyro
        string GatlingName = "Отстреливатель"; // Name of launch gatling
        string RadarName = "РЛС Изумруд-1"; // Название ПБ РЛС, если целеуказание не идёт напрямую || Name of Radar PB if targeting not given directly
        //
        int StartDirection = 1;
        // Направление пуска двигателей старта || Direction of launch start thrusters 
        // 1 - перед, 2 - верх, 3 - низ, 4 - лево, 5 - право, 6 - назад
        // 1 - forward, 2 - upward, 3 - downward, 4 - left, 5 - right, 6 - backward
        //
        double SDist = 2; // Дистанция поиска блоков || Distance of search blocks
        double WDist = 11; // Дистанция подрыва БЧ || Distance of explostion
        double WDist1 = 100; // Дистанция отсчёта БЧ || Distance of countdown for warhead
        double TimeR = 150; // Время запуска снаряда || Launch time 
        double TimerStartDist = 900; // Дистанция активации таймера || Distance of timer activation
        double MaxxValue = 650; // Коэффициент маневрирования для упреждения как ракеты || Maneuvrablity coeff of missile
        double Refresh1 = 150;
        double Time = 1;
        double refreshcams = 9; // Частота сканирования
        double IceAmount = 2000;
        double ATGMdist = 1000; // Точка куда будет лететь ПТУР будет на этом удалении от ориентира || Target point of ATGM will be on this distance from orient
        double MC = 55; // Коэффициент расстояния для ракеты (чем больше, тем сильнее) || Coeff of distance for missile (than more then more)
        double Modif = 1; // Коэффициент мощности гироскопа (усилитель). Должен соответствовать числу Pi (3.14) || The power factor of the gyroscope (amplifier). Must match the number Pi (3.14)
        //
        static bool ThrustStart = true; // Старт с двигателями || Start with thrusts
        static bool GravDemp = false; // Гасить гравитацию маршевыми двигателями? || Demp gravity by forward thrusters?
        static bool LowMode = false; // Уменьшить частоту вызова в 10 раз? (скажется на точности) || Reduce the frequency? (will affect on accuracy)
        static bool TurnOnMass = true; // Включать блоки массы на дистанции 400 метров от матер. корабля?
        static bool ENG = false; // English localisation?
        static bool AlternateGuidSystems = false; // Использовать альтернативные системы наведения при отсутствии целеуказания? // Use alternate guidance if no targets?
        static bool ExplOnMiss = false; // Детонация боеголовки не будет прекращена при риске промаха || detonation will be not stopped if miss
        static double RiskMiss = 20; // Если промах будет этого размера и меньше в метрах, детонация произойдёт || If missile misses on that distance from target missile will detonate
        static bool UseTimers = true; // Если на снаряде есть таймер (с названием "ТАЙМЕРСТАРТ") он будет активирован. || If missile have timer with name "ТАЙМЕРСТАРТ" it will be activated
        static bool ResetTurrets = true; // Если турель на ракете видит "лишнюю" цель, её целеуказание будет сброшено || If missile turret see not needed target his targeting will be reset
        // Индикатор загрузки || Load indicator
        public string IsLoad(string load)
        {
            if (load == "|") return "/";
            if (load == "/") return "_";
            if (load == "_") return "<";
            if (load == "<") return "|";
            return "";
        }
        string load = "|";
        //
        // Информация панели || LCD info
        // RU
        string str1 = "ЦП Ракет Р-4 ";
        string str2 = "Всего ракет: ";
        string str3 = "Из них запущено: ";
        string str4 = "Ракета ";
        string str5 = " в полёте";
        string str6 = " в процессе запуска";
        string str7 = " Дистанция до цели: ";
        string str8 = " Не видит цель!";
        // ENG
        string Estr1 = "Computer of R-4 missile system ";
        string Estr2 = "Total missiles: ";
        string Estr3 = "Launched: ";
        string Estr4 = "Missile ";
        string Estr5 = " in flight";
        string Estr6 = " is launching";
        string Estr7 = " Distance to target: ";
        string Estr8 = " Can't see target!";
        //
        //////////////////////////////////////////////////////////////////////////
        double ref1 = 1;
        public bool IsMerge(IMyTerminalBlock block)
        {
            IMyShipMergeBlock merge = block as IMyShipMergeBlock;
            if (merge != null) return true;
            return false;
        }
        public bool IsRemote(IMyTerminalBlock block)
        {
            IMyRemoteControl RC = block as IMyRemoteControl;
            if (RC != null) return true;
            return false;
        }
        public bool IsMass(IMyTerminalBlock block)
        {
            IMyVirtualMass mass = block as IMyVirtualMass;
            if (mass != null) return true;
            return false;
        }
        public bool IsAcb(IMyTerminalBlock block)
        {
            IMyBatteryBlock bat = block as IMyBatteryBlock;
            if (bat != null) return true;
            return false;
        }
        public bool IsWarhead(IMyTerminalBlock block)
        {
            IMyWarhead warhead = block as IMyWarhead;
            if (warhead != null) return true;
            return false;
        }
        public bool IsThrust(IMyTerminalBlock block)
        {
            IMyThrust thrust = block as IMyThrust;
            if (thrust != null) return true;
            return false;
        }
        public bool IsTimer(IMyTerminalBlock block)
        {
            IMyTimerBlock timer = block as IMyTimerBlock;
            if (timer != null) return true;
            return false;
        }
        public bool IsConnector(IMyTerminalBlock block)
        {
            IMyShipConnector connector = block as IMyShipConnector;
            if (connector != null) return true;
            return false;
        }
        public bool IsGyro(IMyTerminalBlock block)
        {
            IMyGyro gyro = block as IMyGyro;
            if (gyro != null) return true;
            return false;
        }
        public bool IsContainer(IMyTerminalBlock block)
        {
            IMyCargoContainer cargo = block as IMyCargoContainer;
            if (cargo != null) return true;
            return false;
        }
        public bool IsRotor(IMyTerminalBlock block)
        {
            IMyMotorStator rotor = block as IMyMotorStator;
            if (rotor != null) return true;
            return false;
        }
        public bool IsIceGen(IMyTerminalBlock block)
        {
            IMyGasGenerator gen = block as IMyGasGenerator;
            if (gen != null) return true;
            return false;
        }
        bool Fire = false;
        IMyTextPanel LCD;
        IMyTerminalBlock orient;
        IMyProgrammableBlock radar;
        List<IMyTerminalBlock> merges = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> RCS = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> mass = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> generators = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> bats = new List<IMyTerminalBlock>();
        List<IMyRemoteControl> FiredRCS = new List<IMyRemoteControl>();
        List<IMyTerminalBlock> Warheads = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Connectors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Thrusts = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Timers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Gyros = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> Rotors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> IceGens = new List<IMyTerminalBlock>();
        List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>();
        List<IMyLargeTurretBase> Turrets = new List<IMyLargeTurretBase>();
        List<IMySmallGatlingGun> gatlings = new List<IMySmallGatlingGun>();
        List<string> TargetsRC = new List<string>();
        List<string> Times = new List<string>();
        List<MyDetectedEntityInfo> Targets = new List<MyDetectedEntityInfo>();
        MyDetectedEntityInfo Target;
        double Refresh;
        StringBuilder sb = new StringBuilder();
        Program()
        {
            Refresh = Refresh1;
            if (!LowMode) Runtime.UpdateFrequency |= UpdateFrequency.Update1;
            else Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            if (LowMode) Time = 10;
            if (ENG)
            {
                str1 = Estr1;
                str2 = Estr2;
                str3 = Estr3;
                str4 = Estr4;
                str5 = Estr5;
                str6 = Estr6;
                str7 = Estr7;
                str8 = Estr8;
            }
        }
        void Main(String args)
        {
            Refresh--;
            ref1--;
            load = IsLoad(load);
            sb.Clear();
            radar = GridTerminalSystem.GetBlockWithName(RadarName) as IMyProgrammableBlock;
            orient = GridTerminalSystem.GetBlockWithName(OrientName);
            LCD = GridTerminalSystem.GetBlockWithName(LCDName) as IMyTextPanel;
            IMyCargoContainer MainCargo = GridTerminalSystem.GetBlockWithName(IceCargoName) as IMyCargoContainer;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(RCName, blocks, IsRemote);
            foreach (IMyTerminalBlock block in blocks) { if (!RCS.Contains(block)) RCS.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(MergeName, blocks, IsMerge);
            foreach (IMyTerminalBlock block in blocks) { if (!merges.Contains(block)) merges.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(MassName, blocks, IsMass);
            foreach (IMyTerminalBlock block in blocks) { if (!mass.Contains(block)) mass.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(AcbName, blocks, IsAcb);
            foreach (IMyTerminalBlock block in blocks) { if (!bats.Contains(block)) bats.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(WarheadName, blocks, IsWarhead);
            foreach (IMyTerminalBlock block in blocks) { if (!Warheads.Contains(block)) Warheads.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(ConnName, blocks, IsConnector);
            foreach (IMyTerminalBlock block in blocks) { if (!Connectors.Contains(block)) Connectors.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName("ТАЙМЕРСТАРТ", blocks, IsTimer);
            foreach (IMyTerminalBlock block in blocks) { if (!Timers.Contains(block)) Timers.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(ThrustName, blocks, IsThrust);
            foreach (IMyTerminalBlock block in blocks) { if (!Thrusts.Contains(block)) Thrusts.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(GyroName, blocks, IsGyro);
            foreach (IMyTerminalBlock block in blocks) { if (!Gyros.Contains(block)) Gyros.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(IceGenName, blocks, IsIceGen);
            foreach (IMyTerminalBlock block in blocks) { if (!IceGens.Contains(block)) IceGens.Add(block); }
            blocks.Clear();
            GridTerminalSystem.SearchBlocksOfName(RotorName, blocks, IsRotor);
            foreach (IMyTerminalBlock block in blocks) { if (!Rotors.Contains(block)) Rotors.Add(block); }
            blocks.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(blocks);
            foreach (IMyTerminalBlock block in blocks) { if (!Cameras.Contains((IMyCameraBlock)block)) Cameras.Add((IMyCameraBlock)block); }
            blocks.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(blocks);
            foreach (IMyTerminalBlock block in blocks) { if (!Turrets.Contains((IMyLargeTurretBase)block)) Turrets.Add((IMyLargeTurretBase)block); }
            blocks.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySmallGatlingGun>(blocks);
            foreach (IMyTerminalBlock block in blocks) { if (!gatlings.Contains((IMySmallGatlingGun)block)) gatlings.Add((IMySmallGatlingGun)block); }
            blocks.Clear();
            for (int i = merges.Count - 1; i >= 0; i--) { IMyShipMergeBlock merge = merges[i] as IMyShipMergeBlock; if (!merge.IsFunctional || merge == null) merges.RemoveAt(i); }
            for (int i = RCS.Count - 1; i >= 0; i--) { IMyRemoteControl merge = RCS[i] as IMyRemoteControl; if (!merge.IsFunctional || merge == null) RCS.RemoveAt(i); }
            for (int i = mass.Count - 1; i >= 0; i--) { IMyVirtualMass merge = mass[i] as IMyVirtualMass; if (!merge.IsFunctional || merge == null) mass.RemoveAt(i); }
            for (int i = bats.Count - 1; i >= 0; i--) { IMyBatteryBlock merge = bats[i] as IMyBatteryBlock; if (!merge.IsFunctional || merge == null) bats.RemoveAt(i); }
            for (int i = FiredRCS.Count - 1; i >= 0; i--) { IMyRemoteControl merge = FiredRCS[i]; if (!merge.IsFunctional || merge == null) FiredRCS.RemoveAt(i); }
            for (int i = Warheads.Count - 1; i >= 0; i--) { IMyWarhead merge = Warheads[i] as IMyWarhead; if (!merge.IsFunctional || merge == null) Warheads.RemoveAt(i); }
            for (int i = Connectors.Count - 1; i >= 0; i--) { IMyShipConnector merge = Connectors[i] as IMyShipConnector; if (!merge.IsFunctional || merge == null) Connectors.RemoveAt(i); }
            for (int i = Timers.Count - 1; i >= 0; i--) { IMyTimerBlock merge = Timers[i] as IMyTimerBlock; if (!merge.IsFunctional || merge == null) Timers.RemoveAt(i); }
            for (int i = Thrusts.Count - 1; i >= 0; i--) { IMyThrust merge = Thrusts[i] as IMyThrust; if (!merge.IsFunctional || merge == null) Thrusts.RemoveAt(i); }
            for (int i = Gyros.Count - 1; i >= 0; i--) { IMyGyro merge = Gyros[i] as IMyGyro; if (!merge.IsFunctional || merge == null) Gyros.RemoveAt(i); }
            for (int i = Rotors.Count - 1; i >= 0; i--) { IMyMotorStator merge = Rotors[i] as IMyMotorStator; if (!merge.IsFunctional || merge == null) Rotors.RemoveAt(i); }
            for (int i = IceGens.Count - 1; i >= 0; i--) { IMyGasGenerator merge = IceGens[i] as IMyGasGenerator; if (!merge.IsFunctional || merge == null) IceGens.RemoveAt(i); }
            for (int i = Cameras.Count - 1; i >= 0; i--) { IMyCameraBlock merge = Cameras[i]; if (!merge.IsFunctional || merge == null) Cameras.RemoveAt(i); }
            for (int i = Turrets.Count - 1; i >= 0; i--) { IMyLargeTurretBase merge = Turrets[i]; if (!merge.IsFunctional || merge == null) Turrets.RemoveAt(i); }
            double dist;
            if (LCD != null) { sb.Append(str1 + load); sb.AppendLine(); sb.Append(str2 + RCS.Count); sb.AppendLine(); sb.Append(str3 + FiredRCS.Count); sb.AppendLine(); }
            if (RCS.Count > 0)
            {
                if (MainCargo != null && IceGens.Count > 0)
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    MainCargo.GetInventory().GetItems(items);
                    foreach (MyInventoryItem item in items)
                    {
                        if (item.Type.SubtypeId.Equals("Ice"))
                        {
                            foreach (IMyGasGenerator gen in IceGens)
                            {
                                gen.UseConveyorSystem = false;
                                gen.Enabled = false;
                                if (MainCargo.GetInventory().IsConnectedTo(gen.GetInventory()))
                                {
                                    List<MyInventoryItem> items1 = new List<MyInventoryItem>();
                                    gen.GetInventory().GetItems(items1);
                                    if (items1.Count > 0)
                                    {
                                        double amount = 0;
                                        foreach (MyInventoryItem item1 in items1)
                                        {
                                            if (item1.Type.SubtypeId.Equals("Ice")) amount = (double)item1.Amount;
                                        }
                                        MainCargo.GetInventory().TransferItemTo(gen.GetInventory(), item, (VRage.MyFixedPoint)(IceAmount - amount));
                                    }
                                    else
                                    {
                                        MainCargo.GetInventory().TransferItemTo(gen.GetInventory(), item, (VRage.MyFixedPoint)IceAmount);
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (IMySmallGatlingGun gun in gatlings)
                {
                    if (gun.CustomName == GatlingName) gun.Enabled = false;
                }
                foreach (IMyRemoteControl RC in RCS)
                {
                    int MassCount = 0;
                    int ThrustCount = 0;
                    if (!FiredRCS.Contains(RC))
                    {
                        foreach (IMyBatteryBlock bat in bats)
                        {
                            Dist(bat, RC, out dist);
                            if (!Fire && !FiredRCS.Contains(RC)) if (dist <= SDist) bat.ChargeMode = ChargeMode.Recharge;
                        }
                        foreach (IMyArtificialMassBlock mas in mass)
                        {
                            Dist(mas, RC, out dist);
                            if (dist <= SDist) { mas.Enabled = false; MassCount++; }
                        }
                        foreach (IMyThrust mas in Thrusts)
                        {
                            Dist(mas, RC, out dist);
                            if (dist <= SDist) { mas.Enabled = false; ThrustCount++; }
                        }
                        foreach (IMyGravityGenerator gen in generators)
                        {
                            Dist(gen, RC, out dist);
                            if (dist <= SDist) gen.GravityAcceleration = 0;
                        }
                        foreach (IMyShipConnector conn in Connectors)
                        {
                            Dist(conn, RC, out dist);
                            if (dist <= SDist) { conn.Enabled = true; conn.Connect(); }
                        }
                    }
                    if (Fire && ThrustCount > 0 && (Target.EntityId != 0 || AlternateGuidSystems) && !FiredRCS.Contains(RC))
                    {
                        foreach (IMyBatteryBlock bat in bats)
                        {
                            Dist(bat, RC, out dist);
                            if (dist <= SDist) bat.ChargeMode = ChargeMode.Discharge;
                        }
                        foreach (IMyMotorStator rotor in Rotors)
                        {
                            Dist(rotor, RC, out dist);
                            if (dist <= SDist) rotor.Detach();
                        }
                        foreach (IMyGasGenerator gen in IceGens)
                        {
                            Dist(gen, RC, out dist);
                            if (dist <= SDist) { gen.Enabled = true; gen.UseConveyorSystem = true; }
                        }
                        foreach (IMyShipMergeBlock merge in merges)
                        {
                            Dist(merge, RC, out dist);
                            if (dist <= SDist) merge.Enabled = false;
                        }
                        foreach (IMyArtificialMassBlock mas in mass)
                        {
                            Dist(mas, RC, out dist);
                            if (dist <= SDist) mas.Enabled = true;
                        }
                        foreach (IMyShipConnector conn in Connectors)
                        {
                            Dist(conn, RC, out dist);
                            if (dist <= SDist) { conn.Enabled = false; conn.Disconnect(); }
                        }
                        if (ThrustStart)
                        {
                            foreach (IMyThrust thrust in Thrusts)
                            {
                                Dist(thrust, RC, out dist);
                                if (StartDirection == 1 && dist <= SDist && thrust.WorldMatrix.Backward == RC.WorldMatrix.Forward) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 1; }
                                if (StartDirection == 2 && dist <= SDist && thrust.WorldMatrix.Backward == RC.WorldMatrix.Up) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 1; }
                                if (StartDirection == 3 && dist <= SDist && thrust.WorldMatrix.Backward == RC.WorldMatrix.Down) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 1; }
                                if (StartDirection == 4 && dist <= SDist && thrust.WorldMatrix.Backward == RC.WorldMatrix.Left) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 1; }
                                if (StartDirection == 5 && dist <= SDist && thrust.WorldMatrix.Backward == RC.WorldMatrix.Right) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 1; }
                                if (StartDirection == 6 && dist <= SDist && thrust.WorldMatrix.Backward == RC.WorldMatrix.Backward) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 1; }
                            }
                        }
                        foreach (IMySmallGatlingGun gun in gatlings)
                        {
                            if (gun.CustomName == GatlingName)
                            {
                                Dist(RC, gun, out dist);
                                if (dist > SDist) continue;
                                gun.Enabled = true;
                                gun.ApplyAction("ShootOnce");
                                break;
                            }
                        }
                        FiredRCS.Add(RC);
                        string TimeLaunch = RC.GetId().ToString() + "b" + TimeR;
                        Times.Add(TimeLaunch);
                        string IdTarget = "";
                        if (!Target.IsEmpty()) IdTarget = RC.GetId().ToString() + "b" + Target.EntityId;
                        else IdTarget = RC.GetId().ToString() + "b" + 0;
                        TargetsRC.Add(IdTarget);
                        Fire = false;
                    }
                }
            }
            if (FiredRCS.Count > 0 && (Targets.Count > 0 || AlternateGuidSystems))
            {
                foreach (IMyRemoteControl RC in FiredRCS)
                {
                    bool exist = false;
                    for (int i = Times.Count - 1; i >= 0 || exist; i--)
                    {
                        string tim = Times[i];
                        string[] timstring = tim.Split('b');
                        long rcid;
                        long.TryParse(timstring[0], out rcid);
                        if (RC.GetId() == rcid)
                        {
                            double time;
                            double.TryParse(timstring[1], out time);
                            time--;
                            tim = RC.GetId().ToString() + "b" + time.ToString();
                            if (time > 0) { Times[i] = tim; exist = true; }
                            else { Times.RemoveAt(i); exist = false; }
                            Echo(time.ToString());
                            Echo(exist.ToString());
                            break;
                        }
                    }
                    if (exist) sb.Append(str4 + RC.EntityId + str6);
                    if (exist) sb.AppendLine();
                    if (exist) continue;
                    if (RC.IsSameConstructAs(Me)) { FiredRCS.Remove(RC); break; }
                    sb.Append(str4 + RC.EntityId + str5);
                    sb.AppendLine();
                    if (Targets.Count > 0 || AlternateGuidSystems)
                    {
                        long id = 0;
                        foreach (string target in TargetsRC)
                        {
                            string[] oh = target.Split('b');
                            long idr;
                            long.TryParse(oh[0], out idr);
                            if (idr == RC.GetId()) long.TryParse(oh[1], out id);
                        }

                        #region FROM

                        MyDetectedEntityInfo Targed = new MyDetectedEntityInfo();
                        foreach (MyDetectedEntityInfo target in Targets)
                        {
                            if (target.EntityId == id)
                            {
                                Targed = target;
                                bool See = false;
                                bool.TryParse(RC.CustomData, out See);
                                if (ref1 <= 0)
                                {
                                    for (int i = Cameras.Count - 1; i >= 0; i--)
                                    {
                                        IMyCameraBlock cam = Cameras[i];
                                        if (cam.IsSameConstructAs(RC))
                                        {
                                            Vector3D dir = (Vector3D)Targed.HitPosition + Vector3D.Normalize((Vector3D)Targed.HitPosition - cam.GetPosition()) * 0.05 + Targed.Velocity / 5 - cam.GetPosition();
                                            cam.Enabled = true;
                                            if (!cam.EnableRaycast) cam.EnableRaycast = true;
                                            MyDetectedEntityInfo targ;
                                            targ = cam.Raycast(dir);
                                            if (targ.IsEmpty()) { See = false; RC.CustomData = "false"; continue; }
                                            if (!targ.IsEmpty() && targ.EntityId == target.EntityId) { Refresh = Refresh1; Targed = targ; Targets.Remove(target); Targets.Add(targ); See = true; RC.CustomData = "true"; break; }
                                        }
                                    }
                                }
                                if (!See) { sb.Append(str4 + str8); sb.AppendLine(); }
                                for (int i = Turrets.Count - 1; i >= 0; i--)
                                {
                                    IMyLargeTurretBase Turret = Turrets[i];
                                    if (Turret.IsSameConstructAs(RC))
                                    {
                                        Turret.Enabled = true;
                                        MyDetectedEntityInfo targ;
                                        if (Turret.HasTarget) targ = Turret.GetTargetedEntity();
                                        else continue;
                                        if (!targ.IsEmpty() && targ.EntityId == target.EntityId) { Refresh = Refresh1; Targed = targ; Targets.Remove(target); Targets.Add(targ); break; }
                                        else if (ResetTurrets) Turret.ResetTargetingToDefault();
                                    }
                                }
                                break;
                            }
                        }
                        if (Targets.Count > 0 || AlternateGuidSystems)
                        {
                            List<IMyThrust> BordThrust = new List<IMyThrust>();
                            foreach (IMyThrust thrust in Thrusts)
                            {
                                if (thrust.IsSameConstructAs(RC)) BordThrust.Add(thrust);
                            }
                            Vector3D ShootVector;
                            Vector3D Direction = new Vector3D();
                            if (Targed.EntityId != 0)
                            {
                                Vector3D EnemyPos = (Vector3D)Targed.HitPosition;
                                if (Targed.HitPosition == null) EnemyPos = Targed.Position;
                                Vector3D OurPos = RC.GetPosition();
                                Direction = EnemyPos - OurPos;
                                Vector3D targetVelOrth = Vector3D.Dot(Targed.Velocity, Vector3D.Normalize(Direction)) * Vector3D.Normalize(Direction);
                                Vector3D targetVelTang = Vector3D.Reject(Targed.Velocity, Vector3D.Normalize(Direction));
                                if (targetVelTang.Length() > RC.GetShipSpeed())
                                {
                                    ShootVector = Vector3D.Normalize(Targed.Velocity) * RC.GetShipSpeed() * Time;
                                }
                                else
                                {
                                    double shotSpeedOrth = Math.Sqrt(RC.GetShipSpeed() * RC.GetShipSpeed() - targetVelTang.Length() * targetVelTang.Length());
                                    Vector3D shotVelOrth = Vector3D.Normalize(Direction) * shotSpeedOrth;
                                    ShootVector = (shotVelOrth + targetVelTang) * Time;
                                }
                            }
                            else
                            {
                                if (orient == null) ShootVector = Me.GetPosition() - RC.GetPosition() + Me.CubeGrid.WorldMatrix.Forward * ATGMdist;
                                else ShootVector = orient.GetPosition() - RC.GetPosition() + orient.WorldMatrix.Forward * ATGMdist;
                                if (ShootVector.Length() <= 20)
                                {
                                    if (orient == null) ShootVector = Me.GetPosition() - RC.GetPosition() + Me.CubeGrid.WorldMatrix.Forward * 10000;
                                    else ShootVector = orient.GetPosition() - RC.GetPosition() + orient.WorldMatrix.Forward * 10000;
                                }
                                Direction = new Vector3D(0, 10000, 0);
                            }
                            double FForward = Vector3D.Dot(RC.WorldMatrix.Forward, Vector3D.Normalize(ShootVector)) / ShootVector.Length() * 100;
                            double RRight = Vector3D.Dot(RC.WorldMatrix.Right, Vector3D.Normalize(ShootVector)) / ShootVector.Length() * 100;
                            double UUp = Vector3D.Dot(RC.WorldMatrix.Up, Vector3D.Normalize(ShootVector)) / ShootVector.Length() * 100;
                            sb.Append(str7 + Direction.Length());
                            sb.AppendLine();
                            CustomReflect(RC.GetShipVelocities().LinearVelocity, ShootVector, out ShootVector);
                            if (Vector3D.Dot(RC.GetShipVelocities().LinearVelocity, Vector3D.Normalize(ShootVector)) > 180)
                            {
                                ShootVector = Vector3D.Reject(-RC.GetShipVelocities().LinearVelocity, Vector3D.Normalize(ShootVector));
                            }
                            if (UseTimers) TimerAct(RC, Direction);
                            List<IMyGyro> BordGyros = new List<IMyGyro>();
                            foreach (IMyGyro gyro in Gyros)
                            {
                                if (!BordGyros.Contains(gyro))
                                {
                                    if (gyro.IsSameConstructAs(RC))
                                    {
                                        BordGyros.Add(gyro);
                                    }
                                }
                            }
                            foreach (IMyGyro gyro in BordGyros)
                            {
                                gyro.Enabled = true;
                                float gForward = 0;
                                float gRight = 0;
                                float gUp = 0;
                                Vector3D grav = new Vector3D();
                                if (RC.GetNaturalGravity().Length() > 0) grav = RC.GetNaturalGravity();
                                if (!GravDemp || RC.GetNaturalGravity().Length() == 0)
                                {
                                    gForward = (float)(Vector3D.Dot(gyro.WorldMatrix.Forward, Vector3D.Normalize(ShootVector)) / ShootVector.Length() * 100);
                                    gRight = (float)(Vector3D.Dot(gyro.WorldMatrix.Right, Vector3D.Normalize(ShootVector)) / ShootVector.Length() * 100);
                                    gUp = (float)(Vector3D.Dot(gyro.WorldMatrix.Up, Vector3D.Normalize(ShootVector)) / ShootVector.Length() * 100);
                                }
                                else
                                {
                                    gForward = (float)(Vector3D.Dot(gyro.WorldMatrix.Forward, Vector3D.Normalize(ShootVector) * grav) / ShootVector.Length() * 100);
                                    gRight = (float)(Vector3D.Dot(gyro.WorldMatrix.Right, Vector3D.Normalize(ShootVector) * grav) / ShootVector.Length() * 100);
                                    gUp = (float)(Vector3D.Dot(gyro.WorldMatrix.Up, Vector3D.Normalize(ShootVector) * grav) / ShootVector.Length() * 100);
                                }
                                gyro.GyroOverride = true;
                                double D = ShootVector.Length();
                                if (D > 1000) D = 1000;
                                if (gyro.WorldMatrix.Forward == RC.WorldMatrix.Forward)
                                {
                                    gyro.Pitch = (float)(Math.Atan2(-gUp, gForward) / D * MC * Modif);
                                    gyro.Yaw = (float)(Math.Atan2(gRight, gForward) / D * MC * Modif);
                                    gyro.Roll = (float)(Math.Atan2(gRight, gUp) * 1.5);
                                }
                                if (gyro.WorldMatrix.Backward == RC.WorldMatrix.Forward)
                                {
                                    gyro.Pitch = (float)(-Math.Atan2(-gUp, -gForward) / D * MC * Modif);
                                    gyro.Yaw = (float)(Math.Atan2(gRight, -gForward) / D * MC * Modif);
                                    gyro.Roll = (float)(Math.Atan2(gRight, gUp) * 1.5);
                                }
                                if (gyro.WorldMatrix.Right == RC.WorldMatrix.Forward)
                                {
                                    gyro.Roll = (float)(-Math.Atan2(gUp, gRight) / D * MC * Modif);
                                    gyro.Yaw = (float)(-Math.Atan2(gForward, gRight) / D * MC * Modif);
                                    gyro.Pitch = (float)(Math.Atan2(gForward, gUp) * 1.5);
                                }
                                if (gyro.WorldMatrix.Left == RC.WorldMatrix.Forward)
                                {
                                    gyro.Roll = (float)(Math.Atan2(gUp, -gRight) / D * MC * Modif);
                                    gyro.Yaw = (float)(Math.Atan2(gForward, -gRight) / D * MC * Modif);
                                    gyro.Pitch = (float)(-Math.Atan2(gForward, gUp) * 1.5);
                                }
                                if (gyro.WorldMatrix.Up == RC.WorldMatrix.Forward)
                                {
                                    gyro.Pitch = (float)(Math.Atan2(gForward, gUp) / D * MC * Modif);
                                    gyro.Roll = (float)(Math.Atan2(gRight, gUp) / D * MC * Modif);
                                    gyro.Yaw = (float)(Math.Atan2(gForward, gRight) * 1.5);
                                }
                                if (gyro.WorldMatrix.Down == RC.WorldMatrix.Forward)
                                {
                                    gyro.Pitch = -(float)(-Math.Atan2(gForward, -gUp) / D * MC * Modif);
                                    gyro.Roll = -(float)(-Math.Atan2(gRight, -gUp) / D * MC * Modif);
                                    gyro.Yaw = (float)(-Math.Atan2(gForward, gRight) * 1.5);
                                }
                            }
                            foreach (IMyArtificialMassBlock mas in mass)
                            {
                                if (mas.IsSameConstructAs(RC)) mas.Enabled = false;
                            }
                            if (FForward > 0)
                            {
                                foreach (IMyThrust thrust in BordThrust)
                                {
                                    if (thrust.WorldMatrix.Backward == RC.WorldMatrix.Forward) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 1; }
                                    if (thrust.WorldMatrix.Backward != RC.WorldMatrix.Forward) { thrust.Enabled = true; thrust.ThrustOverridePercentage = 0; }
                                }
                            }
                            else
                            {
                                foreach (IMyThrust thrust in BordThrust)
                                {
                                    thrust.Enabled = true;
                                    thrust.ThrustOverridePercentage = 0;
                                }
                            }
                            foreach (IMyWarhead warhead in Warheads)
                            {
                                if (warhead.IsSameConstructAs(RC) && !warhead.IsCountingDown && Direction.Length() < WDist1 && (ExplOnMiss || (UUp < RiskMiss && UUp > -RiskMiss && RRight < RiskMiss && RRight > -RiskMiss))) { warhead.DetonationTime = (float)(Direction.Length() / (RC.GetShipVelocities().LinearVelocity - Targed.Velocity).Length()); warhead.StartCountdown(); }
                                else if (warhead.IsSameConstructAs(RC) && warhead.IsCountingDown && Direction.Length() > WDist1 && !ExplOnMiss) warhead.StopCountdown();
                                if (warhead.IsSameConstructAs(RC) && Direction.Length() < WDist) { warhead.IsArmed = true; warhead.Detonate(); }
                            }
                        }

                        #endregion TO

                        foreach (IMyArtificialMassBlock mas in mass)
                        {
                            if (mas.IsSameConstructAs(RC) && TurnOnMass && (Me.CubeGrid.GetPosition() - RC.GetPosition()).Length() <= 400) mas.Enabled = true;
                        }
                    }
                }
            }
            if (ref1 <= 0) ref1 = refreshcams;
            if (!ENG)
            {
                Echo("ЦП Р-4 " + load);
                Echo("Сделано в Н.С.К.С.");
                Echo("Ракет доступно: " + RCS.Count);
                Echo("Из них запущено: " + FiredRCS.Count);
                Echo("Кол-во переданных целей: " + Targets.Count);
            }
            else
            {
                Echo("R-4 Missile System" + load);
                Echo("Made in N.U.C.C ");
                Echo("Total missiles: " + RCS.Count);
                Echo("Launched: " + FiredRCS.Count);
                Echo("Amount of transmited targets: " + Targets.Count);
            }
            if (!ENG) sb.Append("Ошибки:");
            else sb.Append("Errors:");
            sb.AppendLine();
            if (!ENG) if (MainCargo == null) sb.Append("Не найден контейнер со льдом");
                else if (MainCargo == null) sb.Append("Cargo with ice not found");
            sb.AppendLine();
            if (Target.EntityId == 0) { if (!ENG) sb.Append("Атакуемой цели нет"); if (ENG) sb.Append("No target for attack"); sb.AppendLine(); }
            if (LCD != null) { LCD.Enabled = true; LCD.WriteText(sb.ToString()); LCD.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE; }

            /***************************/
            switch (args)
            {
                case "FIRE": if (Target.EntityId != 0 || AlternateGuidSystems) Fire = true; break;
                case "EXPL":
                    foreach (IMyRemoteControl RC in FiredRCS)
                    {
                        foreach (IMyWarhead warhead in Warheads)
                        {
                            if (warhead.IsSameConstructAs(RC)) { warhead.IsArmed = true; warhead.Detonate(); }
                        }
                    }
                    ; break;
            }
            if (radar != null && radar.Enabled)
            {
                Targets.Clear();
                int q = 0;
                string[] rinfo = radar.CustomData.Split('@');
                int.TryParse(rinfo[0], out q);

                if (q > 0)
                {
                    Refresh = Refresh1;
                    string[] info = rinfo[1].Split('b');
                    Vector3D Pos;
                    Vector3D.TryParse(info[0], out Pos);
                    Vector3D Veloc;
                    Vector3D.TryParse(info[1], out Veloc);
                    Vector3D min;
                    Vector3D max;
                    Vector3D.TryParse(info[2], out min);
                    Vector3D.TryParse(info[3], out max);
                    BoundingBoxD Box = new BoundingBoxD(min, max);
                    long id;
                    long.TryParse(info[4], out id);
                    Target = new MyDetectedEntityInfo(id, "TargetedEnt", MyDetectedEntityType.LargeGrid, Pos, new MatrixD(), Veloc, MyRelationsBetweenPlayerAndBlock.Enemies, Box, 0);
                    if (Targets.Count > 0 && Target.EntityId != 0)
                    {
                        bool exist = false;
                        foreach (MyDetectedEntityInfo target in Targets)
                        {
                            if (target.EntityId == Target.EntityId) { exist = true; break; }
                        }
                        if (!exist) Targets.Add(Target);
                        if (exist)
                        {
                            foreach (MyDetectedEntityInfo target in Targets)
                            {
                                if (target.EntityId == id) { Targets.Remove(target); Targets.Add(Target); break; }
                            }
                        }
                    }
                    else if (Target.EntityId != 0) Targets.Add(Target);

                    for (int i = 2; i <= q; i++)
                    {
                        info = rinfo[i].Split('b');
                        Vector3D.TryParse(info[0], out Pos);
                        Vector3D.TryParse(info[1], out Veloc);
                        Vector3D.TryParse(info[2], out min);
                        Vector3D.TryParse(info[3], out max);
                        Box = new BoundingBoxD(min, max);
                        long.TryParse(info[4], out id);
                        MyDetectedEntityInfo Darget = new MyDetectedEntityInfo(id, "TargetedEnt", MyDetectedEntityType.LargeGrid, Pos, new MatrixD(), Veloc, MyRelationsBetweenPlayerAndBlock.Enemies, Box, 0);
                        if (Targets.Count > 0 && Darget.EntityId != 0)
                        {
                            bool exist = false;
                            foreach (MyDetectedEntityInfo target in Targets)
                            {
                                if (target.EntityId == Darget.EntityId) { exist = true; break; }
                            }
                            if (!exist) Targets.Add(Darget);
                            if (exist)
                            {
                                foreach (MyDetectedEntityInfo target in Targets)
                                {
                                    if (target.EntityId == id) { Targets.Remove(target); Targets.Add(Darget); break; }
                                }
                            }
                        }
                        else if (Darget.EntityId != 0) Targets.Add(Darget);
                    }
                }
            }
            if (args.EndsWith("TTARGET") && !args.EndsWith("TARGET"))
            {
                string[] info = args.Split('b');
                Vector3D Pos;
                Vector3D.TryParse(info[0], out Pos);
                Vector3D Veloc;
                Vector3D.TryParse(info[1], out Veloc);
                Vector3D min;
                Vector3D max;
                Vector3D.TryParse(info[2], out min);
                Vector3D.TryParse(info[3], out max);
                BoundingBoxD Box = new BoundingBoxD(min, max);
                long id;
                long.TryParse(info[4], out id);
                Target = new MyDetectedEntityInfo(id, "TargetedEnt", MyDetectedEntityType.LargeGrid, null, new MatrixD(), Veloc, MyRelationsBetweenPlayerAndBlock.Enemies, Box, 0);
                if (Target.EntityId != 0) Refresh = Refresh1;
                if (Targets.Count > 0 && Target.EntityId != 0)
                {
                    bool exist = false;
                    foreach (MyDetectedEntityInfo target in Targets)
                    {
                        if (target.EntityId == Target.EntityId) { exist = true; if (exist) { Targets.Remove(target); Targets.Add(Target); } break; }
                    }
                    if (!exist) Targets.Add(Target);
                }
                else if (Target.EntityId != 0) Targets.Add(Target);
            }
            if (args.EndsWith("TARGET") && !args.EndsWith("TTARGET"))
            {
                string[] info = args.Split('b');
                Vector3D Pos;
                Vector3D.TryParse(info[0], out Pos);
                Vector3D Veloc;
                Vector3D.TryParse(info[1], out Veloc);
                Vector3D min;
                Vector3D max;
                Vector3D.TryParse(info[2], out min);
                Vector3D.TryParse(info[3], out max);
                BoundingBoxD Box = new BoundingBoxD(min, max);
                long id;
                long.TryParse(info[4], out id);
                MyDetectedEntityInfo Darget = new MyDetectedEntityInfo(id, "TargetedEnt", MyDetectedEntityType.LargeGrid, null, new MatrixD(), Veloc, MyRelationsBetweenPlayerAndBlock.Enemies, Box, 0);

                if (Targets.Count > 0 && Darget.EntityId != 0)
                {
                    bool exist = false;
                    foreach (MyDetectedEntityInfo target in Targets)
                    {
                        if (target.EntityId == Darget.EntityId) { exist = true; if (exist) { Targets.Remove(target); Targets.Add(Darget); } break; }
                    }
                    if (!exist) Targets.Add(Darget);
                }
                else if (Darget.EntityId != 0) Targets.Add(Darget);
            }
            if (Refresh <= 0) { Refresh = Refresh1; Targets.Clear(); Target = new MyDetectedEntityInfo(); }
        }
        void Dist(IMyTerminalBlock block, IMyTerminalBlock block1, out double Distance)
        {
            if (!block.IsSameConstructAs(block1)) { Distance = 1000000000; return; }
            Distance = (block.GetPosition() - block1.GetPosition()).Length();
        }
        void TimerAct(IMyRemoteControl RC, Vector3D Direction)
        {
            if (TimerStartDist >= Direction.Length())
            {
                foreach (IMyTimerBlock timer in Timers)
                {
                    if (timer.IsSameConstructAs(RC))
                    {
                        timer.Enabled = true;
                        timer.Trigger();
                        break;
                    }
                }
            }
        }
        void CustomReflect(Vector3D velocity, Vector3D direction, out Vector3D direction1)
        {

            Vector3D rej = Vector3D.Reject(velocity, Vector3D.Normalize(direction));
            direction1 = velocity - rej * (MaxxValue / direction.Length());
            if (Vector3D.Dot(velocity, direction) < 180)
            {
                rej = Vector3D.Reject(-velocity, Vector3D.Normalize(direction));
                direction1 = -velocity + rej * (MaxxValue / direction.Length());
            }
        }
    }
}
