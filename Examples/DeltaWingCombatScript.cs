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
using Sandbox.Game.Entities;

namespace Examples.DeltaWingCombatScript
{
    public sealed class Program : MyGridProgram
    {
        /*
 *         Current features:
 *         Target prioritization - prioritizes the target that you have the most locked onto
 *         Acceleration - Accounts for acceleration in addition to velocity and position
 *         Super easy setup - Place all the required blocks in a group, and you're good to go. Promise.
 *         Toggle on/off: - see arguments below
 *         Set velocity - see arguments below
 *         Turret based targeting - LiDAR to be added later
 *         Completely autonomous AI mode - simulates how a good player might fly
 * 
 * 
 *         Arguments (all case INsensitive):
 *         toggle ship aim                             Master switch for aiming
 *         set velocity                                Sets the velocity of the projectile used for lead calculations
 *         set aim type                                Sets the aim type, valid arguments are CenterOfMass, TurretAverage, and RandomTurretTarget. Still case INsensitive.
 *         cycle aim type                              Cycles the aim type between the three valid types
 *         toggle turret aim                           Toggles turret aiming override
 *         set turret velocity                         Sets the velocity of the projectile used for turret lead calculations
 *         unfuck turrets                              Attempts to fix turrets that have broken due to Keen. Causes turrets to retarget in the process
 *         retarget turrets                            Same as unfuck
 * 
 * 
 *         TBA:
 *         Individual weapon aim and fire (like that one turret script I made)
 *         LiDAR based targeting
 * 
 * 
 *         To set up:
 *         Place all of your gyroscopes, targeting turrets (artillery etc), and a seat in a group. 
 *         OPTIONAL: Add a LCD to the group to display targeting information, add railguns and thrusters to the group for AI controlled spin to win and firing
 */


        //Inconspcuous name :D
        string GroupName = "Flight Control";



        float ProjectileVelocity = 2000;                //Initialize this if you only have one primary weapon, otherwise run with argument to set velocity
        float ProjectileMaxDist = 2000;                 //Maximum distance to target, used for determining if the ship should fire or not, and if it should approach the target or not
        float TurretVelocity = 500.0f;                  //velocity that the turrets use for turret aim overriding
        float rollSensitivityMultiplier = 1;            //Increase if spinning too slowly, decrease if spi   nning too quickly
        float maxAngular = 30.0f;                       //Max angular velocity in RPM, set to 60 for small gridId and 30 for large gridId (or something less if you wish for slower seek)


        AimType aimType = AimType.CenterOfMass;         //Valid options are CenterOfMass, TurretAverage, and RandomTurretTarget. Can also be set with argument


        /// COMPLETELY AUTONOMOUS MODE ///
        bool AutonomousMode = true;                     // complete control over the ship
        float autonomousDesiredDistance = 1500;         //Distance to idle at
        double autonomousRollPower = 0.5;               //speed to roll at
        float autonomousFireSigma = 0.9997f;            //how close to on target the ship needs to be to fire
        static int autonomousMinFramesOnTarget = 3;     //minimum frames o be on target before firing

        bool SendEnemyLocation = true;                  //Send enemy location to other ships
        bool ReceiveEnemyLocation = true;               //Receive enemy location from other ships (autonomous required)
        string TaskForceTag = "TaskForceOne";           //Tag use for co ordination between ships, use a different tag if you want co ordination with a different group

        float FriendlyAvoidanceThreshold = 10;          //Distance to stop moving away from friendlies

        //Used for maintaining distance
        float autonomouskP = 0.5f;
        float autonomouskI = 0.0f;
        float autonomouskD = 1f;


        /// PID CONFIG ///
        float kP = 40.0f;
        float kI = 0.0f;
        float kD = 15.0f;

        const double TimeStep = 1.0 / 60.0;

        //offset the forward reference
        float OffsetVert = -5;                          //offset in meters, positive is up
        float OffsetCoax = 0;                           //offset in meters, positive is forward
        float OffsetHoriz = 0;                          //offset in meters, positive is right

        float PassiveRadius = 300;                      //For passive antenna range
        float TransmitRadius = 50000;                   //For transmitting enemy location
        string TransmitMessage = "";

        bool UseRandomTransmitMessage = true;
        const int framesPerTransmitMessage = 1200;
        List<string> splitText = new List<string>();
        string[] TransmitMessages =
        {
    "Do you know who ate all the doughnuts?",
    "Sometimes I dream about cheese.",
    "Why do we all have to wear these ridiculous ties?",

    "America will never fall to Communist invasion.",
    "Commencing tactical assessment. Red Chinese threat detected.",
    "Democracy is non-negotiable.",
    "Engaging Red Chinese aggressors.",
    "Freedom is the sovereign right of every American.",
    "Death is a preferable alternative to Communism.",
    "Chairman Cheng will fail. China will fall.",
    "Communist engaged.",
    "Communist detected on American soil. Lethal force engaged.",
    "Democracy will never be defeated.",
    "Alaska's liberation is imminent.",
    "Engaging Chinese invader.",
    "Communism is a lie.",
    "Initiating directive 7395 -- destroy all Communists.",
    "Tactical assessment: Red Chinese victory... impossible.",
    "Communist target acquired.",
    "Anchorage will be liberated.",
    "Communism is the very definition of failure.",
    "The last domino falls here.",
    "We will not fear the Red Menace.",
    "Communism is a temporary setback on the road to freedom.",
    "Embrace democracy, or you will be eradicated.",
    "Democracy is truth. Communism is death.",
    "Voice module online. Audio functionality test... initialized. Designation: Liberty Prime. Mission: the Liberation of Anchorage, Alaska.",
    "Bzzzt.",
    "Established strategem: Inadequate.",
    "Revised strategy: Initiate photonic resonance overcharge.",
    "Significant obstruction detected. Composition: Titanium alloy supplemented by enhanced photonic resonance barrier.",
    "Obstruction detected. Composition: Titanium alloy supplemented by photonic resonance barrier. Probability of mission hindrance: zero percent.",
    "Obstruction detected. Composition: Titanium alloy supplemented by photonic resonance barrier. Chinese blockade attempt: futile.",
    "Warning: Forcible impact alert. Scanning for Chinese artillery.",
    "Liberty Prime is online. All systems nominal. Weapons hot. Mission: the destruction of any and all Chinese communists.",
    "Catastrophic system failure. Initiating core shutdown as per emergency initiative 2682209. I die so that democracy may live.",
    "Repeat: Red Chinese orbital strike inbound! All U.S Army personnel must vacate the area immediately! Protection protocals engaged!",
    "Warning! Warning! Red Chinese orbital strike imminent! All personnel should reach minimum safe distance immediately!",
    "Satellite Uplink detected. Analysis of Communist transmission pending.",
    "Structural weakness detected. Exploiting.",
    "Communist threat assessment: Minimal. Scanning defenses...",
    "Liberty Prime... back online.",
    "Diagnostic command: accepted.",
    "Desigation: Liberty Prime Mark II. Mission: the liberation of Anchorage, Alaska.",
    "Primary Targets: any and all Red Chinese invaders.",
    "All systems: nominal. Weapons: hot.",
    "Warning: Nuclear weapon payload depleted. Reload required.",
    "Warning: Power Core offline. Running on external power only. Core restart recommended.",
    "Ability to repel Red Chinese invaders: compromised.",
    "Updated tactical assessment: Red Chinese presence detected.",
    "Aerial incursion by Communist forces cannon succeed.",
    "Global positioning initialized. Location: the Commonwealth of Massachusetts. Birthplace of American freedom.",
    "Designation: Liberty Prime. Operational assessment: All systems nominal. Primary directive: War.",
    "Area classified as active warzone. Engaging sentry protocols. Weapons hot.",
    "System diagnostic commencing. Mobility - Complete. Optic beam - fully charged. Nuclear warheads - armed.",
    "Defending Life, Liberty and the pursuit of happiness.",
    "Only together can we stop the spread of communism.",
    "Cultural database accessed. Quoting New England poet Robert Frost: 'Freedom lies in being bold.'",
    "Accessing dictionary database. Entry: democracy. A form of government in which the leader is chosen by vote, and everyone has equal rights.",
    "Accessing dictionary database. Entry: communism. A form of government in which the state controls everything, and people are denied... freedom",
    "I am Liberty Prime. I am America.",
    "Scanners operating at 100% efficiency. Enemy presence detected. Attack imminent.",
    "Mission proceeding as planned.",
    "Defense protocols active. All friendly forces - remain in close proximity.",
    "Democracy is the essence of good. Communism, the very definition of evil.",
    "Freedom is always worth fighting for.",
    "Democracy is freedom. Communism is tyranny.",
    "I hold these truths to be self-evident that all Americans are created... equal. And are endowed with certain unalienable rights",
    "Victory is assured.",
    "American casualties unacceptable. Overkill protocols authorized.",
    "Glory is the reward of valor.",
    "Defeat is not an option.",
    "Commence tactical assessment: Red Chinese threat detected.",
    "Proceeding to target coordinates.",
    "Fusion Core: reinitialized.",
    "Liberty Prime full system analysis.",
    "Hostile software detected. Communist subversion likely.",
    "Targeting... parameters...offline. Re-calibrating...",
    "Red Chinese Infiltration Unit: eliminated. Let freedom ring.",
    "Obstruction: eliminated.",
    "Ground units initiate Directive 7395. Destroy all Communists!",
    "Memorial site: recognized.",
    "Patriotism subroutines: engaged.",
    "Honoring the fallen is the duty of every red-blooded American.",
    "Obstruction detected. Overland travel to target: compromised.",
    "Probability of mission hindrance: thirty-two percent.",
    "Revised stratagem: initiated. Aquatic transit protocol: activated.",
    "Probability of mission hindrance: zero percent.",
    "Democracy is truth. Communism is death. Anchorage will be liberated.",
    "Objective reached.",
    "Scanning defenses.",
    "Scanning results, negative.",
    "Warning: subterranean Red Chinese compound detected.",
    "Obstruction depth: five meters. Composition: sand, gravel and communism.",
    "Tactical assessment: Breach compound to restore democracy.",
    "Warning: all personnel should move to minimum safe distance.",

};

        //No touchy below >:(
        Vector3D FriendlyAvoidanceVector = Vector3D.Zero;
        string EnemyLocationTag = "EnemyLocation";
        string CurrentlyAttackingEnemyTag = "CurrentlyAttackingEnemy";
        string CoordinationPositionalDataTag = "CoordinationPositionalData";
        int framesSinceLastTransmitMessage = 0;
        IMyBroadcastListener EnemyLocator;
        IMyBroadcastListener CurrentlyAttackingEnemy;
        IMyBroadcastListener CoordinationPositionalData;
        ClampedIntegralPID forwardBackwardPID;
        bool[] autonomousOnTarget = new bool[autonomousMinFramesOnTarget];

        int maximumLogLength = 20;

        string echoMessage = "";
        bool aim = true;

        Random rng = new Random();
        List<IMyShipController> controllers;
        IMyShipController currentController;
        List<IMyLargeTurretBase> turrets;
        Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> turretTargets = new Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo>();
        List<IMyGyro> gyros;
        List<IMyTextPanel> panels;
        List<IMyThrust> allThrusters;
        Thrusters thrusters;
        List<IMyRadioAntenna> antennas;
        //make a list of railguns
        List<IMySmallMissileLauncherReload> railguns;
        Vector3D averageGunPos = Vector3D.Zero;
        List<IMyJumpDrive> jumpDrives;

        static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        const float IdlePowerDraw = 0.0002f;
        const float Epsilon = 1e-6f;


        MyDetectedEntityInfo target;
        Vector3D primaryShipAimPos = Vector3D.Zero;
        bool hasTarget = false;

        ShipAim ShipAim;
        ShipAimUpdate newDetails = new ShipAimUpdate();

        string[] Args =
        {
    "toggle ship aim",
    "set velocity",
    "set aim type",
    "cycle aim type",
    "toggle turret aim",
    "set turret velocity",
    "unfuck turrets",
    "retarget turrets",
};
        public enum AimType
        {
            CenterOfMass, //Useful for maneuverable and small targets
            TurretAverage, //Useful for large targets
            RandomTurretTarget //Useful for strike runs on large targets, or sniping reactors and other critical components
        }

        public Program()
        {
            Targeting.program = this;
            Turrets.program = this;
            GetGroupBlocks();
            InitializeShipAim();
            InitializeTurrets();
            InitializeThrusters();
            InitializeIGC();


            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;
            LCDManager.InitializePanels(panels);
            LCDManager.program = this;
            LCDManager.WriteText();
        }

        private void GetGroupBlocks()
        {
            gyros = new List<IMyGyro>();
            turrets = new List<IMyLargeTurretBase>();
            controllers = new List<IMyShipController>();
            panels = new List<IMyTextPanel>();
            allThrusters = new List<IMyThrust>();
            railguns = new List<IMySmallMissileLauncherReload>();
            jumpDrives = new List<IMyJumpDrive>();
            antennas = new List<IMyRadioAntenna>();
            bool groupFound = false;

            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            foreach (IMyBlockGroup group in groups)
            {
                if (group.Name == GroupName)
                {
                    groupFound = true;
                    group.GetBlocksOfType(turrets);
                    group.GetBlocksOfType(gyros);
                    group.GetBlocksOfType(controllers);
                    group.GetBlocksOfType(panels);
                    group.GetBlocksOfType(allThrusters);
                    group.GetBlocksOfType(railguns);
                    group.GetBlocksOfType(antennas);
                    group.GetBlocksOfType(jumpDrives);
                }
            }
            if (!groupFound)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;

                LCDManager.AddText("No group found, please create a group named \"" + GroupName + "\" and add the required blocks to it, then recompile!");
            }
        }

        private void InitializeShipAim()
        {
            ShipAimConfig aimDetails = new ShipAimConfig();
            aimDetails.program = this;
            aimDetails.OffsetVert = OffsetVert;
            aimDetails.OffsetCoax = OffsetCoax;
            aimDetails.OffsetHoriz = OffsetHoriz;
            aimDetails.rollSensitivityMultiplier = rollSensitivityMultiplier;
            aimDetails.maxAngular = maxAngular;
            aimDetails.TimeStep = TimeStep;
            aimDetails.kP = kP;
            aimDetails.kI = kI;
            aimDetails.kD = kD;
            aimDetails.autonomousRollPower = autonomousRollPower;
            aimDetails.AutonomousMode = AutonomousMode;
            ShipAim = new ShipAim(aimDetails, gyros);
            forwardBackwardPID = new ClampedIntegralPID(autonomouskP, autonomouskI, autonomouskD, TimeStep, -maxAngular, maxAngular);
            if (controllers.Count > 0)
            {
                currentController = controllers[0];
            }
            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = false;
            }
            int spinDirection = rng.Next(0, 2) * 2 - 1;
            autonomousRollPower *= spinDirection;
        }

        private void InitializeTurrets()
        {
            Turrets.TimeStep = TimeStep;
            Turrets.projectileVelocity = TurretVelocity;
        }

        public void InitializeThrusters()
        {
            if (AutonomousMode)
            {

                thrusters = new Thrusters(allThrusters, currentController);
            }
        }

        private void InitializeIGC()
        {
            EnemyLocator = IGC.RegisterBroadcastListener(TaskForceTag + EnemyLocationTag);
            CurrentlyAttackingEnemy = IGC.RegisterBroadcastListener(CurrentlyAttackingEnemyTag);
            CoordinationPositionalData = IGC.RegisterBroadcastListener(CoordinationPositionalDataTag);
        }

        //main loop entrypoint
        public void Main(string argument, UpdateType updateType)
        {

            LCDManager.AddText("Aim Type: " + aimType.ToString());
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                RunCommand(argument);
            }

            if ((updateType & UpdateType.Update1) != 0)
            {
                RunContinuousLogic();

            }
            if ((updateType & UpdateType.Update100) != 0)
            {

            }
            LCDManager.WriteText();
        }
        private void RunCommand(string arg)
        {
            arg = arg.ToLower();
            switch (arg)
            {
                case "toggle ship aim":
                    ToggleShipAim();
                    break;
                case "set velocity":
                    SetProjectileVelocity(arg);
                    break;
                case "set aim type":
                    SetAimType(arg);
                    break;
                case "cycle aim type":
                    CycleAimType();
                    break;
                case "toggle turret aim":
                    ToggleTurretAim();
                    break;
                case "set turret velocity":
                    SetTurretVelocity(arg);
                    break;
                case "unfuck turrets":
                case "retarget turrets":
                    UnfuckTurrets();
                    break;
                default:
                    string echoMessage = "Invalid argument! Valid arguments are:\n";
                    foreach (string argString in Args)
                    {
                        echoMessage += argString + "\n";
                    }
                    Log(echoMessage);
                    break;
            }
        }

        private void ToggleShipAim()
        {
            aim = !aim;
            Log("Aim set to " + aim.ToString());
        }
        private void SetProjectileVelocity(string arg)
        {
            for (int i = 0; i < arg.Length; i++)
            {
                if (char.IsDigit(arg[i]))
                {
                    try
                    {
                        ProjectileVelocity = float.Parse(arg.Substring(i));
                        Log("Set velocity to " + ProjectileVelocity.ToString());
                    }
                    catch
                    {
                        Log("Error parsing velocity, please remove any bad characters\n");
                        //Echo("Error parsing velocity, please remove any bad characters");
                    }
                    break;
                }
            }
        }
        private void SetAimType(string arg)
        {
            string arge = "set aim type";
            for (int i = arge.Length - 1; i < arg.Length; i++)
            {
                foreach (AimType aimType in Enum.GetValues(typeof(AimType)))
                {
                    if (arg.Substring(i) == aimType.ToString().ToLower())
                    {
                        this.aimType = aimType;
                        Log("Aim type set to " + aimType.ToString());
                        return;
                    }
                }
            }
            Log("Couldn't find aimtype!");
        }

        private void CycleAimType()
        {
            int index = (int)aimType;
            index++;
            if (index >= Enum.GetValues(typeof(AimType)).Length)
            {
                index = 0;
            }
            aimType = (AimType)index;
            Log("Aim type set to " + aimType.ToString());
        }

        private void ToggleTurretAim()
        {
            Turrets.overrideTurretAim = !Turrets.overrideTurretAim;
            Log("Turret override set to " + Turrets.overrideTurretAim.ToString());
        }

        private void SetTurretVelocity(string arg)
        {
            //If setvel is parsed with any series of numbers, set the projectile velocity to that number
            //loop through string to find the first number
            for (int i = 0; i < arg.Length; i++)
            {
                if (char.IsDigit(arg[i]))
                {
                    try
                    {
                        Turrets.projectileVelocity = float.Parse(arg.Substring(i));
                        Log("Set turret velocity to " + Turrets.projectileVelocity.ToString());
                    }
                    catch
                    {
                        Log("Error parsing turret velocity, please remove any bad characters");
                        //Echo("Error parsing velocity, please remove any bad characters");
                    }
                    break;
                }
            }
        }

        private void UnfuckTurrets()
        {
            Helpers.UnfuckTurrets(turrets);
            Log("Attempting to unfuck turrets!");
        }

        private void RunContinuousLogic()
        {
            SetCurrentController();
            Targeting.currentController = currentController;
            turretTargets.Clear();
            GetTurretTargets(turrets, ref turretTargets);
            primaryShipAimPos = GetShipTarget(out hasTarget, ref target, turretTargets);

            if (hasTarget)
            {
                if (SendEnemyLocation)
                {
                    //Split transmit message text up into 64 charcter chunks, feed to antennas
                    splitText.Clear();
                    int arrayIndex = -1;
                    for (int i = 0; i < TransmitMessage.Length; i++)
                    {
                        if (i % 50 == 0)
                        {
                            arrayIndex++;
                            splitText.Add("");
                        }
                        splitText[arrayIndex] += TransmitMessage[i];
                    }
                    for (int i = 0; i < antennas.Count; i++)
                    {
                        string text = " ";
                        try { text = splitText[i]; }

                        catch { }

                        IMyRadioAntenna antenna = antennas[i];
                        antenna.Radius = TransmitRadius;
                        antenna.HudText = text;
                    }

                    IGC.SendBroadcastMessage<Vector3D>(TaskForceTag + EnemyLocationTag, primaryShipAimPos, TransmissionDistance.TransmissionDistanceMax);
                }
            }
            else
            {
                foreach (var antenna in antennas)
                {
                    antenna.Radius = PassiveRadius;
                }
                while (EnemyLocator.HasPendingMessage)
                {
                    MyIGCMessage myIGCMessage = EnemyLocator.AcceptMessage();
                    if (!hasTarget && ReceiveEnemyLocation && AutonomousMode && myIGCMessage.Tag == TaskForceTag + EnemyLocationTag)
                    {
                        primaryShipAimPos = (Vector3D)myIGCMessage.Data;
                        hasTarget = true;
                    }
                }
            }
            UpdateGuns();
            UpdateShipAim();
            Turrets.UpdateTurretAim(currentController, turretTargets);
            UpdateShipThrust();
            CoordinateAttack();
            UpdateAntennas();
            UpdateLog();
        }

        void UpdateAntennas()
        {
            if (UseRandomTransmitMessage && hasTarget)
            {
                framesSinceLastTransmitMessage++;
                if (framesSinceLastTransmitMessage > framesPerTransmitMessage)
                {
                    framesSinceLastTransmitMessage = 0;

                    int random = rng.Next(0, TransmitMessages.Length);
                    TransmitMessage = TransmitMessages[random];
                }
            }
        }
        List<long> shipsToCoordinateWith = new List<long>();
        List<MyIGCMessage> shipPositions = new List<MyIGCMessage>();
        private void CoordinateAttack()
        {
            Vector3D position = Me.CubeGrid.GetPosition();
            if (hasTarget)
            {
                //We want to transmit on the "public" channel our current target so that we can find co ordinating ships and avoid crashing into them
                IGC.SendBroadcastMessage(CurrentlyAttackingEnemyTag, target.EntityId, TransmissionDistance.TransmissionDistanceMax);
                IGC.SendBroadcastMessage(CoordinationPositionalDataTag, position);
            }
            shipsToCoordinateWith.Clear();
            shipPositions.Clear();
            while (CurrentlyAttackingEnemy.HasPendingMessage)
            {
                MyIGCMessage message = CurrentlyAttackingEnemy.AcceptMessage();
                if ((long)message.Data == target.EntityId)
                {
                    shipsToCoordinateWith.Add(message.Source);
                }

            }
            while (CoordinationPositionalData.HasPendingMessage)
            {
                MyIGCMessage message = CoordinationPositionalData.AcceptMessage();
                if (shipsToCoordinateWith.Contains(message.Source))
                {
                    shipPositions.Add(message);
                }
            }
            LCDManager.AddText("Coordinating with " + shipsToCoordinateWith.Count + " other ships");

            //Now that we have a list of ships that could potentially collide
            FriendlyAvoidanceVector = Vector3D.Zero;

            foreach (var message in shipPositions)
            {
                if ((Vector3D)message.Data == position)
                {
                    continue;
                }
                Vector3D friendlyPosition = (Vector3D)message.Data;
                Vector3D selfToFriendly = friendlyPosition - position;
                LCDManager.AddText("Distance to friendly: " + selfToFriendly.Length());
                FriendlyAvoidanceVector += Vector3D.Normalize(selfToFriendly) * (float)(1 / selfToFriendly.Length());
            }




        }
        private void UpdateShipAim()
        {

            //check if the forward reference is valid
            if (controllers.Count == 0)
            {

                LCDManager.AddText("No controller found, please include a controller in the group, and recompile!");
                return;
            }
            if (gyros.Count == 0)
            {
                LCDManager.AddText("No gyros found, please include gyros in the group, and recompile!");
                return;
            }
            if (turrets.Count == 0)
            {
                LCDManager.AddText("No turrets found, please include turrets in the group, and recompile!");
                return;
            }
            LCDManager.AddText("Using current controller: " + currentController.CustomName);
            newDetails.averageGunPos = averageGunPos;
            newDetails.target = target;
            newDetails.aimPos = primaryShipAimPos;
            newDetails.hasTarget = hasTarget;
            newDetails.aim = aim;
            newDetails.currentController = currentController;
            newDetails.ProjectileVelocity = ProjectileVelocity;
            LCDManager.AddText("Currently targeted grid: " + newDetails.target.Name);

            ShipAim.CheckForTargets(newDetails);
        }
        void UpdateShipThrust()
        {
            if (AutonomousMode)
            {
                if (hasTarget)
                {
                    currentController.DampenersOverride = false;
                    //get the distance to the target, if less than max projectile range, don't thrust up
                    if (Vector3D.Distance(currentController.GetPosition(), primaryShipAimPos) < ProjectileMaxDist)
                    {
                        thrusters.SetThrustInDirection(1, thrusterDir.Up);
                    }
                    else
                    {
                        thrusters.SetThrustInDirection(0, thrusterDir.Up);
                    }
                    //get distance from target
                    float distance = (float)Vector3D.Distance(currentController.GetPosition(), primaryShipAimPos);

                    float error = distance - autonomousDesiredDistance;
                    forwardBackwardPID.Control(error);
                    thrusters.SetThrustInAxis((float)forwardBackwardPID.Value, thrusterAxis.ForwardBackward);

                    float sideThrustMul = Vector3.Dot(currentController.WorldMatrix.Left, FriendlyAvoidanceVector) * 1000;
                    thrusters.SetThrustInAxis(-sideThrustMul, thrusterAxis.LeftRight);
                    float upThrustMul = Vector3.Dot(currentController.WorldMatrix.Up, FriendlyAvoidanceVector) * 1000;
                    thrusters.SetThrustInDirection(upThrustMul, thrusterDir.Down);

                }
                else
                {
                    currentController.DampenersOverride = true;
                    thrusters.SetThrustInDirection(0, thrusterDir.Up);
                    thrusters.SetThrustInDirection(0, thrusterDir.Down);
                    thrusters.SetThrustInDirection(0, thrusterDir.Left);
                    thrusters.SetThrustInDirection(0, thrusterDir.Right);
                    thrusters.SetThrustInDirection(0, thrusterDir.Forward);
                    thrusters.SetThrustInDirection(0, thrusterDir.Backward);
                }


            }

        }
        List<IMySmallMissileLauncherReload> tempRailguns = new List<IMySmallMissileLauncherReload>();
        void UpdateGuns()
        {
            int activeRailguns = 0;
            averageGunPos = Vector3D.Zero;
            tempRailguns.Clear();
            foreach (IMySmallMissileLauncherReload railgun in railguns)
            {
                tempRailguns.Add(railgun);
            }
            foreach (IMySmallMissileLauncherReload railgun in tempRailguns)
            {
                if (railgun == null)
                {
                    railguns.Remove(railgun);
                    continue;
                }
                if (railgun.Components.Get<MyResourceSinkComponent>().MaxRequiredInputByType(ElectricityId) < (IdlePowerDraw + Epsilon) && railgun.Enabled && railgun.IsFunctional)
                {
                    averageGunPos += railgun.GetPosition();
                    activeRailguns++;
                }


            }
            if (activeRailguns != 0)
            {
                averageGunPos /= activeRailguns;
            }
            LCDManager.AddText("Active railguns: " + activeRailguns.ToString());


            if (AutonomousMode)
            {
                //get a vector from the ship to the target
                Vector3D shipToTarget = (primaryShipAimPos - currentController.GetPosition());

                Vector3D shipToTargetNormal = Vector3D.Normalize(shipToTarget);
                Vector3D forward = currentController.WorldMatrix.Forward;
                bool onTarget = Vector3D.Dot(shipToTargetNormal, forward) > autonomousFireSigma;

                //remove the oldest value
                for (int i = 0; i < autonomousOnTarget.Length - 1; i++)
                {
                    autonomousOnTarget[i] = autonomousOnTarget[i + 1];
                }
                //add the new value
                autonomousOnTarget[autonomousOnTarget.Length - 1] = onTarget;

                //check if all values are true
                onTarget = true;
                foreach (bool value in autonomousOnTarget)
                {
                    if (!value)
                    {
                        onTarget = false;
                    }
                }
                if (onTarget && shipToTarget.Length() < ProjectileMaxDist)
                {
                    LCDManager.AddText("Firing!");
                    tempRailguns.Clear();
                    foreach (IMySmallMissileLauncherReload railgun in railguns)
                    {
                        tempRailguns.Add(railgun);
                    }
                    foreach (IMySmallMissileLauncherReload railgun in tempRailguns)
                    {
                        if (railgun == null)
                        {
                            railguns.Remove(railgun);
                            continue;
                        }
                        railgun.Shoot = true;
                    }
                }
                else
                {
                    tempRailguns.Clear();
                    foreach (IMySmallMissileLauncherReload railgun in railguns)
                    {
                        tempRailguns.Add(railgun);
                    }
                    foreach (IMySmallMissileLauncherReload railgun in tempRailguns)
                    {
                        if (railgun == null)
                        {
                            railguns.Remove(railgun);
                            continue;
                        }
                        railgun.Shoot = false;
                    }
                }
            }

        }



        void SetCurrentController()
        {
            foreach (IMyShipController controller in controllers)
            {
                //do a validity check
                if (controller == null)
                {
                    controllers.Remove(controller);

                }
            }
            foreach (IMyShipController controller in controllers)
            {
                if (controller.IsUnderControl && controller.CanControlShip)
                {
                    currentController = controller;
                    return;
                }
            }
            try
            {
                currentController = controllers[0];
            }
            catch
            {
                //no controllers
            }
        }


        void GetTurretTargets(List<IMyLargeTurretBase> turrets, ref Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> targets)
        {
            //put in separate for loop for efficiency or something
            foreach (IMyLargeTurretBase turret in turrets)
            {
                if (turret == null)
                {
                    Log("removing bad turret " + turret.CustomName);
                    turrets.Remove(turret);
                    continue;
                }
            }

            targets.Clear();

            foreach (IMyLargeTurretBase turret in turrets)
            {
                targets.Add(turret, turret.GetTargetedEntity());
            }
        }

        //Rework to:
        //Check the target of every turret, pick the most targeted target
        //Get the block target of each turret on that target
        //Evaluate the block target positions, and return a position that roughly represents a cluster of blocks
        //Or perhaps just average all the target positions?
        Dictionary<MyDetectedEntityInfo, int> detectionCount = new Dictionary<MyDetectedEntityInfo, int>();
        Vector3D GetShipTarget(out bool result, ref MyDetectedEntityInfo currentTarget, Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> targets)
        {
            result = false;
            //must declare since readonly
            MyDetectedEntityInfo finalTarget = new MyDetectedEntityInfo();


            detectionCount.Clear();
            foreach (KeyValuePair<IMyLargeTurretBase, MyDetectedEntityInfo> pair in targets)
            {

                if (aimType == AimType.CenterOfMass)
                {
                    if (pair.Value.EntityId == currentTarget.EntityId)
                    {
                        currentTarget = pair.Value;
                    }
                }
                IMyLargeTurretBase turret = pair.Key;
                MyDetectedEntityInfo target = pair.Value;
                if (turret.HasTarget)
                {

                    if (target.EntityId == currentTarget.EntityId)
                    {
                        switch (aimType)
                        {
                            case AimType.CenterOfMass:
                                result = true;
                                return target.Position;
                            case AimType.TurretAverage:
                                break;
                            case AimType.RandomTurretTarget:
                                if (target.HitPosition != null)
                                {
                                    result = true;
                                    return (Vector3D)target.HitPosition;
                                }
                                break;
                        }
                        result = true;
                        finalTarget = target;
                    }
                    if (detectionCount.ContainsKey(target))
                    {
                        detectionCount[target]++;
                    }
                    else
                    {
                        detectionCount.Add(target, 1);
                    }

                }
            }
            //If the current target couldn't be found, then find the most detected target

            if (!result)
            {
                int max = 0;
                foreach (KeyValuePair<MyDetectedEntityInfo, int> pair in detectionCount)
                {
                    if (pair.Value > max)
                    {
                        result = true;
                        max = pair.Value;
                        finalTarget = pair.Key;
                    }
                }
            }


            if (result)
            {

                currentTarget = finalTarget;
                //Get the average position of the turret targets
                return AverageTurretTarget(finalTarget, targets);
            }
            else
            {
                return Vector3D.Zero;
            }
        }
        List<Vector3D> aimpoints = new List<Vector3D>();
        Vector3D AverageTurretTarget(MyDetectedEntityInfo target, Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> turrets)
        {
            aimpoints.Clear();
            foreach (KeyValuePair<IMyLargeTurretBase, MyDetectedEntityInfo> pair in turrets)
            {
                IMyLargeTurretBase turret = pair.Key;
                MyDetectedEntityInfo turretTarget = pair.Value;
                if (turretTarget.EntityId == target.EntityId)
                {
                    if (turretTarget.HitPosition != null)
                    {
                        aimpoints.Add((Vector3D)turretTarget.HitPosition);
                    }
                }
            }

            return Helpers.AverageVectorList(aimpoints);

        }



        private void UpdateLog()
        {
            //Clear old lines from the log by counting \n characters
            int lineCount = 0;
            for (int i = 0; i < echoMessage.Length; i++)
            {
                if (echoMessage[i] == '\n')
                {
                    lineCount++;
                }
            }

            if (lineCount > maximumLogLength)
            {
                int index = echoMessage.LastIndexOf('\n');
                echoMessage = echoMessage.Remove(index);
            }

            LCDManager.AddText("\n" + echoMessage);
        }

        private void Log(string toAdd)
        {
            echoMessage = toAdd + "\n" + echoMessage;
        }

    }
    public static class Data
    {
        public static Vector3D prevTargetVelocity = Vector3D.Zero;
    }

    public static class Helpers
    {

        public static float RoundToDecimal(this double value, int decimalPlaces)
        {
            float multiplier = (float)Math.Pow(10, decimalPlaces);
            return (float)Math.Round(value * multiplier) / multiplier;
        }



        public static void UnfuckTurrets(List<IMyLargeTurretBase> turrets)
        {

            foreach (var turret in turrets)
            {
                UnfuckTurret(turret);

            }
        }
        public static void UnfuckTurret(IMyLargeTurretBase turret)
        {
            //store turret values
            bool enabled = turret.Enabled;
            bool targetMeteors = turret.TargetMeteors;
            bool targetMissiles = turret.TargetMissiles;
            bool targetCharacters = turret.TargetCharacters;
            bool targetSmallGrids = turret.TargetSmallGrids;
            bool targetLargeGrids = turret.TargetLargeGrids;
            bool targetStations = turret.TargetStations;
            float range = turret.Range;
            bool enableIdleRotation = turret.EnableIdleRotation;

            turret.ResetTargetingToDefault();

            //restore turret values
            turret.Enabled = enabled;
            turret.TargetMeteors = targetMeteors;
            turret.TargetMissiles = targetMissiles;
            turret.TargetCharacters = targetCharacters;
            turret.TargetSmallGrids = targetSmallGrids;
            turret.TargetLargeGrids = targetLargeGrids;
            turret.TargetStations = targetStations;
            turret.Range = range;
            turret.EnableIdleRotation = enableIdleRotation;
        }
        public static Vector3D AverageVectorList(List<Vector3D> vectors)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (Vector3D vector in vectors)
            {
                x += vector.X;
                y += vector.Y;
                z += vector.Z;
            }
            x /= vectors.Count;
            y /= vectors.Count;
            z /= vectors.Count;
            return new Vector3D(x, y, z);
        }

    }

    public static class LCDManager
    {
        public static List<IMyTextPanel> panels;
        private static string text = "";
        public static MyGridProgram program;

        public static void InitializePanels(List<IMyTextPanel> panels)
        {
            LCDManager.panels = panels;
            foreach (var panel in panels)
            {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
            }
        }
        public static void AddText(string text)
        {
            LCDManager.text += "\n" + text;
        }
        public static void WriteText()
        {
            foreach (var panel in panels)
            {
                try
                {
                    panel.WriteText(text);
                }
                catch
                {
                    panels.Remove(panel);
                }

            }
            program.Echo(text);
            text = "";
        }
    }


    /// <summary>
    /// Discrete time PID controller class.
    /// Last edited: 2022/08/11 - Whiplash141
    /// </summary>
    public class PID
    {
        public double Kp { get; set; } = 0;
        public double Ki { get; set; } = 0;
        public double Kd { get; set; } = 0;
        public double Value { get; private set; }

        double _timeStep = 0;
        double _inverseTimeStep = 0;
        double _errorSum = 0;
        double _lastError = 0;
        bool _firstRun = true;

        public PID(double kp, double ki, double kd, double timeStep)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            _timeStep = timeStep;
            _inverseTimeStep = 1 / _timeStep;
        }

        protected virtual double GetIntegral(double currentError, double errorSum, double timeStep)
        {
            return errorSum + currentError * timeStep;
        }

        public double Control(double error)
        {
            //Compute derivative term
            double errorDerivative = (error - _lastError) * _inverseTimeStep;

            if (_firstRun)
            {
                errorDerivative = 0;
                _firstRun = false;
            }

            //Get error sum
            _errorSum = GetIntegral(error, _errorSum, _timeStep);

            //Store this error as last error
            _lastError = error;

            //Construct output
            Value = Kp * error + Ki * _errorSum + Kd * errorDerivative;
            return Value;
        }

        public double Control(double error, double timeStep)
        {
            if (timeStep != _timeStep)
            {
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
            }
            return Control(error);
        }

        public virtual void Reset()
        {
            _errorSum = 0;
            _lastError = 0;
            _firstRun = true;
        }
    }


    public class DecayingIntegralPID : PID
    {
        public double IntegralDecayRatio { get; set; }

        public DecayingIntegralPID(double kp, double ki, double kd, double timeStep, double decayRatio) : base(kp, ki, kd, timeStep)
        {
            IntegralDecayRatio = decayRatio;
        }

        protected override double GetIntegral(double currentError, double errorSum, double timeStep)
        {
            return errorSum * (1.0 - IntegralDecayRatio) + currentError * timeStep;
        }
    }


    public class ClampedIntegralPID : PID
    {
        public double IntegralUpperBound { get; set; }
        public double IntegralLowerBound { get; set; }

        public ClampedIntegralPID(double kp, double ki, double kd, double timeStep, double lowerBound, double upperBound) : base(kp, ki, kd, timeStep)
        {
            IntegralUpperBound = upperBound;
            IntegralLowerBound = lowerBound;
        }

        protected override double GetIntegral(double currentError, double errorSum, double timeStep)
        {
            errorSum = errorSum + currentError * timeStep;
            return Math.Min(IntegralUpperBound, Math.Max(errorSum, IntegralLowerBound));
        }
    }


    public class BufferedIntegralPID : PID
    {
        readonly Queue<double> _integralBuffer = new Queue<double>();
        public int IntegralBufferSize { get; set; } = 0;

        public BufferedIntegralPID(double kp, double ki, double kd, double timeStep, int bufferSize) : base(kp, ki, kd, timeStep)
        {
            IntegralBufferSize = bufferSize;
        }

        protected override double GetIntegral(double currentError, double errorSum, double timeStep)
        {
            if (_integralBuffer.Count == IntegralBufferSize)
                _integralBuffer.Dequeue();
            _integralBuffer.Enqueue(currentError * timeStep);
            return _integralBuffer.Sum();
        }

        public override void Reset()
        {
            base.Reset();
            _integralBuffer.Clear();
        }
    }

    public struct ShipAimConfig
    {
        public MyGridProgram program;
        public float OffsetVert;
        public float OffsetCoax;
        public float OffsetHoriz;
        public float rollSensitivityMultiplier;
        public float maxAngular;
        public double TimeStep;
        public bool AutonomousMode;
        public double autonomousRollPower;
        public float kP;
        public float kI;
        public float kD;
    }

    public struct ShipAimUpdate
    {
        public MyDetectedEntityInfo target;
        public Vector3D aimPos;
        public Vector3D averageGunPos;
        public bool hasTarget;
        public bool aim;
        public IMyShipController currentController;
        public float ProjectileVelocity;
    }

    public class ShipAim
    {
        //variables to assign once/from config
        ShipAimConfig config;
        public List<IMyGyro> gyros;

        //variables to determine here
        ClampedIntegralPID pitch;
        ClampedIntegralPID yaw;
        ClampedIntegralPID roll;
        public bool active = true;
        public Vector3D previousTargetVelocity;
        //variables to assign continuously
        public MyDetectedEntityInfo target;
        public Vector3D aimPos;
        public bool hasTarget = false;
        public bool aim = true;
        public IMyShipController currentController;
        public float ProjectileVelocity = 0.0f;
        public Vector3D averageGunPos = Vector3D.Zero;
        public MatrixD angularVelocity = MatrixD.Zero;
        public MatrixD previousRotation = MatrixD.Zero;

        public ShipAim(ShipAimConfig config, List<IMyGyro> gyros)
        {
            this.config = config;
            this.gyros = gyros;
            pitch = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.maxAngular, config.maxAngular);
            yaw = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.maxAngular, config.maxAngular);
            roll = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.maxAngular, config.maxAngular);
            active = false;
            previousTargetVelocity = Vector3D.Zero;

        }
        public void CheckForTargets(ShipAimUpdate newDetails)
        {
            averageGunPos = newDetails.averageGunPos;
            target = newDetails.target;
            aimPos = newDetails.aimPos;
            hasTarget = newDetails.hasTarget;
            aim = newDetails.aim;
            currentController = newDetails.currentController;
            ProjectileVelocity = newDetails.ProjectileVelocity;


            if (hasTarget && aim)
            {
                active = true;
                LCDManager.AddText("Fuck shit up, captain!");
                //get reference pos
                MatrixD refOrientation = currentController.WorldMatrix;
                MatrixD ShipMatrix = currentController.CubeGrid.WorldMatrix;
                Vector3D referencePosition = currentController.GetPosition();
                //Offset reference
                referencePosition += refOrientation.Up * config.OffsetVert;
                referencePosition += refOrientation.Forward * config.OffsetCoax;
                referencePosition += refOrientation.Right * config.OffsetHoriz;
                if (averageGunPos != Vector3D.Zero)
                {
                    referencePosition = averageGunPos;
                }
                Data.prevTargetVelocity = new Vector3D(previousTargetVelocity.X, previousTargetVelocity.Y, previousTargetVelocity.Z);
                angularVelocity = previousRotation - target.Orientation;
                Vector3D leadPos = Targeting.GetTargetLeadPosition(aimPos, target.Velocity, angularVelocity, referencePosition, currentController.CubeGrid.LinearVelocity, ProjectileVelocity, config.TimeStep, ref previousTargetVelocity, true);
                previousRotation = target.Orientation;
                double roll = currentController.RollIndicator;
                if (config.AutonomousMode)
                {
                    roll = config.autonomousRollPower;
                    config.program.Echo(roll.ToString());
                }

                Vector3D worldDirection = Vector3D.Normalize(leadPos - referencePosition);
                Rotate(worldDirection, roll);
            }
            else
            {

                LCDManager.AddText("All systems nominal");
                if (active)
                {
                    active = false;
                    foreach (IMyGyro gyro in gyros)
                    {
                        gyro.GyroOverride = false;
                    }
                }

            }

        }
        private void Rotate(Vector3D desiredGlobalFwdNormalized, double roll)
        {
            double gp;
            double gy;
            double gr = roll;
            //Rotate Toward forward
            if (currentController.WorldMatrix.Forward.Dot(desiredGlobalFwdNormalized) < 1)
            {
                var waxis = Vector3D.Cross(currentController.WorldMatrix.Forward, desiredGlobalFwdNormalized);
                Vector3D axis = Vector3D.TransformNormal(waxis, MatrixD.Transpose(currentController.WorldMatrix));
                gp = (float)MathHelper.Clamp(pitch.Control(-axis.X), -config.maxAngular, config.maxAngular);
                gy = (float)MathHelper.Clamp(yaw.Control(-axis.Y), -config.maxAngular, config.maxAngular);
            }
            else
            {
                gp = 0.0;
                gy = 0.0;
            }
            if (Math.Abs(gy) + Math.Abs(gp) > config.maxAngular)
            {
                double adjust = config.maxAngular / (Math.Abs(gy) + Math.Abs(gp));
                gy *= adjust;
                gp *= adjust;
            }
            const double sigma = 0.0009;
            if (Math.Abs(gp) < sigma) gp = 0;
            if (Math.Abs(gy) < sigma) gy = 0;
            //if (Math.Abs(gr) < sigma * 1000) gr = 0;
            ApplyGyroOverride(gp, gy, gr, gyros, currentController.WorldMatrix);
        }

        private void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
        {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var thisGyro in gyroList)
            {
                if (thisGyro == null)
                {
                    LCDManager.AddText("Removing bad gyro!");
                    gyroList.Remove(thisGyro);
                    continue;
                }
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));

                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }
    }

    public static class Targeting
    {
        public static IMyShipController currentController;
        public static MyGridProgram program;
        public static Vector3D GetTargetLeadPosition(Vector3D targetPos, Vector3D targetVel, MatrixD targetAngularVel, Vector3D shooterPos, Vector3D shooterVel, float projectileSpeed, double timeStep, ref Vector3D previousTargetVelocity, bool doEcho)
        {
            Vector3D deltaV = (targetVel - previousTargetVelocity) / timeStep;
            MatrixD deltaW = targetAngularVel * timeStep;  // Calculate change in angular velocity

            Vector3D relativePos = targetPos - shooterPos;
            Vector3D relativeVel = targetVel - shooterVel;
            Vector3D gravity = currentController.GetNaturalGravity();

            double timeToIntercept = CalculateTimeToIntercept(relativePos, relativeVel, deltaV, projectileSpeed);
            Vector3D targetLeadPos = targetPos + ((deltaV + relativeVel) * timeToIntercept) + (gravity * timeToIntercept);

            // Correct for angular velocity
            //targetLeadPos = RotatePosition(targetLeadPos, targetPos, deltaW);

            previousTargetVelocity = targetVel;

            if (doEcho)
            {
                LCDManager.AddText("Target velocity: " + targetVel.Length().RoundToDecimal(2).ToString() + " m/s\nRelative: " + relativeVel.Length().RoundToDecimal(2).ToString() + " m/s");
                LCDManager.AddText("Target acceleration: " + deltaV.Length().RoundToDecimal(2).ToString() + " m/s^2");
                LCDManager.AddText("Target distance: " + relativePos.Length().RoundToDecimal(2).ToString() + " meters");
                LCDManager.AddText($"Time to intercept (@{projectileSpeed}m/s): " + timeToIntercept.RoundToDecimal(2).ToString() + " seconds");

            }

            return targetLeadPos;
        }
        private static Vector3D RotatePosition(Vector3D position, Vector3D center, MatrixD rotation)
        {
            Vector3D rotatedPosition = Vector3D.Transform(position, rotation);
            return center + rotatedPosition;
        }

        private static double CalculateTimeToIntercept(Vector3D relativePos, Vector3D relativeVel, Vector3D targetAcc, float projectileSpeed)
        {
            double a = targetAcc.X * targetAcc.X + targetAcc.Y * targetAcc.Y + targetAcc.Z * targetAcc.Z - projectileSpeed * projectileSpeed;
            double b = 2 * (relativePos.X * targetAcc.X + relativePos.Y * targetAcc.Y + relativePos.Z * targetAcc.Z + relativeVel.X * targetAcc.X + relativeVel.Y * targetAcc.Y + relativeVel.Z * targetAcc.Z);
            double c = relativePos.X * relativePos.X + relativePos.Y * relativePos.Y + relativePos.Z * relativePos.Z;

            double discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                // No real solution, return a default position (e.g., current target position)
                return 0;
            }

            double t1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
            double t2 = (-b - Math.Sqrt(discriminant)) / (2 * a);

            if (t1 < 0 && t2 < 0)
            {
                // Both solutions are negative, return a default position (e.g., current target position)
                return 0;
            }
            else if (t1 < 0)
            {
                // t1 is negative, return t2
                return t2;
            }
            else if (t2 < 0)
            {
                // t2 is negative, return t1
                return t1;
            }
            else
            {
                // Both solutions are valid, return the minimum positive time
                return Math.Min(t1, t2);
            }
        }
    }

    internal class Thrusters
    {
        public List<IMyThrust> upThrust;
        public List<IMyThrust> downThrust;
        public List<IMyThrust> leftThrust;
        public List<IMyThrust> rightThrust;
        public List<IMyThrust> forwardThrust;
        public List<IMyThrust> backwardThrust;

        public Thrusters(List<IMyThrust> allThrust, IMyShipController currentController)
        {
            upThrust = new List<IMyThrust>();
            downThrust = new List<IMyThrust>();
            leftThrust = new List<IMyThrust>();
            rightThrust = new List<IMyThrust>();
            forwardThrust = new List<IMyThrust>();
            backwardThrust = new List<IMyThrust>();

            foreach (var thruster in allThrust)
            {
                //compare the thruster direction to the controller direction

                if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Forward)
                {
                    forwardThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Forward)
                {
                    backwardThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Left)
                {
                    leftThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Left)
                {
                    rightThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Up)
                {
                    upThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Up)
                {
                    downThrust.Add(thruster);
                }
                //there's probably a better solution lol
            }
        }
        private List<IMyThrust> GetThrusterDir(thrusterDir dir)
        {
            if (dir == thrusterDir.Up)
            {
                return upThrust;
            }
            else if (dir == thrusterDir.Down)
            {
                return downThrust;
            }
            else if (dir == thrusterDir.Left)
            {
                return leftThrust;
            }
            else if (dir == thrusterDir.Right)
            {
                return rightThrust;
            }
            else if (dir == thrusterDir.Forward)
            {
                return forwardThrust;
            }
            else if (dir == thrusterDir.Backward)
            {
                return backwardThrust;
            }
            return null;
        }

        public void SetThrustInDirection(float thrust, thrusterDir dir)
        {
            var actualList = GetThrusterDir(dir);
            IMyThrust[] thrusters = new IMyThrust[actualList.Count];
            actualList.CopyTo(thrusters);
            foreach (var thruster in thrusters)
            {
                if (thruster == null)
                {
                    actualList.Remove(thruster);
                    continue;
                }
                thruster.ThrustOverridePercentage = thrust;
            }
        }
        private thrusterDir GetThrusterDirFromAxis(thrusterAxis axis, int sign)
        {
            switch (axis)
            {
                case thrusterAxis.UpDown:
                    if (sign > 0)
                    {
                        return thrusterDir.Up;
                    }
                    else
                    {
                        return thrusterDir.Down;
                    }
                case thrusterAxis.LeftRight:
                    if (sign > 0)
                    {
                        return thrusterDir.Left;
                    }
                    else
                    {
                        return thrusterDir.Right;
                    }
                case thrusterAxis.ForwardBackward:
                    if (sign > 0)
                    {
                        return thrusterDir.Forward;
                    }
                    else
                    {
                        return thrusterDir.Backward;
                    }
            }
            return thrusterDir.Up;
        }
        public void SetThrustInAxis(float thrust, thrusterAxis axis)
        {
            var sign = Math.Sign(thrust);
            thrusterDir mainThrust = GetThrusterDirFromAxis(axis, sign);
            thrusterDir oppositeThrust = GetThrusterDirFromAxis(axis, -sign);
            SetThrustInDirection(Math.Abs(thrust), mainThrust);
            SetThrustInDirection(0, oppositeThrust);
        }
    }

    public enum thrusterDir
    {
        Up,
        Down,
        Left,
        Right,
        Forward,
        Backward

    }


    public enum thrusterAxis
    {
        UpDown,
        LeftRight,
        ForwardBackward
    }

    public static class Turrets
    {
        public static float projectileVelocity = 0;
        public static bool overrideTurretAim = false;
        public static MyGridProgram program;
        public static double TimeStep;
        public static void UpdateTurretAim(IMyShipController currentController, Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> turretTargets)
        {
            if (!overrideTurretAim)
            {
                return;
            }
            Vector3D shipVelocity = currentController.CubeGrid.LinearVelocity;

            foreach (var pair in turretTargets)
            {
                IMyLargeTurretBase turret = pair.Key;
                if (turret.IsShooting && turret.IsAimed)
                {
                    Vector3D aimPoint = GetAimPoint(pair, shipVelocity);

                    SetTurretAim(turret, aimPoint);
                    //Helpers.UnfuckTurret(turret);
                }
            }
        }

        private static Vector3D GetAimPoint(KeyValuePair<IMyLargeTurretBase, MyDetectedEntityInfo> pair, Vector3D shipVelocity)
        {
            IMyLargeTurretBase turret = pair.Key;
            MyDetectedEntityInfo target = pair.Value;
            if (target.HitPosition != null)
            {
                Vector3D tempVelocity = new Vector3D(Data.prevTargetVelocity.X, Data.prevTargetVelocity.Y, Data.prevTargetVelocity.Z);
                return Targeting.GetTargetLeadPosition((Vector3D)target.HitPosition, target.Velocity, MatrixD.Zero, turret.GetPosition(), shipVelocity, projectileVelocity, TimeStep, ref tempVelocity, false);
            }
            return Vector3D.Zero;
        }

        private static void SetTurretAim(IMyLargeTurretBase turret, Vector3D aimPoint)
        {
            // Get the turret's local coordinate system vectors
            Vector3D turretForward = turret.WorldMatrix.Forward;
            Vector3D turretUp = turret.WorldMatrix.Up;
            Vector3D turretRight = turret.WorldMatrix.Right;

            // Calculate the direction vector from the turret's position to the aim point
            Vector3D targetDir = Vector3D.Normalize(aimPoint - turret.WorldMatrix.Translation);

            // Calculate the azimuth angle (yaw) in radians
            double azimuth = Math.Atan2(Vector3D.Dot(targetDir, turretRight), Vector3D.Dot(targetDir, turretForward));

            // Calculate the elevation angle (pitch) in radians
            double elevation = Math.Asin(Vector3D.Dot(targetDir, turretUp));

            if (double.IsNaN(azimuth) || double.IsNaN(elevation))
            {
                return;
            }
            turret.SetManualAzimuthAndElevation(-(float)azimuth, (float)elevation);
            turret.SyncAzimuth();
            turret.SyncElevation();
        }
    }

    public static class VectorMath
    {
        /// <summary>
        ///  Normalizes a vector only if it is non-zero and non-unit
        /// </summary>
        public static Vector3D SafeNormalize(Vector3D a)
        {
            if (Vector3D.IsZero(a))
                return Vector3D.Zero;

            if (Vector3D.IsUnit(ref a))
                return a;

            return Vector3D.Normalize(a);
        }

        /// <summary>
        /// Reflects vector a over vector b with an optional rejection factor
        /// </summary>
        public static Vector3D Reflection(Vector3D a, Vector3D b, double rejectionFactor = 1) //reflect a over b
        {
            Vector3D proj = Projection(a, b);
            Vector3D rej = a - proj;
            return proj - rej * rejectionFactor;
        }

        /// <summary>
        /// Rejects vector a on vector b
        /// </summary>
        public static Vector3D Rejection(Vector3D a, Vector3D b) //reject a on b
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a - a.Dot(b) / b.LengthSquared() * b;
        }

        /// <summary>
        /// Projects vector a onto vector b
        /// </summary>
        public static Vector3D Projection(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return Vector3D.Zero;

            if (Vector3D.IsUnit(ref b))
                return a.Dot(b) * b;

            return a.Dot(b) / b.LengthSquared() * b;
        }

        /// <summary>
        /// Scalar projection of a onto b
        /// </summary>
        public static double ScalarProjection(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return 0;

            if (Vector3D.IsUnit(ref b))
                return a.Dot(b);

            return a.Dot(b) / b.Length();
        }

        /// <summary>
        /// Computes angle between 2 vectors in radians.
        /// </summary>
        public static double AngleBetween(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return 0;
            else
                return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }

        /// <summary>
        /// Computes cosine of the angle between 2 vectors.
        /// </summary>
        public static double CosBetween(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return 0;
            else
                return MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1);
        }

        /// <summary>
        /// Returns if the normalized dot product between two vectors is greater than the tolerance.
        /// This is helpful for determining if two vectors are "more parallel" than the tolerance.
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <param name="tolerance">Cosine of maximum angle</param>
        /// <returns></returns>
        public static bool IsDotProductWithinTolerance(Vector3D a, Vector3D b, double tolerance)
        {
            double dot = Vector3D.Dot(a, b);
            double num = a.LengthSquared() * b.LengthSquared() * tolerance * Math.Abs(tolerance);
            return Math.Abs(dot) * dot > num;
        }
    }
}
