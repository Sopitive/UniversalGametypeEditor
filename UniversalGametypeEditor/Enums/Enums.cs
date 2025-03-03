﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor.Enums
{
    public class Enums
    {

        public enum TriggerTypeEnum
        {
            Do = 0b000,
            Player = 0b001,
            RandomPlayer = 0b010,
            Team = 0b011,
            Object = 0b100,
            Labeled = 0b101,
            Unlabelled1 = 0b110,
            Unlabelled2 = 0b111
        }

        public enum TriggerAttributeEnum
        {
            OnTick = 0b000,
            OnCall = 0b001,
            OnInit = 0b010,
            OnLocalInit = 0b011,
            OnHostMigration = 0b100,
            OnObjectDeath = 0b101,
            OnLocal = 0b110,
            OnPregame = 0b111
        }


        public enum NameIndex
        {
            None = 0b00000000,
            MpBoneyardAIdleStart = 0b00000001,
            MpBoneyardAFlyIn = 0b00000010,
            MpBoneyardAIdleMid = 0b00000011,
            MpBoneyardAFlyOut = 0b00000100,
            MpBoneyardBFlyIn = 0b00000101,
            MpBoneyardBIdleMid = 0b00000110,
            MpBoneyardBFlyOut = 0b00000111,
            MpBoneyardBIdleStart = 0b00001000,
            MpBoneyardALeave1 = 0b00001001,
            MpBoneyardBLeave1 = 0b00001010,
            MpBoneyardBPickup = 0b00001011,
            MpBoneyardBIdlePickup = 0b00001100,
            MpBoneyardA = 0b00001101,
            MpBoneyardB = 0b00001110,
            Default = 0b00001111,
            Carter = 0b00010000,
            Jun = 0b00010001,
            Female = 0b00010010,
            Male = 0b00010011,
            Emile = 0b00010100,
            PlayerSkull = 0b00010101,
            Kat = 0b00010110,
            Minor = 0b00010111,
            Officer = 0b00011000,
            Ultra = 0b00011001,
            Space = 0b00011010,
            SpecOps = 0b00011011,
            General = 0b00011100,
            Zealot = 0b00011101,
            Mp = 0b00011110,
            Jetpack = 0b00011111,
            Gauss = 0b00100000,
            Troop = 0b00100001,
            Rocket = 0b00100010,
            Fr = 0b00100011,
            Pl = 0b00100100,
            Spire35Fp = 0b00100101,
            MpSpireFp = 0b00100110,
            MinusOne = 0b11111111
        }

        public enum PlayerTypeRefEnum
        {
            Player = 0b00,
            PlayerPlayer = 0b01,
            ObjectPlayer = 0b10,
            TeamPlayer = 0b11
        }

        public enum ObjectType
        {
            Spartan = 0x000,
            Elite = 0x001,
            Monitor = 0x002,
            Flag = 0x003,
            Bomb = 0x004,
            Ball = 0x005,
            Area = 0x006,
            Stand = 0x007,
            Destination = 0x008,
            FragGrenade = 0x009,
            PlasmaGrenade = 0x00A,
            SpikeGrenade = 0x00B,
            FirebombGrenade = 0x00C,
            Dmr = 0x00D,
            AssaultRifle = 0x00E,
            PlasmaPistol = 0x00F,
            SpikeRifle = 0x010,
            Smg = 0x011,
            NeedleRifle = 0x012,
            PlasmaRepeater = 0x013,
            EnergySword = 0x014,
            Magnum = 0x015,
            Needler = 0x016,
            PlasmaRifle = 0x017,
            RocketLauncher = 0x018,
            Shotgun = 0x019,
            SniperRifle = 0x01A,
            BruteShot = 0x01B,
            BeamRifle = 0x01C,
            SpartanLaser = 0x01D,
            GravityHammer = 0x01E,
            Mauler = 0x01F,
            Flamethrower = 0x020,
            MissilePod = 0x021,
            Warthog = 0x022,
            Ghost = 0x023,
            Scorpion = 0x024,
            Wraith = 0x025,
            Banshee = 0x026,
            Mongoose = 0x027,
            Chopper = 0x028,
            Prowler = 0x029,
            Hornet = 0x02A,
            Stingray = 0x02B,
            HeavyWraith = 0x02C,
            Falcon = 0x02D,
            Sabre = 0x02E,
            SprintEquipment = 0x02F,
            JetPackEquipment = 0x030,
            ArmorLockEquipment = 0x031,
            PowerFistEquipment = 0x032,
            ActiveCamoEquipment = 0x033,
            AmmoPackEquipment = 0x034,
            SensorPackEquipment = 0x035,
            Revenant = 0x036,
            Pickup = 0x037,
            PrototypeCoveySniper = 0x038,
            TerritoryStatic = 0x039,
            CtfFlagReturnArea = 0x03A,
            CtfFlagSpawnPoint = 0x03B,
            RespawnZone = 0x03C,
            InvasionEliteBuy = 0x03D,
            InvasionEliteDrop = 0x03E,
            InvasionSlayer = 0x03F,
            InvasionSpartanBuy = 0x040,
            InvasionSpartanDrop = 0x041,
            InvasionSpawnController = 0x042,
            OddballBallSpawnPoint = 0x043,
            PlasmaLauncher = 0x044,
            FusionCoil = 0x045,
            UnscShieldGenerator = 0x046,
            CovShieldGenerator = 0x047,
            InitialSpawnPoint = 0x048,
            InvasionVehicleReq = 0x049,
            VehicleReqFloor = 0x04A,
            WallSwitch = 0x04B,
            HealthStation = 0x04C,
            ReqUnscLaser = 0x04D,
            ReqUnscDmr = 0x04E,
            ReqUnscRocket = 0x04F,
            ReqUnscShotgun = 0x050,
            ReqUnscSniper = 0x051,
            ReqCovyLauncher = 0x052,
            ReqCovyNeedler = 0x053,
            ReqCovySniper = 0x054,
            ReqCovySword = 0x055,
            ShockLoadout = 0x056,
            SpecialistLoadout = 0x057,
            AssassinLoadout = 0x058,
            InfiltratorLoadout = 0x059,
            WarriorLoadout = 0x05A,
            CombatantLoadout = 0x05B,
            EngineerLoadout = 0x05C,
            InfantryLoadout = 0x05D,
            OperatorLoadout = 0x05E,
            ReconLoadout = 0x05F,
            ScoutLoadout = 0x060,
            SeekerLoadout = 0x061,
            AirborneLoadout = 0x062,
            RangerLoadout = 0x063,
            ReqBuyBanshee = 0x064,
            ReqBuyFalcon = 0x065,
            ReqBuyGhost = 0x066,
            ReqBuyMongoose = 0x067,
            ReqBuyRevenant = 0x068,
            ReqBuyScorpion = 0x069,
            ReqBuyWarthog = 0x06A,
            ReqBuyWraith = 0x06B,
            Fireteam1RespawnZone = 0x06C,
            Fireteam2RespawnZone = 0x06D,
            Fireteam3RespawnZone = 0x06E,
            Fireteam4RespawnZone = 0x06F,
            Semi = 0x070,
            SoccerBall = 0x071,
            GolfBall = 0x072,
            GolfBallBlue = 0x073,
            GolfBallRed = 0x074,
            GolfClub = 0x075,
            GolfCup = 0x076,
            GolfTee = 0x077,
            Dice = 0x078,
            SpaceCrate = 0x079,
            EradicatorLoadout = 0x07A,
            SaboteurLoadout = 0x07B,
            GrenadierLoadout = 0x07C,
            MarksmanLoadout = 0x07D,
            Flare = 0x07E,
            GlowStick = 0x07F,
            EliteShot = 0x080,
            GrenadeLauncher = 0x081,
            PhantomApproach = 0x082,
            HologramEquipment = 0x083,
            EvadeEquipment = 0x084,
            UnscDataCore = 0x085,
            DangerZone = 0x086,
            TeleporterSender = 0x087,
            TeleporterReceiver = 0x088,
            Teleporter2Way = 0x089,
            DataCoreBeam = 0x08A,
            PhantomOverwatch = 0x08B,
            Longsword = 0x08C,
            InvisibleCubeOfDerek = 0x08D,
            PhantomScenery = 0x08E,
            PelicanScenery = 0x08F,
            Phantom = 0x090,
            Pelican = 0x091,
            ArmoryShelf = 0x092,
            CovResupplyCapsule = 0x093,
            CovyDropPod = 0x094,
            InvisibleMarker = 0x095,
            WeakRespawnZone = 0x096,
            WeakAntiRespawnZone = 0x097,
            PhantomDevice = 0x098,
            ResupplyCapsule = 0x099,
            ResupplyCapsuleOpen = 0x09A,
            WeaponBox = 0x09B,
            TechConsoleStationary = 0x09C,
            TechConsoleWall = 0x09D,
            MpCinematicCamera = 0x09E,
            InvisCovResupplyCapsule = 0x09F,
            CovPowerModule = 0x0A0,
            FlakCannon = 0x0A1,
            DropzoneBoundary = 0x0A2,
            ShieldDoorSmall = 0x0A3,
            ShieldDoorMedium = 0x0A4,
            ShieldDoorLarge = 0x0A5,
            DropShieldEquipment = 0x0A6,
            Machinegun = 0x0A7,
            MachinegunTurret = 0x0A8,
            PlasmaTurretWeapon = 0x0A9,
            MountedPlasmaTurret = 0x0AA,
            ShadeTurret = 0x0AB,
            CargoTruck = 0x0AC,
            CartElectric = 0x0AD,
            Forklift = 0x0AE,
            MilitaryTruck = 0x0AF,
            OniVan = 0x0B0,
            WarthogGunner = 0x0B1,
            WarthogGaussTurret = 0x0B2,
            WarthogRocketTurret = 0x0B3,
            ScorpionInfantryGunner = 0x0B4,
            FalconGrenadierLeft = 0x0B5,
            FalconGrenadierRight = 0x0B6,
            WraithInfantryTurret = 0x0B7,
            LandMine = 0x0B8,
            TargetLaser = 0x0B9,
            FfKillZone = 0x0BA,
            FfPlat1x1Flat = 0x0BB,
            ShadeAntiAir = 0x0BC,
            ShadeFlak = 0x0BD,
            ShadePlasma = 0x0BE,
            Killball = 0x0BF,
            FfLightRed = 0x0C0,
            FfLightBlue = 0x0C1,
            FfLightGreen = 0x0C2,
            FfLightOrange = 0x0C3,
            FfLightPurple = 0x0C4,
            FfLightYellow = 0x0C5,
            FfLightWhite = 0x0C6,
            FfLightFlashRed = 0x0C7,
            FfLightFlashYellow = 0x0C8,
            FxColorblind = 0x0C9,
            FxGloomy = 0x0CA,
            FxJuicy = 0x0CB,
            FxNova = 0x0CC,
            FxOldeTimey = 0x0CD,
            FxPenAndInk = 0x0CE,
            FxDusk = 0x0CF,
            FxGoldenHour = 0x0D0,
            FxEerie = 0x0D1,
            FfGrid = 0x0D2,
            InvisibleCubeOfAlarming1 = 0x0D3,
            InvisibleCubeOfAlarming2 = 0x0D4,
            SpawningSafe = 0x0D5,
            SpawningSafeSoft = 0x0D6,
            SpawningKill = 0x0D7,
            SpawningKillSoft = 0x0D8,
            PackageCabinet = 0x0D9,
            CovPowermoduleStand = 0x0DA,
            DlcCovenantBomb = 0x0DB,
            DlcInvasionHeavyShield = 0x0DC,
            DlcInvasionBombDoor = 0x0DD,
            LanAMf = 0x104
        }

        public enum ObjectTypeRefEnum
        {
            ObjectRef = 0b000,
            PlayerObject = 0b001,
            ObjectObject = 0b010,
            TeamObject = 0b011,
            PlayerBiped = 0b100,
            PlayerPlayerBiped = 0b101,
            ObjectPlayerBiped = 0b110,
            TeamPlayerBiped = 0b111
        }

        public enum PlayerRefEnum
        {
            NoPlayer = 0b00000,
            Player0 = 0b00001,
            Player1 = 0b00010,
            Player2 = 0b00011,
            Player3 = 0b00100,
            Player4 = 0b00101,
            Player5 = 0b00110,
            Player6 = 0b00111,
            Player7 = 0b01000,
            Player8 = 0b01001,
            Player9 = 0b01010,
            Player10 = 0b01011,
            Player11 = 0b01100,
            Player12 = 0b01101,
            Player13 = 0b01110,
            Player14 = 0b01111,
            Player15 = 0b10000,
            GlobalPlayer0 = 0b10001,
            GlobalPlayer1 = 0b10010,
            GlobalPlayer2 = 0b10011,
            GlobalPlayer3 = 0b10100,
            GlobalPlayer4 = 0b10101,
            GlobalPlayer5 = 0b10110,
            GlobalPlayer6 = 0b10111,
            GlobalPlayer7 = 0b11000,
            CurrentPlayer = 0b11001,
            HudPlayer = 0b11010,
            HudTargetPlayer = 0b11011,
            ObjectKiller = 0b11100,
            Unlabelled1 = 0b11101,
            Unlabelled2 = 0b11110,
            Unlabelled3 = 0b11111
        }

        public enum ObjectRef
        {
            NoObject = 0b00000,
            GlobalObject0 = 0b00001,
            GlobalObject1 = 0b00010,
            GlobalObject2 = 0b00011,
            GlobalObject3 = 0b00100,
            GlobalObject4 = 0b00101,
            GlobalObject5 = 0b00110,
            GlobalObject6 = 0b00111,
            GlobalObject7 = 0b01000,
            GlobalObject8 = 0b01001,
            GlobalObject9 = 0b01010,
            GlobalObject10 = 0b01011,
            GlobalObject11 = 0b01100,
            GlobalObject12 = 0b01101,
            GlobalObject13 = 0b01110,
            GlobalObject14 = 0b01111,
            GlobalObject15 = 0b10000,
            CurrentObject = 0b10001,
            TargetObject = 0b10010,
            KilledObject = 0b10011,
            KillerObject = 0b10100,
            Unlabelled1 = 0b10101,
            Unlabelled2 = 0b10110,
            Unlabelled3 = 0b10111,
            Unlabelled4 = 0b11000,
            Unlabelled5 = 0b11001,
            Unlabelled6 = 0b11010,
            Unlabelled7 = 0b11011,
            Unlabelled8 = 0b11100,
            Unlabelled9 = 0b11101,
            Unlabelled10 = 0b11110,
            Unlabelled11 = 0b11111
        }

        public enum TeamRef
        {
            NoTeam = 0b00000,
            Team0 = 0b00001,
            Team1 = 0b00010,
            Team2 = 0b00011,
            Team3 = 0b00100,
            Team4 = 0b00101,
            Team5 = 0b00110,
            Team6 = 0b00111,
            Team7 = 0b01000,
            NeutralTeam = 0b01001,
            GlobalTeam0 = 0b01010,
            GlobalTeam1 = 0b01011,
            GlobalTeam2 = 0b01100,
            GlobalTeam3 = 0b01101,
            GlobalTeam4 = 0b01110,
            GlobalTeam5 = 0b01111,
            GlobalTeam6 = 0b10000,
            GlobalTeam7 = 0b10001,
            CurrentTeam = 0b10010,
            HudPlayerTeam = 0b10011,
            HudTargetTeam = 0b10100,
            UnkTeam21 = 0b10101,
            UnkTeam22 = 0b10110,
            Unlabelled1 = 0b10111,
            Unlabelled2 = 0b11000,
            Unlabelled3 = 0b11001,
            Unlabelled4 = 0b11010,
            Unlabelled5 = 0b11011,
            Unlabelled6 = 0b11100,
            Unlabelled7 = 0b11101,
            Unlabelled8 = 0b11110,
            Unlabelled9 = 0b11111
        }


        public enum NumericTypeRefEnum
        {
            Int16 = 0,
            PlayerNumber = 1,
            ObjectNumber = 2,
            TeamNumber = 3,
            GlobalNumber = 4,
            ScriptOption = 5,
            ObjectSpawnSeq = 6,
            TeamScore = 7,
            PlayerScore = 8,
            PlayerMoney = 9,
            PlayerRating = 10,
            PlayerStat = 11,
            TeamStat = 12,
            CurrentRound = 13,
            SymmetricMode = 14,
            SymmetricModeWritable = 15,
            ScoreToWin = 16,
            FireteamsEnabled = 17,
            TeamsEnabled = 18,
            RoundTimeLimit = 19,
            RoundLimit = 20,
            PerfectionEnabled = 21,
            EarlyVictoryWinCount = 22,
            SuddenDeathTimeLimit = 23,
            GracePeriodTimeLimit = 24,
            PlayerLives = 25,
            TeamLives = 26,
            RespawnTime = 27,
            SuicideRespawnPenalty = 28,
            BetrayalRespawnPenalty = 29,
            RespawnGrowthTime = 30,
            InitialLoadoutSelectionTime = 31,
            RespawnTraitsDuration = 32,
            FriendlyFireEnabled = 33,
            BetrayalBootingEnabled = 34,
            EnemyVoiceEnabled = 35,
            OpenChannelVoiceEnabled = 36,
            DeadPlayerVoiceEnabled = 37,
            GrenadesOnMap = 38,
            IndestructibleVehiclesEnabled = 39,
            RedTraitsDuration = 40,
            BlueTraitsDuration = 41,
            YellowTraitsDuration = 42,
            ObjectDeathDamageType = 43,
            Unlabelled_44 = 44,
            Unlabelled_45 = 45,
            Unlabelled_46 = 46,
            Unlabelled_47 = 47,
            Unlabelled_48 = 48,
            Unlabelled_49 = 49,
            Unlabelled_50 = 50,
            Unlabelled_51 = 51,
            Unlabelled_52 = 52,
            Unlabelled_53 = 53,
            Unlabelled_54 = 54,
            Unlabelled_55 = 55,
            Unlabelled_56 = 56,
            Unlabelled_57 = 57,
            Unlabelled_58 = 58,
            Unlabelled_59 = 59,
            Unlabelled_60 = 60,
            Unlabelled_61 = 61,
            Unlabelled_62 = 62,
            Unlabelled_63 = 63
        }

        public enum NumericRef
        {
            GlobalNum0 = 0,
            GlobalNum1 = 1,
            GlobalNum2 = 2,
            GlobalNum3 = 3,
            GlobalNum4 = 4,
            GlobalNum5 = 5,
            GlobalNum6 = 6,
            GlobalNum7 = 7,
            GlobalNum8 = 8,
            GlobalNum9 = 9,
            GlobalNum10 = 10,
            GlobalNum11 = 11,
            GlobalNum12 = 12,
            GlobalNum13 = 13,
            GlobalNum14 = 14,
            GlobalNum15 = 15
        }

    }
}
