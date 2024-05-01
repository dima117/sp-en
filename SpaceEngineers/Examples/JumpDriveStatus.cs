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

namespace xxx2
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

        /*
         
Тип: Прыжковый двигатель
Макс. потребление: 32.00 MW
Макс. заряд: 3.00 MWh
Потребление: 32.00 MW (80%)
Заряд: 1.81 MWh
Полностью зарядится через: 3 мин.
Макс. дальность прыжка:0 km

Потребление: 200 W
Заряд: 500.00 kWh
Полностью зарядится через: 0 сек.
100.00
         */

        readonly IMyTextPanel lcd;
        readonly IMySmallMissileLauncherReload railgun;
        readonly IMyJumpDrive jumpDrive;

        public Program()
        {
            lcd = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
            railgun = GridTerminalSystem.GetBlockWithName("RG") as IMySmallMissileLauncherReload;
            jumpDrive = GridTerminalSystem.GetBlockWithName("JD") as IMyJumpDrive;
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            lcd.WriteText(jumpDrive.DetailedInfo);
            lcd.WriteText(GetJumpDriveChargePercent(jumpDrive).ToString("\n0.00"), true);
            lcd.WriteText("=====", true);
            lcd.WriteText(railgun.DetailedInfo, true);
            lcd.WriteText(GetRailgunChargePercent(railgun).ToString("\n0.00"), true);
        }

        float GetRailgunChargePercent(IMySmallMissileLauncherReload railgun, float max = 500000)
        {
            if (railgun.BlockDefinition.SubtypeId != "LargeRailgun")
            {
                return 0f;
            }

            var lines = railgun.DetailedInfo.Split('\n');
            float result = 100 * ParseNumber(lines[1]) / max;

            return result;
        }

        float GetJumpDriveChargePercent(IMyJumpDrive jumpDrive)
        {
            var max = jumpDrive.MaxStoredPower;
            var current = jumpDrive.CurrentStoredPower;

            float result = 100 * current / max;

            return result;
        }

        float ParseNumber(string str)
        {
            var start = 0;

            while (!char.IsDigit(str[start]))
            {
                start++;
            }

            var end = start + 1;
            while (char.IsDigit(str[end]) || str[end] == '.')
            {
                end++;
            }

            var strValue = str.Substring(start, end - start);
            var measure = str.Substring(end + 1, 3).Trim();

            var value = Convert.ToSingle(strValue);

            switch (measure)
            {
                case "Wh":
                    return value;
                case "kWh":
                    return value * 1000;
                case "MWh":
                    return value * 1000000;
                default:
                    return 0;
            }
        }
    }
}
