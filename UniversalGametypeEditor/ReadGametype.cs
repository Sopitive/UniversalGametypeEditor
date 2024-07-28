using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.Xml;
using UniversalGametypeEditor.Properties;
using System.Collections.ObjectModel;
using static UniversalGametypeEditor.ReadGametype.ModeSettings;
using Newtonsoft.Json;
using static UniversalGametypeEditor.ReadGametype;
using System.Windows.Documents;
using System.Windows.Markup.Localizer;

namespace UniversalGametypeEditor
{
    
    public class ReadGametype
    {

        public class Gametype
        {
            public string? FileHeader;
            public string? GametypeHeader;
            public string? ModeSettings;
            public string? SpawnSettings;
            public string? GameSettings;
            public string? PowerupTraits;
            public string? TeamSettings;
            public string? loadoutCluster;
            public string? scriptedPlayerTraits;
            public string? scriptOptions;
            public string? Strings;
            public string? Game;
            public Map Map;
            public PlayerRatings playerratings;
        }

       

        public class FileHeader
        {
            public string? mpvr;
            public int megaloversion;
            public int Unknown0x2F8;
            public int Unknown0x2FA;
            public string? UnknownHash0x2FC;
            public string? Blank0x310;
            public int Fileusedsize;
            public int Unknown0x318;
            public int Unknown0x319;
            public int Unknown0x31D;
            public int FileLength;
            public int VariantType;
            public int Unknown0x31C;

            
        }


        public class GametypeHeader
        {
            public string ID0x48;
            public string ID0x50;
            public string ID0x58;
            public string Blank0x60;
            public string UnknownFlags;
            public int Unknown_1;
            public int Unknown0x1;
            public int Blank04;
            public int TimeStampUint;
            public string XUID;
            public string Gamertag;
            public string Blank041bit;
            public int EditTimeStampUint;
            public string EditXUID;
            public string EditGamertag;
            public int UnknownFlag1;
            public string Title;
            public string Description;
            public int GameIcon;
        }

        public enum VariantTypeEnum
        {
            Campaign,
            Forge,
            Multiplayer,
            Firefight
        }

        public enum IconEnum
        {
            Flag,
            Slayer,
            Skull,
            KOTH,
            Juggernaut,
            Territories,
            Assault,
            Infection,
            VIP,
            Invasion,
            EliteandSkull,
            Stockpile,
            ActionSack,
            Lightning,
            Rocket,
            Griffball,
            Biohazard,
            Headhunter,
            SpikySwirl,
            Wings,
            Swirl,
            Castle,
            Plus,
            Shield,
            Arrow,
            Infinity,
            Forerunner,
            EightBall,
            UNSC,
            Unknown,
            Diamond
        }

        public enum H2AH4IconEnum
        {
            None = 0,
            Speaker = 1,
            X = 2,
            LightningBolt = 3,
            Bullseye = 4,
            Diamond = 5,
            Bomb = 6,
            Flag = 7,
            Skull = 8,
            Crown = 9,
            VIP = 10,
            Locked = 11,
            A = 12,
            B = 13,
            C = 14,
            D = 15,
            E = 16,
            F = 17,
            G = 18,
            H = 19,
            I = 20,
            Ordnance = 21,
            Activate = 22,
            Parachute = 23,
            Ammo = 24,
            Uplink = 25,
            Shield = 26,
            Inwards = 27,
            Teardrop = 28,
            Unlabelled1 = 29,
            Unlabelled2 = 30,
            Unlabelled3 = 31
        }


    public enum WeaponSetEnum
        {
            
            Human,
            Covenant,
            NoSnipers,
            Rockets,
            NoPower,
            Juggernaut,
            SlayerPro,
            Rifles,
            MidRange,
            LongRange,
            Snipers,
            Melee,
            EnergySwords,
            GravityHammers,
            MassDestruction,
            Random = 253,
            MapDefault = 254,
            None = 255,

        }

        public enum VehicleSetEnum
        {
            Mongooses,
            Warthogs,
            NoAircraft,
            AircraftOnly,
            NoTanks,
            TanksOnly,
            NoLightGround,
            LightGroundOnly,
            NoCovenant,
            CovenantOnly,
            NoHuman,
            HumanOnly,
            NoVehicles,
            AllVehicles,
            MapDefault = 254,
        }

        public enum DamageResistanceEnum
        {
            Unchanged,
            TenPercent,
            FiftyPercent,
            NinetyPercent,
            OneHundredPercent,
            OneHundredTenPercent,
            OneHundredFiftyPercent,
            TwoHundredPercent,
            ThreeHundredPercent,
            FiveHundredPercent,
            OneThousandPercent,
            TwoThousandPercent,
            Invlulnerable

        }

        public enum HealthMultiplierEnum
        {
            Uncanged,
            ZeroPercent,
            TenPercent,
            OnehundredFiftyPercent,
            TwoHundredPercent,
            ThreeHundredPercent,
            FourHundredPercent,
            

        }


        public enum RegenEnum
        {
            Unchanged,
            NegativeTwentyFivePercent,
            NegativeTenPercent,
            NegativeFivePercent,
            ZeroPercent,
            FiftyPercent,
            NinetyPercent,
            OneHunredPercent,
            OneHundredTenPercent,
            TwoHunredPercent,
        }

        public enum ShieldMultiplyerEnum
        {
            Unchanged,
            ZeroPercent,
            OneHundredPercent,
            TwoHundredPercent,
            ThreeHundredPercent,
            FourHundredPercent,
        }

        public enum ToggleEnum
        {
            Unchanged,
            Disabled,
            Enabled
        }

        public enum VampirismEnum
        {
            Uncanged,
            None,
            TenPercent,
            TwentyFivePercent,
            FiftyPercent,
            OneHundredPercent
        }

        public enum DamageEnum
        {
            Unchanged,
            ZeroPercent,
            TwentyFivePercent,
            FiftyPercent,
            SeventyFivePercent,
            NinetyPercent,
            OneHundredPercent,
            OneHundredTenPercent,
            OneHundredTwentyFivePercent,
            OneHundredFiftyPercent,
            TwoHundredPercent,
            ThreeHundredPercent,
        }

        public enum WeaponEnum
        {
            DMR = 0,
            AssaultRifle = 1,
            PlasmaPistol = 2,
            Spiker = 3,
            EnergySword = 4,
            Magnum = 5,
            Needler = 6,
            PlasmaRifle = 7,
            RocketLauncher = 8,
            Shotgun = 9,
            SniperRifle = 10,
            SpartanLaser = 11,
            GravityHammer = 12,
            PlasmaRepeater = 13,
            NeedleRifle = 14,
            FocusRifle = 15,
            PlasmaLauncher = 16,
            ConcussionRifle = 17,
            GrenadeLauncher = 18,
            GolfClub = 19,
            FuelRodCannon = 20,
            DetachedMachineGun = 21,
            DetachedPlasmaTurret = 22,
            TargetLocator = 23,
            Unchanged = 253,
            Random = 252,
            MapDefault = 254,
            None = 255
        }

        public static Dictionary<VariantTypeEnum, string> VariantTypeStrings = new Dictionary<VariantTypeEnum, string>
        {
            { VariantTypeEnum.Campaign, "Campaign" },
            { VariantTypeEnum.Forge, "Forge" },
            { VariantTypeEnum.Multiplayer, "Multiplayer" },
            { VariantTypeEnum.Firefight, "Firefight" }
        };

        public static Dictionary<IconEnum, string> IconStrings = new Dictionary<IconEnum, string>
        {
            { IconEnum.Flag, "Flag" },
            { IconEnum.Slayer, "Slayer" },
            { IconEnum.Skull, "Skull" },
            { IconEnum.KOTH, "KOTH" },
            { IconEnum.Juggernaut, "Juggernaut" },
            { IconEnum.Territories, "Territories" },
            { IconEnum.Assault, "Assault" },
            { IconEnum.Infection, "Infection" },
            { IconEnum.VIP, "VIP" },
            { IconEnum.Invasion, "Invasion" },
            { IconEnum.EliteandSkull, "Elite and Skull" },
            { IconEnum.Stockpile, "Stockpile" },
            { IconEnum.ActionSack, "Action Sack" },
            { IconEnum.Lightning, "Lightning" },
            { IconEnum.Rocket, "Rocket" },
            { IconEnum.Griffball, "Griffball" },
            { IconEnum.Biohazard, "Biohazard" },
            { IconEnum.Headhunter, "Headhunter" },
            { IconEnum.SpikySwirl, "Spiky Swirl" },
            { IconEnum.Wings, "Wings" },
            { IconEnum.Swirl, "Swirl" },
            { IconEnum.Castle, "Castle" },
            { IconEnum.Plus, "Plus" },
            { IconEnum.Shield, "Shield" },
            { IconEnum.Arrow, "Arrow" },
            { IconEnum.Infinity, "Infinity" },
            { IconEnum.Forerunner, "Forerunner" },
            { IconEnum.EightBall, "Eight Ball" },
            { IconEnum.UNSC, "UNSC" },
            { IconEnum.Unknown, "Unknown" },
            { IconEnum.Diamond, "Diamond" }
        };

        public static Dictionary<WeaponEnum, string> WeaponStrings = new Dictionary<WeaponEnum, string>
        {
            { WeaponEnum.DMR, "DMR" },
            { WeaponEnum.AssaultRifle, "Assault Rifle" },
            { WeaponEnum.PlasmaPistol, "Plasma Pistol" },
            { WeaponEnum.Spiker, "Spiker" },
            { WeaponEnum.EnergySword, "Energy Sword" },
            { WeaponEnum.Magnum, "Magnum" },
            { WeaponEnum.Needler, "Needler" },
            { WeaponEnum.PlasmaRifle, "Plasma Rifle" },
            { WeaponEnum.RocketLauncher, "Rocket Launcher" },
            { WeaponEnum.Shotgun, "Shotgun" },
            { WeaponEnum.SniperRifle, "Sniper Rifle" },
            { WeaponEnum.SpartanLaser, "Spartan Laser" },
            { WeaponEnum.GravityHammer, "Gravity Hammer" },
            { WeaponEnum.PlasmaRepeater, "Plasma Repeater" },
            { WeaponEnum.NeedleRifle, "Needle Rifle" },
            { WeaponEnum.FocusRifle, "Focus Rifle" },
            { WeaponEnum.PlasmaLauncher, "Plasma Launcher" },
            { WeaponEnum.ConcussionRifle, "Concussion Rifle" },
            { WeaponEnum.GrenadeLauncher, "Grenade Launcher" },
            { WeaponEnum.GolfClub, "Golf Club" },
            { WeaponEnum.FuelRodCannon, "Fuel Rod Cannon" },
            { WeaponEnum.DetachedMachineGun, "Detached Machine Gun" },
            { WeaponEnum.DetachedPlasmaTurret, "Detached Plasma Turret" },
            { WeaponEnum.TargetLocator, "Target Locator" },
            { WeaponEnum.Random, "Random"},
            { WeaponEnum.Unchanged, "Unchanged" },
            { WeaponEnum.MapDefault, "Map Default" },
            { WeaponEnum.None, "None" }
        };

        public static Dictionary<DamageEnum, string> DamageStrings = new Dictionary<DamageEnum, string>
        {
            { DamageEnum.Unchanged, "Unchanged" },
            { DamageEnum.ZeroPercent, "0%" },
            { DamageEnum.TwentyFivePercent, "25%" },
            { DamageEnum.FiftyPercent, "50%" },
            { DamageEnum.SeventyFivePercent, "75%" },
            { DamageEnum.NinetyPercent, "90%" },
            { DamageEnum.OneHundredPercent, "100%" },
            { DamageEnum.OneHundredTenPercent, "110%" },
            { DamageEnum.OneHundredTwentyFivePercent, "125%" },
            { DamageEnum.OneHundredFiftyPercent, "150%" },
            { DamageEnum.TwoHundredPercent, "200%" },
            { DamageEnum.ThreeHundredPercent, "300%" }
        };

        public static Dictionary<VampirismEnum, string> VamparismStrings = new Dictionary<VampirismEnum, string>
        {
            { VampirismEnum.Uncanged, "Unchanged" },
            { VampirismEnum.None, "None" },
            { VampirismEnum.TenPercent, "10%" },
            { VampirismEnum.TwentyFivePercent, "25%" },
            { VampirismEnum.FiftyPercent, "50%" },
            { VampirismEnum.OneHundredPercent, "100%" }
        };

        public static Dictionary<ToggleEnum, string> ToggleEnumStrings = new Dictionary<ToggleEnum, string>
        {
            { ToggleEnum.Unchanged, "Unchanged" },
            { ToggleEnum.Enabled, "Enabled" },
            { ToggleEnum.Disabled, "Disabled" }
        };


        public static Dictionary<DamageResistanceEnum, string> DamageResistanceStrings = new Dictionary<DamageResistanceEnum, string>
        {
            { DamageResistanceEnum.Unchanged, "Unchanged" },
            { DamageResistanceEnum.TenPercent, "10%" },
            { DamageResistanceEnum.FiftyPercent, "50%" },
            { DamageResistanceEnum.NinetyPercent, "90%" },
            { DamageResistanceEnum.OneHundredPercent, "100%" },
            { DamageResistanceEnum.OneHundredTenPercent, "110%" },
            { DamageResistanceEnum.OneHundredFiftyPercent, "150%" },
            { DamageResistanceEnum.TwoHundredPercent, "200%" },
            { DamageResistanceEnum.ThreeHundredPercent, "300%" },
            { DamageResistanceEnum.FiveHundredPercent, "500%" },
            { DamageResistanceEnum.OneThousandPercent, "1000%" },
            { DamageResistanceEnum.TwoThousandPercent, "2000%" },
            { DamageResistanceEnum.Invlulnerable, "Invulnerable" }
        };

        public static Dictionary<HealthMultiplierEnum, string> HealthMultiplierStrings = new Dictionary<HealthMultiplierEnum, string>
        {
            { HealthMultiplierEnum.Uncanged, "Unchanged" },
            { HealthMultiplierEnum.ZeroPercent, "0%" },
            { HealthMultiplierEnum.TenPercent, "10%" },
            { HealthMultiplierEnum.OnehundredFiftyPercent, "150%" },
            { HealthMultiplierEnum.TwoHundredPercent, "200%" },
            { HealthMultiplierEnum.ThreeHundredPercent, "300%" },
            { HealthMultiplierEnum.FourHundredPercent, "400%" }
        };

        public static Dictionary<RegenEnum, string> RegenStrings = new Dictionary<RegenEnum, string>
        {
            { RegenEnum.Unchanged, "Unchanged" },
            { RegenEnum.NegativeTwentyFivePercent, "-25%" },
            { RegenEnum.NegativeTenPercent, "-10%" },
            { RegenEnum.NegativeFivePercent, "-5%" },
            { RegenEnum.ZeroPercent, "0%" },
            { RegenEnum.FiftyPercent, "50%" },
            { RegenEnum.NinetyPercent, "90%" },
            { RegenEnum.OneHunredPercent, "100%" },
            { RegenEnum.OneHundredTenPercent, "110%" },
            { RegenEnum.TwoHunredPercent, "200%" }
        };


        public static Dictionary<ShieldMultiplyerEnum, string> ShieldMultiplyerStrings = new Dictionary<ShieldMultiplyerEnum, string>
        {
            { ShieldMultiplyerEnum.Unchanged, "Unchanged" },
            { ShieldMultiplyerEnum.ZeroPercent, "0%" },
            { ShieldMultiplyerEnum.OneHundredPercent, "100%" },
            { ShieldMultiplyerEnum.TwoHundredPercent, "200%" },
            { ShieldMultiplyerEnum.ThreeHundredPercent, "300%" },
            { ShieldMultiplyerEnum.FourHundredPercent, "400%" }
        };


        public class ModeSettings
        {
            public int UnknownFlag2;
            public int Teamsenabled;
            public int Resetmaponnewroundunused;
            public int Resetplayersonnewroundunused;
            public int Perfectionmedalenabled;
            public int RoundTimeLimit;
            public int NumberOfRounds;
            public int RoundsToWin;
            public int? SuddenDeathTime;
            public ReachSettings Reach { get; set; } // Fields for Reach

            public class ReachSettings
            {
                public int GracePeriod;
            }


            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4

            public class H2AH4Settings
            {
                public int? Bit4;
                public int? Bit5;
                public int? MoshDifficulty;
                public int? ProtoMode;
                public int? Unknown4;
                public int? ClassColorOverride;
                public int? InheritRespawnTime;
                public int? Unknown42;
                public int? KillCamEnabled;
                public int? PointsSystemEnabled;
                public int? FinalKillCamEnabled;
                public int? Unknown2;
            }
        }


        public class SpawnSettings
        {
            

            public int LivesPerround;
            public int TeamLivesPerround;
            
            public int RespawnTime;
            public int Suicidepenalty;
            public int Betrayalpenalty;
            public int RespawnTimegrowth;
            public int LoadoutCamTime;
            public int Respawntraitsduration;

            public PlayerTraits RespawnPlayerTraits { get; set; }

            public ReachSettings Reach { get; set; } // Fields for Reach

            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4

            public class H2AH4Settings
            {
                public int? MinRespawnTime;
            }

            public class ReachSettings
            {
                public int? RespawnOnKills;
                public int? respawnatlocationunused;
                public int? respawnwithteammateunused;
                public int? RespawnSyncwithteam;
            }

            
        }


        public class PlayerTraits
        {

            public int Healthmultiplyer;
            public int Healthregenrate;
            

            public int DamageResistance;
            public int ShieldRegenrate;

            public int ShieldMultiplyer;
            
            public int Overshieldregenrate;
            public int HeadshotImmunity;
            public int shieldvampirism;
            public int Assasinationimmunity;
            public int invincible;
            public int WeaponDamagemultiplier;
            public int MeleeDamagemultiplier;
            public int Primaryweapon;
            public int Secondaryweapon;
            public int Grenades;
            public int Infiniteammo;
            public int Grenaderegen;
            public int WeaponPickup;
            public int AbilityUsage;
            public int Abilitiesdropondeath;
            public int InfiniteAbility;
            public int ArmorAbility;
            public int MovementSpeed;
            public int Playergravity;
            public int VehicleUse;
            public int Unknown;
            public int JumpHeight;
            public int JumpOverride;
            public int Camo;
            public int Visiblewaypoint;
            public int VisibleName;
            public int Aura;
            public int Forcedcolor;
            public int Motiontrackermode;
            public int MotiontrackerRange;
            public int DirectionalDamageindicator;

            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4

            //H2A
            public class H2AH4Settings
            {
                public int explosivedamageresistance;
                public int falldamage;
                public int fasttrackarmor;
                public int powerupcancelation;
                public int shieldstunduration;
                public int wheelmanvehicleemp;
                public int wheelmanvehiclerechargetime;
                public int wheelmanvehiclestuntime;
                public int GrenadeRechargeFrag;
                public int GrenadeRechargePlasma;
                public int GrenadeRechargeSpike;
                public int HeroEquipmentEnergyUse;
                public int HeroEquipmentEnergyRechargeDelay;
                public int HeroEquipmentEnergyRechargeRate;
                public int HeroEquipmentInitialEnergy;
                public int EquipmentEnergyUse;
                public int EquipmentEnergyRechargeDelay;
                public int EquipmentEnergyRechargeRate;
                public int EquipmentInitialEnergy;
                public int SwitchSpeed;
                public int ReloadSpeed;
                public int OrdinancePoints;
                public int ExplosiveAOE;
                public int GunnerArmor;
                public int StabilityArmor;
                public int DropReconWarning;
                public int DropReconDistance;
                public int AssassinationSpeed;
                public int UsageSansAutoTurret;
                public int AmmoPack;
                public int Grenadier;
                public int DropGrenadeDeath;
                public int OrdinanceMarkerVisibility;
                public int ScavengeGrenades;
                public int Firepower;
                public int OrdinanceReroll;
                public int OrdinanceDisabled;
                public int TacticalPackage;
                public int Nemesis;
                public int Aura2;
                public int Unknown2;
                public int Unknown3;
                public int Unknown4;
                public int AutoMomentum;
                public int BattleAwareness;
                public int DeathEffect;
                public int DoubleJump;
                public int ForcedPrimaryColor;
                public int ForcedSecondaryColor;
                public int Gravity;
                public int LoopingEffect;
                public int MotionTrackerEnabled;
                public int MotionTrackerUsageZoomed;
                public int Name;
                public int NemesisDuration;
                public int OverridePlayerModel;
                public int OverridePrimaryColor;
                public int OverrideSecondaryColor;
                public int PrimaryBlue;
                public int PrimaryGreen;
                public int PrimaryRed;
                public int SecondaryBlue;
                public int SecondaryGreen;
                public int SecondaryRed;
                public int Scale;
                public int ShieldHud;
                public int Speed;
                public int Sprint;
                public int Stealthy;
                public int SupportPackage;
                public int ThreadView;
                public int TurnSpeed;
                public int Vaulting;
                public int VisionMode;
            }
            



        }


        public class TeamOptions
        {
            public int TertiarycolorOverride;
            public int SecondarycolorOverride;
            public int PrimarycolorOverride;
            public int TeamEnabled;
            public int Unknown;
            public int Unknown2;
            public LanguageStrings Teamstring;
            public int InitialDesignator;
            public int Elitespecies;
            public int PrimaryColor;
            public int SecondaryColor;
            public int TertiaryColor;
            public int FireteamCount;

            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4

            public class H2AH4Settings
            {
                //H2A
                public int Unknown1;
                public int EmblemOverride;
                public int EmblemForeground;
                public int EmblemBackground;
                public int EmblemBackgroundColor;
                public int EmblemPrimaryColor;
                public int EmblemSecondaryColor;
                public int EmblemUnknown;
                public int InterfaceColor;
                public int TextColor;
            }

        }

        

        public class GameSettings
        {
            public int EnableObservers;
            public int Teamchanging;
            public int FriendlyFire;
            public int BetrayalBooting;
            public int ProximityVoice;
            public int Dontrestrictteamvoicechat;
            public int allowdeadplayerstotalk;
            public int Indestructiblevehicles;
            public int turretsonmap;
            public int powerupsonmap;
            public int abilitiesonmap;
            public int shortcutsonmap;
            public int grenadesonmap;
            public PlayerTraits BasePlayerTraits { get; set;}
            public int WeaponSet;
            public int VehicleSet;
            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4
            public class H2AH4Settings
            {
                public int EquipmentSet;
                public int Unknown1;
                public int Unknown2;
                public int Unknown3;
                public string Unknown4;
            }
            


            

        }


        public class PowerupTraits
        {
            public PlayerTraits? RedPlayerTraits { get; set; }
            public PlayerTraits? BluePlayerTraits { get; set; }
            public PlayerTraits? YellowPlayerTraits { get; set; }
            public int? RedPowerupDuration;
            public int? BluePowerupDuration;
            public int? YellowPowerupDuration;


            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4


            public class H2AH4Settings
            {
                //H2A
                public PlayerTraits? DamageTraits { get; set; }
                public int? DamageTraitsDuration;
                public PlayerTraits? DamageTraitsRuntime { get; set; }
                public int? DamageTraitsRuntimeDuration;
                public PlayerTraits? SpeedTraits { get; set; }
                public int? SpeedTraitsDuration;
                public PlayerTraits? SpeedTraitsRuntime { get; set; }
                public int? SpeedTraitsRuntimeDuration;
                public PlayerTraits? OverShieldTraits { get; set; }
                public int? OverShieldTraitsDuration;
                public PlayerTraits? OverShieldTraitsRuntime { get; set; }
                public int? OverShieldTraitsRuntimeDuration;
                public PlayerTraits? CustomTraits { get; set; }
                public int? CustomTraitsDuration;
                public PlayerTraits? CustomTraitsRuntime { get; set; }
                public int? CustomTraitsRuntimeDuration;
            }
        }

        
        public class Ordinance
        {
            public int Ordinances2bit;
            public int InitialOrdinance;
            public int RandomOrdinance;
            public int PersonalOrdinance;
            public int OrdinancesEnabled;
            public int Ordinance8Bit;
            public int OrdResupplyTimeMin;
            public int OrdResupplyTimeMax;
            public int Ordinance16Bit1;
            public string InitialDropsetName;
            public int Ordinance16Bit2;
            public int Ordinance16Bit3;
            public string RandomDropsetName;
            public string PersonalDropsetName;
            public string OrdinanceSubstitutionsName;
            public int CustomizePersonalOrdinance;
            public OrdnanceWeights OrdnanceRight;
            public OrdnanceWeights OrdnanceLeft;
            public OrdnanceWeights OrdnanceDown;
            public OrdnanceWeights OrdnanceUnknown;
            public string OrdnancePointsRequirement;
            public string OrdnanceIncreaseMultiplier;
        }

        public class OrdnanceWeights
        {
            public string PosibilityName1;
            public int PossibilityWeight1;
            public string PosibilityName2;
            public int PossibilityWeight2;
            public string PosibilityName3;
            public int PossibilityWeight3;
            public string PosibilityName4;
            public int PossibilityWeight4;
            public string PosibilityName5;
            public int PossibilityWeight5;
            public string PosibilityName6;
            public int PossibilityWeight6;
            public string PosibilityName7;
            public int PossibilityWeight7;
            public string PosibilityName8;
            public int PossibilityWeight8;
        }


        public class TeamSettings
        {
            public int TeamScoringMethod;
            public int PlayerSpecies;
            public int DesignatorSwitchtype;
            public TeamOptions Team1Options;
            public TeamOptions Team2Options;
            public TeamOptions Team3Options;
            public TeamOptions Team4Options;
            public TeamOptions Team5Options;
            public TeamOptions Team6Options;
            public TeamOptions Team7Options;
            public TeamOptions Team8Options;

            //H2A
            public int? Unknown1;
            public int? Unknown2;
            public int? Unknown3;
            public int? Unknown4;
        }

        public class LanguageStrings
        {
            public string English;
            public string Japanese;
            public string German;
            public string French;
            public string Spanish;
            public string LatinAmericanSpanish;
            public string Italian;
            public string Korean;
            public string ChineseTraditional;
            public string ChineseSimplified;
            public string Portuguese;
            public string Polish;

            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4
            //H2A
            public class H2AH4Settings
            {
                public string Russian;
                public string Finnish;
                public string Norwegian;
                public string Dutch;
                public string Danish;
            }

           
        }


        public class LoadoutOptions
        {
            public int LoadoutVisibleingame;
            public int LoadoutName;
            public int LoadoutNameIndex;
            public int NameIndex;
            public int PrimaryWeapon;
            public int SecondaryWeapon;
            public int Armorability;
            public int Grenades;

            //H2A
            public int Unknown;
            public int TacticalPackage;
            public int SupportUpgrade;
        }


        public class LoadoutCluster
        {
            public int? MapLoadoutsEnabled;
            public int EliteLoadoutsEnabled;
            public int SpartanLoadoutsEnabled;
            public int? CustomLoadoutsEnabled;
            public LoadoutOptions Loadout1;
            public LoadoutOptions Loadout2;
            public LoadoutOptions Loadout3;
            public LoadoutOptions Loadout4;
            public LoadoutOptions Loadout5;
            public LoadoutOptions Loadout6;

            public LoadoutOptions? Loadout7;
            public LoadoutOptions? Loadout8;
            public LoadoutOptions? Loadout9;
            public LoadoutOptions? Loadout10;
            public LoadoutOptions? Loadout11;
            public LoadoutOptions? Loadout12;
            public LoadoutOptions? Loadout13;
            public LoadoutOptions? Loadout14;
            public LoadoutOptions? Loadout15;
            public LoadoutOptions? Loadout16;
            public LoadoutOptions? Loadout17;
            public LoadoutOptions? Loadout18;
            public LoadoutOptions? Loadout19;
            public LoadoutOptions? Loadout20;
            public LoadoutOptions? Loadout21;
            public LoadoutOptions? Loadout22;
            public LoadoutOptions? Loadout23;
            public LoadoutOptions? Loadout24;
            public LoadoutOptions? Loadout25;
            public LoadoutOptions? Loadout26;
            public LoadoutOptions? Loadout27;
            public LoadoutOptions? Loadout28;
            public LoadoutOptions? Loadout29;
            public LoadoutOptions? Loadout30;
            public LoadoutOptions? Loadout31;
        }


        public class ScriptedPlayerTraits
        {
            public int count;
            public int String1;
            public int String2;
            public PlayerTraits? PlayerTraits { get; set; }

            public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4

            public class H2AH4Settings
            {
                public int hidden;
            }
            
        }


        public class ScriptOptions
        {
            public int count;
            public int String1;
            public int String2;
            public int ScriptOption;
            public int ChildIndex;
            public int ScriptOptionChild;
            public int Value;
            public int Unknown;
            public int range1;
            public int range2;
            public int range3;
            public int range4;
            public int ActualChildIndex;
        }


        public class Strings
        {
            public LanguageStrings Stringtable;
            public int StringNameIndex;
            public LanguageStrings metanameStrings;
            public LanguageStrings metadescStrings;
            public LanguageStrings metagroupStrings;
            public LanguageStrings metaintroStrings;
        }


        public class Game
        {
            public int ActualGameicon;
            public int ActualGamecategory;
        }


        public class PlayerRatings
        {
            public int ratingscale;
            public int kilweight;
            public int assistweight;
            public int betrayalweight;
            public int deathweight;
            public int normalizebymaxkills;
            public int baserating;
            public int range;
            public int lossscalar;
            public int customstat0;
            public int customstat1;
            public int customstat2;
            public int customstat3;
            public int expansion0;
            public int expansion1;
            public int showplayerratings;
        }

        public class Conditions
        {
            public List<string> conditions;
            public int  ConditionCount;
            public int ConditionType;
            public string NOT;
            public int ORSequence;
            public long ConditionOffset;
            public int Vartype1;
            public int Vartype2;
            public string SpecificType;
            public int RefType;
            public string SpecificType2;
            public int RefType2;
            public int Oper;
        }

        public class Actions
        {
            public int ActionCount;
            public int ActionType;
            public string Parameter1;
            public string Parameter2;
            public string Parameter3;
            public int Parameter4;
            public string Parameter5;
            public string Parameter6;
            public string Parameter7;
            public int Parameter8;
            public int Parameter9;
            public int Parameter10;
            public string Parameter11;
        }

 

        public List<string> WaypointPriority = new List<string>
        {
            "Low",
            "Normal",
            "High",
            "Blink"
        };

        public List<string> WaypointIcon = new List<string>
        {
            "none",
            "Speaker",
            "X",
            "Lightning bolt",
            "Bullseye",
            "Diamond",
            "Bomb",
            "Flag",
            "Skull",
            "Crown",
            "VIP",
            "Locked",
            "A",
            "B",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "Ordnance",
            "Activate",
            "Parachute",
            "Ammo",
            "Uplink",
            "Shield",
            "Inwards",
            "Teardrop",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled"
        };

        public List<string> PlayerSet = new List<string>
        {
            "no_one",
            "everyone",
            "allies",
            "enemies",
            "player",
            "default"
        };

        public List<string> Names = new List<string> {

            "none",
            "mp_boneyard_a_idle_start",
            "mp_boneyard_a_fly_in",
            "mp_boneyard_a_idle_mid",
            "mp_boneyard_a_fly_out",
            "mp_boneyard_b_fly_in",
            "mp_boneyard_b_idle_mid",
            "mp_boneyard_b_fly_out",
            "mp_boneyard_b_idle_start",
            "mp_boneyard_a_leave1",
            "mp_boneyard_b_leave1",
            "mp_boneyard_b_pickup",
            "mp_boneyard_b_idle_pickup",
            "mp_boneyard_a",
            "mp_boneyard_b",
            "default",
            "carter",
            "jun",
            "female",
            "male",
            "emile",
            "player_skull",
            "kat",
            "minor",
            "officer",
            "ultra",
            "space",
            "spec_ops",
            "general",
            "zealot",
            "mp",
            "jetpack",
            "gauss",
            "troop",
            "rocket",
            "megalo_test_function",
            "build_base_team_0_tier_1",
            "build_base_team_0_tier_2",
            "build_base_team_0_tier_3",
            "build_base_team_0_tier_4",
            "build_base_team_0_tier_5",
            "build_base_team_1_tier_1",
            "build_base_team_1_tier_2",
            "build_base_team_1_tier_3",
            "build_base_team_1_tier_4",
            "build_base_team_1_tier_5",
            "navpoint_megalo_one_row",
            "navpoint_megalo_two_row",
            "navpoint_megalo_three_row",
            "navpoint_regicide",
            "navpoint_ctf_flag_carrier",
            "navpoint_ctf_flag_normal",
            "navpoint_ctf_flag_away",
            "navpoint_ctf_flag_return",
            "navpoint_megalo_general",
            "navpoint_megalo_interact",
            "navpoint_megalo_general_blink",
            "carried_king",
            "carried_flag",
            "carried_skull_yours",
            "carried_skulll_others",
            "navpoint_megalo_message",
            "navpoint_megalo_message_power",
            "navpoint_megalo_message_hostile",
            "you_have_the_hill",
            "you_have_the_hill_clean",
            "navpoint_dom_last_stander",
            "spartan_back",
            "left_hand_wield_spartan",
            "secondary_weapon",
            "weapon_stow_anchor",
            "vehicle_ctf",
            "flood_ur_zombie_banner",
            "flood_ur_alpha_banner",
            "flood_in_barricade_banner",
            "flood_in_safehaven_banner",
            "flood_sole_survivor_banner",
            "navpoint_megalo_dominion",
            "navpoint_megalo_dominion_blink",
            "navpoint_megalo_extraction",
            "navpoint_megalo_extraction_blink",
            "navpoint_megalo_flood",
            "navpoint_megalo_flood_blink",
            "navpoint_megalo_oddball",
            "navpoint_megalo_oddball_blink",
            "blam_banner_you_have_bacon",
            "dom_banner_last_stand_team",
            "dom_banner_last_stand",
            "dom_banner_solo_survivor",
            "oddball_banner_you_have_ball",
            "territories",
            "territories_dominion",
            "territories_extraction",
            "navpoint_oddball_player",
            "navpoint_flood_player",
            "koth_banner_hill_contested",
            "koth_banner_hill_contested_clean",
            "navpoint_oddball_player_blink",
            "navpoint_strong_base",
            "navpoint_strong_base_fort",
            "navpoint_strong_base_defend",
            "navpoint_strong_base_fort_defend",
            "navpoint_strong_base_countdown",
            "oddball_banner_penalty",
            "navpoint_strong_base_fort_countdown",
            "position",
            "navpoint_oddball_trailing",
            "alternate",
            "navpoint_oddball_player_friendly_only",
            "navpoint_megalo_extraction_beacon",
            "navpoint_megalo_extraction_beacon_blink",
            "navpoint_megalo_extraction_se_beacon",
            "navpoint_megalo_extraction_se_beacon_blink",
            "navpoint_megalo_extraction_player_beacon",
            "navpoint_megalo_extraction_incoming",
            "navpoint_megalo_extraction_red",
            "left_grip",
            "infected",
            "navpoint_territory",
            "return_the_flag_banner",
            "navpoint_oddball_thrown",
            "navpoint_oddball_goal",
            "navpoint_assault_bomb_at_spawn",
            "navpoint_assault_bomb_at_spawn_neutral",
            "navpoint_assault_bomb_carried",
            "navpoint_assault_bomb_dropped",
            "navpoint_assault_bomb_arming",
            "navpoint_assault_bomb_planted",
            "navpoint_assault_bomb_defusing",
            "navpoint_assault_goal",
            "navpoint_assault_goal_mybomb_home_enemybomb_carried",
            "assault_banner_bombcarrier",
            "assault_banner_armedbombcarrier",
            "navpoint_ctf_flag_taken",
            "navpoint_objective_a",
            "navpoint_objective_b",
            "navpoint_objective_c",
            "navpoint_objective_d",
            "navpoint_objective_e",
            "navpoint_timer",
            "objective_banner_moveto_a",
            "objective_banner_moveto_b",
            "objective_banner_moveto_c",
            "objective_banner_moveto_d",
            "objective_banner_moveto_e",
            "objective_banner_capturetheflag",
            "objective_banner_returntheflag",
            "objective_banner_destroythetarget",
            "objective_banner_defendthetarget",
            "objective_banner_attackthetarget",
            "oddball_low_ball_banner_obj",
            "navpoint_oddball_pass",
            "navpoint_oddball_lowball_carrier",
            "navpoint_assault_goal_hi_pri",
            "navpoint_assault_bomb_planted_no_defuse",
            "navpoint_assault_bomb_carried_armed",
            "juggernaut_ur_juggernaut_banner",
            "navpoint_territory_locked",
            "navpoint_territory_asymmetric_defenders",
            "navpoint_territory_asymmetric_rest",
            "navpoint_territory_capturing",
            "navpoint_territory_enemy_capturing",
            "navpoint_territory_reverting",
            "navpoint_territory_enemy_reverting",
            "navpoint_territory_contested",
            "navpoint_megalo_juggernaut",
            "navpoint_race_checkpoint",
            "navpoint_race_checkpoint_down",
            "navpoint_race_checkpoint_down_enemy",
            "navpoint_race_ride",
            "navpoint_race_player",
            "navpoint_race_landmine",
            "race_driver_banner",
            "race_gunner_banner",
            "navpoint_koth",
            "navpoint_koth_incoming",
            "navpoint_koth_contested",
            "navpoint_ctf_flag_recover",
            "infection_epidemic_banner_striker",
            "infection_epidemic_banner_jumper",
            "infection_epidemic_banner_heavy",
            "infection_banner_cadre",
            "infection_flight_zone_banner",
            "navpoint_infection_flight_payload",
            "navpoint_infection_flight_payload_carried",
            "navpoint_infection_flight_payload_powering",
            "navpoint_infection_flight_cp",
            "navpoint_infection_flight_cp_next",
            "navpoint_infection_haven",
            "chief",
            "terr_banner_owned",
            "terr_banner_contested",
            "terr_banner_reverting",
            "terr_banner_capturing",

        };

 //       <Var name = "TeamRef" type="Enum" bits="5" >
	//	<Var ID = "00000" name="NoTeam" />
	//	<Var ID = "00001" name="Team0" />
	//	<Var ID = "00010" name="Team1" />
	//	<Var ID = "00011" name="Team2" />
	//	<Var ID = "00100" name="Team3" />
	//	<Var ID = "00101" name="Team4" />
	//	<Var ID = "00110" name="Team5" />
	//	<Var ID = "00111" name="Team6" />
	//	<Var ID = "01000" name="Team7" />
	//	<Var ID = "01001" name="NeutralTeam" />
	//	<Var ID = "01010" name="GlobalTeam[0]" />
	//	<Var ID = "01011" name="GlobalTeam[1]" />
	//	<Var ID = "01100" name="GlobalTeam[2]" />
	//	<Var ID = "01101" name="GlobalTeam[3]" />
	//	<Var ID = "01110" name="GlobalTeam[4]" />
	//	<Var ID = "01111" name="GlobalTeam[5]" />
	//	<Var ID = "10000" name="GlobalTeam[6]" />
	//	<Var ID = "10001" name="GlobalTeam[7]" />
	//	<Var ID = "10010" name="ScratchTeam[0]" />
	//	<Var ID = "10011" name="ScratchTeam[1]" />
	//	<Var ID = "10100" name="ScratchTeam[2]" />
	//	<Var ID = "10101" name="ScratchTeam[3]" />
	//	<Var ID = "10110" name="ScratchTeam[4]" />
	//	<Var ID = "10111" name="ScratchTeam[5]" />
	//	<Var ID = "11000" name="ScratchTeam[6]" />
	//	<Var ID = "11001" name="ScratchTeam[7]" />
	//	<Var ID = "11010" name="CurrentTeam" />
	//	<Var ID = "11011" name="HudPlayer.Team" />
	//	<Var ID = "11100" name="HudTargetTeam·" />
	//	<Var ID = "11101" name="UnkTeam29·" />
	//	<Var ID = "11110" name="UnkTeam30·" />
	//	<Var ID = "11111" name="Unlabelled·" />
	//</Var>

        public List<string> TeamTypeRef = new List<string>
        {
            "NoTeam",
            "Team0",
            "Team1",
            "Team2",
            "Team3",
            "Team4",
            "Team5",
            "Team6",
            "Team7",
            "NeutralTeam",
            "GlobalTeam[0]",
            "GlobalTeam[1]",
            "GlobalTeam[2]",
            "GlobalTeam[3]",
            "GlobalTeam[4]",
            "GlobalTeam[5]",
            "GlobalTeam[6]",
            "GlobalTeam[7]",
            "ScratchTeam[0]",
            "ScratchTeam[1]",
            "ScratchTeam[2]",
            "ScratchTeam[3]",
            "ScratchTeam[4]",
            "ScratchTeam[5]",
            "ScratchTeam[6]",
            "ScratchTeam[7]",
            "CurrentTeam",
            "HudPlayer.Team",
            "HudTargetTeam",
            "UnkTeam29",
            "UnkTeam30",
            "Unlabelled"
        };

 //       <Var name = "ScratchNumericRef" type="Enum" bits="4" >
	//	<Var ID = "0000" name="ScratchNum[0]" />
	//	<Var ID = "0001" name="ScratchNum[1]" />
	//	<Var ID = "0010" name="ScratchNum[2]" />
	//	<Var ID = "0011" name="ScratchNum[3]" />
	//	<Var ID = "0100" name="ScratchNum[4]" />
	//	<Var ID = "0101" name="ScratchNum[5]" />
	//	<Var ID = "0110" name="ScratchNum[6]" />
	//	<Var ID = "0111" name="ScratchNum[7]" />
	//	<Var ID = "1000" name="ScratchNum[8]" />
	//	<Var ID = "1001" name="ScratchNum[9]" />
	//	<Var ID = "1010" name="Unlabelled·" />
	//	<Var ID = "1011" name="Unlabelled·" />
	//	<Var ID = "1100" name="Unlabelled·" />
	//	<Var ID = "1101" name="Unlabelled·" />
	//	<Var ID = "1110" name="Unlabelled·" />
	//	<Var ID = "1111" name="Unlabelled·" />
	//</Var>

        public List<string> ScratchNumbers = new List<string>
        {
            "ScratchNum[0]",
            "ScratchNum[1]",
            "ScratchNum[2]",
            "ScratchNum[3]",
            "ScratchNum[4]",
            "ScratchNum[5]",
            "ScratchNum[6]",
            "ScratchNum[7]",
            "ScratchNum[8]",
            "ScratchNum[9]",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled"
        };

        public List<string> ObjectTypeRef = new List<string>
        {
            "none",
            "GlobalObject[0]",
            "GlobalObject[1]",
            "GlobalObject[2]",
            "GlobalObject[3]",
            "GlobalObject[4]",
            "GlobalObject[5]",
            "GlobalObject[6]",
            "GlobalObject[7]",
            "GlobalObject[8]",
            "GlobalObject[9]",
            "GlobalObject[10]",
            "GlobalObject[11]",
            "GlobalObject[12]",
            "GlobalObject[13]",
            "GlobalObject[14]",
            "GlobalObject[15]",
            "GlobalObject[16]",
            "GlobalObject[17]",
            "ScratchObject[0]",
            "ScratchObject[1]",
            "ScratchObject[2]",
            "ScratchObject[3]",
            "ScratchObject[4]",
            "ScratchObject[5]",
            "ScratchObject[6]",
            "ScratchObject[7]",
            "current_object",
            "TargetObject",
            "KilledObject",
            "KillerObject",
            "CandySpwnrObject"
        };

        public List<string> PlayerTypeRef = new List<string>
        {
            "none",
            "Player0",
            "Player1",
            "Player2",
            "Player3",
            "Player4",
            "Player5",
            "Player6",
            "Player7",
            "Player8",
            "Player9",
            "Player10",
            "Player11",
            "Player12",
            "Player13",
            "Player14",
            "Player15",
            "GlobalPlayer[0]",
            "GlobalPlayer[1]",
            "GlobalPlayer[2]",
            "GlobalPlayer[3]",
            "GlobalPlayer[4]",
            "GlobalPlayer[5]",
            "GlobalPlayer[6]",
            "GlobalPlayer[7]",
            "GlobalPlayer[8]",
            "GlobalPlayer[9]",
            "ScratchPlayer[0]",
            "ScratchPlayer[1]",
            "ScratchPlayer[2]",
            "ScratchPlayer[3]",
            "ScratchPlayer[4]",
            "ScratchPlayer[5]",
            "ScratchPlayer[6]",
            "ScratchPlayer[7]",
            "current_player",
            "HudPlayer",
            "HudTargetPlayer",
            "ObjectKiller",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled",
            "Unlabelled"
        };


        public List<string> Objects = new List<string>
        {
            "spartan",
            "elite",
            "monitor",
            "flag",
            "bomb",
            "ball",
            "area",
            "stand",
            "destination",
            "frag_grenade",
            "plasma_grenade",
            "assault_rifle",
            "plasma_pistol",
            "smg",
            "energy_sword",
            "magnum",
            "magnum_survivor",
            "needler",
            "plasma_rifle",
            "rocket_launcher",
            "shotgun_survivor",
            "shotgun",
            "sniper_rifle",
            "brute_shot",
            "beam_rifle",
            "warthog",
            "ghost",
            "scorpion",
            "wraith",
            "banshee",
            "mongoose",
            "hornet",
            "territory_static",
            "ctf_flag_return_area",
            "ctf_flag_spawn_point",
            "respawn_zone",
            "oddball_ball_spawn_point",
            "fusion_coil",
            "initial_spawn_point",
            "teleporter_sender",
            "teleporter_reciever",
            "teleporter_2way",
            "weak_respawn_zone",
            "weak_anti_respawn_zone",
            "mp_cinematic_camera",
            "cov_power_module",
            "machinegun",
            "machinegun_turret",
            "warthog_gunner",
            "warthog_gauss_turret",
            "teleporter",
            "juggernaut",
            "covenant_carbine",
            "fuel_rod_cannon",
            "battle_rifle",
            "koth_falloff_respawn",
            "aa_none",
            "odd_falloff_respawn",
            "koth_anti_respawn",
            "odd_anti_respawn",
            "bomb_disarm",
            "flashing_area",
            "h2a_koth_hill_flashing",
            "h2a_territories_flashing",
            "h2a_generic_goal_flashing",
            "h2a_assault_flashing",
            "koth_incoming",
            "h2a_assault_armzone",
            "h2a_generic_goal",
            "h2a_koth_hill",
            "h2a_assault_bomb_goal_area",
            "h2a_territory_static",
            "odd_confetti",
            "carry_bomb",
            "koth_explosion",
            "on_switch",
            "off_switch",
            "toggle_switch",
            "mp_ca_ascension_dome_shield",
            "timed_on_switch",
            "timed_toggle_switch",
            "h2a_sancuary_waterfall",
            "damage_on_switch",
            "shutter",
            "h2a_lockout_steamvent",
            "gungoose",
            "brute_plasma",
            "smg_suppressed",
            "h2a_assault_bomb",
            "sentinel_beam",
            "h2a_ricochet_screen_flash",
            "h2a_oddball_ricochet",
            "odd_confetti_ricochet",
            "smaller_anti_respawn_zone_falloff",
            "respawn_zone_force",
            "anti_respawn_zone_force",
            "landmine",
            "h2a_warthog_civ",
            "h2a_banshee_heretic",
            "machinegun_turret_fixed",
            "h2a_warthog_gauss",
            "cov_turret_fixed",
            "barr_on_switch",
            "h2a_active_camo",
            "h2a_speedboost",
            "h2a_overshield",
            "h2a_infected_sword",
            "h2a_assault_bomb_explosion"

        };



      
        

        public class Triggers
        {
            public int TriggerCount;
            public int TriggerType;
        }

        public struct PlayerStats
        {
            public int PlayerStatsCount;
            public int StringIndex;
            public int Format;
            public int Sortorder;
            public int GroupByTeam;

        }

        public struct GlobalVariables
        {
            public int GlobalNumbersCount;

            public int GlobalNumber;
            public int NumberLocality;

            public int GlobalTimerCount;
            public int GlobalTimer;

            public int GlobalTeamCount;
            public int GlobalTeam;
            public int TeamLocality;

            public int GlobalPlayerCount;
            public int GlobalPlayer;

        }



        public struct Map
        {
            public int MapID;
            public int mappermsflip;
        }


     



        public FileHeader fh = new();
        public GametypeHeader gth = new();
        public ModeSettings ms = new();
        public SpawnSettings ss = new();
        public GameSettings gs = new();
        public string binaryString = "";
        public static List<Gametype> gametypeItems = new();
        public Gametype gt = new();
        public void ReadBinary(string filePath)
        {
            byte[] binaryData = File.ReadAllBytes(filePath);

            //Convert binaryData to a string of 1s and 0s
            binaryString = GetBinaryString(binaryData, 752, 32);
            //Check the next 32 bits to see if it is a gametype
            string gametype = GetValue(32);
            //convert gametype to a string
            string gametypeString = "";
            for (int i = 0; i < gametype.Length; i += 8)
            {
                string s = gametype.Substring(i, 8);
                gametypeString += (char)Convert.ToInt32(s, 2);
            }
            if (gametypeString != "mpvr")
            {
                binaryString = GetBinaryString(binaryData, 128, binaryData.Length * 7);
            }
            else
            {
                binaryString = GetBinaryString(binaryData, 752, binaryData.Length * 7);
            }

            //Read FileHeader

            fh.mpvr = GetValue(32);
            //Convert mpvr to a string
            string mpvr = "";
            for (int i = 0; i < fh.mpvr.Length; i += 8)
            {
                string s = fh.mpvr.Substring(i, 8);
                mpvr += (char)Convert.ToInt32(s, 2);
            }
            if (mpvr == "gvar")
            {
                Settings.Default.IsGvar = true;
                GetValue(32);
            }
            if (mpvr == "mpvr")
            {
                fh.megaloversion = ConvertToInt(GetValue(32));
            }
            fh.Unknown0x2F8 = ConvertToInt(GetValue(16));
            fh.Unknown0x2FA = ConvertToInt(GetValue(16));

            if (mpvr == "mpvr")
            {
                Settings.Default.IsGvar = false;
                fh.UnknownHash0x2FC = GetValue(160);
                fh.Blank0x310 = GetValue(32);
                fh.Fileusedsize = ConvertToInt(GetValue(32));
            }



            fh.Unknown0x318 = ConvertToInt(GetValue(2));
            fh.VariantType = ConvertToInt(GetValue(2));
            fh.Unknown0x319 = ConvertToInt(GetValue(4));
            fh.Unknown0x31D = ConvertToInt(GetValue(32));
            fh.Unknown0x31C = ConvertToInt(GetValue(32));
            fh.FileLength = ConvertToInt(GetValue(32));






            gt.FileHeader = Newtonsoft.Json.JsonConvert.SerializeObject(fh);

            if (fh.Unknown0x2F8 == 54)
            {
                Settings.Default.DecompiledVersion = 0; //Reach
            }

            if (fh.Unknown0x2F8 == 137)
            {
                Settings.Default.DecompiledVersion = 2; //H2A
            }

            if (fh.Unknown0x2F8 == 132)
            {
                Settings.Default.DecompiledVersion = 1; //H4
            }
            //ConvertAndSaveToXml(fh, "gametype.xml");

            //Read GametypeHeader

            gth.ID0x48 = GetValue(64);
            gth.ID0x50 = GetValue(64);
            gth.ID0x58 = GetValue(64);
            gth.Blank0x60 = GetValue(64);

            if (Settings.Default.DecompiledVersion == 0)
            {
                gth.UnknownFlags = GetValue(9);
            }
            if (Settings.Default.DecompiledVersion > 0)
            {
                gth.UnknownFlags = GetValue(8);
            }
            gth.Unknown_1 = ConvertToInt(GetValue(32));
            gth.Unknown0x1 = ConvertToInt(GetValue(8));
            gth.Blank04 = ConvertToInt(GetValue(32));
            gth.TimeStampUint = ConvertToInt(GetValue(32));
            gth.XUID = GetValue(64);
            gth.Gamertag = ReadStringFromBits(binaryString, true);
            Settings.Default.Gamertag = gth.Gamertag;
            gth.Blank041bit = GetValue(33);
            gth.EditTimeStampUint = ConvertToInt(GetValue(32));
            gth.EditXUID = GetValue(64);
            gth.EditGamertag = ReadStringFromBits(binaryString, true);
            Settings.Default.EditGamertag = gth.EditGamertag;
            gth.UnknownFlag1 = ConvertToInt(GetValue(1));
            gth.Title = ReadUStringFromBits(binaryString);
            Settings.Default.Title = gth.Title;
            gth.Description = ReadUStringFromBits(binaryString);
            Settings.Default.Description = gth.Description;
            gth.GameIcon = ConvertToInt(GetValue(8));
            gt.GametypeHeader = Newtonsoft.Json.JsonConvert.SerializeObject(gth);
            //ConvertAndSaveToXml(gth, "gametype.xml");

            //Read ModeSettings

            if (Settings.Default.DecompiledVersion == 0)
            {
                ms.UnknownFlag2 = ConvertToInt(GetValue(1));
            }

            if (Settings.Default.DecompiledVersion > 0)
            {
                ms.UnknownFlag2 = ConvertToInt(GetValue(2));
            }

            ms.Teamsenabled = ConvertToInt(GetValue(1));
            ms.Resetmaponnewroundunused = ConvertToInt(GetValue(1));
            ms.Resetplayersonnewroundunused = ConvertToInt(GetValue(1));
            ms.Perfectionmedalenabled = ConvertToInt(GetValue(1));
            ms.RoundTimeLimit = ConvertToInt(GetValue(8));
            ms.NumberOfRounds = ConvertToInt(GetValue(5));
            ms.RoundsToWin = ConvertToInt(GetValue(4));
            if (Settings.Default.DecompiledVersion > 0)
            {
                ms.H2AH4 = new()
                {
                    KillCamEnabled = ConvertToInt(GetValue(1)),
                    PointsSystemEnabled = ConvertToInt(GetValue(1)),
                    FinalKillCamEnabled = ConvertToInt(GetValue(1)),
                    Unknown2 = ConvertToInt(GetValue(1)),
                    Bit4 = ConvertToInt(GetValue(1)),
                    Bit5 = ConvertToInt(GetValue(1)),
                    MoshDifficulty = ConvertToInt(GetValue(2)),
                    ProtoMode = ConvertToInt(GetValue(11)),
                    Unknown4 = ConvertToInt(GetValue(4)),
                    ClassColorOverride = ConvertToInt(GetValue(1)),
                    InheritRespawnTime = ConvertToInt(GetValue(1)),
                    Unknown42 = ConvertToInt(GetValue(4))
                };
                ms.SuddenDeathTime = ConvertToInt(GetValue(7));
            }
            if (Settings.Default.DecompiledVersion == 0)
            {

                ms.SuddenDeathTime = ConvertToInt(GetValue(7));
                ms.Reach = new()
                {
                    GracePeriod = ConvertToInt(GetValue(5))
                };
            }

            gt.ModeSettings = Newtonsoft.Json.JsonConvert.SerializeObject(ms);

            //ConvertAndSaveToXml(ms, "gametype.xml");

            //Read SpawnSettings




            if (Settings.Default.DecompiledVersion == 0)
            {
                ss.Reach = new();
                ss.Reach.RespawnOnKills = ConvertToInt(GetValue(1));
                ss.Reach.respawnatlocationunused = ConvertToInt(GetValue(1));
                ss.Reach.respawnwithteammateunused = ConvertToInt(GetValue(1));
                ss.Reach.RespawnSyncwithteam = ConvertToInt(GetValue(1));
            }


            ss.LivesPerround = ConvertToInt(GetValue(6));
            ss.TeamLivesPerround = ConvertToInt(GetValue(7));
            if (Settings.Default.DecompiledVersion > 0)
            {
                ss.H2AH4 = new();
                ss.H2AH4.MinRespawnTime = ConvertToInt(GetValue(8));
            }
            ss.RespawnTime = ConvertToInt(GetValue(8));
            ss.Suicidepenalty = ConvertToInt(GetValue(8));
            ss.Betrayalpenalty = ConvertToInt(GetValue(8));
            ss.RespawnTimegrowth = ConvertToInt(GetValue(4));
            ss.LoadoutCamTime = ConvertToInt(GetValue(4));
            ss.Respawntraitsduration = ConvertToInt(GetValue(6));
            ss.RespawnPlayerTraits = new();
            //GetValue(333);
            ss.RespawnPlayerTraits = ReadTraits(binaryString, ss.RespawnPlayerTraits);
            //GetValue(3);
            gt.SpawnSettings = Newtonsoft.Json.JsonConvert.SerializeObject(ss);

            //ConvertAndSaveToXml(ss, "gametype.xml");

            //Read GameSettings


            if (Settings.Default.DecompiledVersion == 0)
            {
                gs.EnableObservers = ConvertToInt(GetValue(1));
                gs.Teamchanging = ConvertToInt(GetValue(2));
                gs.FriendlyFire = ConvertToInt(GetValue(1));
                gs.BetrayalBooting = ConvertToInt(GetValue(1));
                gs.ProximityVoice = ConvertToInt(GetValue(1));
                gs.Dontrestrictteamvoicechat = ConvertToInt(GetValue(1));
                gs.allowdeadplayerstotalk = ConvertToInt(GetValue(1));
                gs.Indestructiblevehicles = ConvertToInt(GetValue(1));
                gs.turretsonmap = ConvertToInt(GetValue(1));
                gs.powerupsonmap = ConvertToInt(GetValue(1));
                gs.abilitiesonmap = ConvertToInt(GetValue(1));
                gs.shortcutsonmap = ConvertToInt(GetValue(1));
                gs.grenadesonmap = ConvertToInt(GetValue(1));
                gs.BasePlayerTraits = new();
                gs.BasePlayerTraits = ReadTraits(binaryString, gs.BasePlayerTraits);
                gs.WeaponSet = ConvertToInt(GetValue(8));
                gs.VehicleSet = ConvertToInt(GetValue(8));
            }

            if (Settings.Default.DecompiledVersion > 0)
            {
                gs.H2AH4 = new();
                gs.EnableObservers = ConvertToInt(GetValue(1));
                gs.Teamchanging = ConvertToInt(GetValue(2));
                gs.FriendlyFire = ConvertToInt(GetValue(1));
                gs.H2AH4.Unknown1 = ConvertToInt(GetValue(1));
                gs.Dontrestrictteamvoicechat = ConvertToInt(GetValue(1));
                gs.H2AH4.Unknown2 = ConvertToInt(GetValue(1));
                gs.H2AH4.Unknown3 = ConvertToInt(GetValue(1));
                gs.Indestructiblevehicles = ConvertToInt(GetValue(1));
                gs.turretsonmap = ConvertToInt(GetValue(1));
                gs.powerupsonmap = ConvertToInt(GetValue(1));
                gs.abilitiesonmap = ConvertToInt(GetValue(1));
                gs.shortcutsonmap = ConvertToInt(GetValue(1));
                gs.BasePlayerTraits = new();
                gs.BasePlayerTraits = ReadTraits(binaryString, gs.BasePlayerTraits);
                gs.WeaponSet = ConvertToInt(GetValue(8));
                gs.VehicleSet = ConvertToInt(GetValue(8));
                gs.H2AH4.EquipmentSet = ConvertToInt(GetValue(8));
                if (Settings.Default.IsGvar)
                {
                    gs.H2AH4.Unknown4 = GetValue(41);
                } else
                {
                    gs.H2AH4.Unknown4 = GetValue(55);
                }
                
            }

            gt.GameSettings = Newtonsoft.Json.JsonConvert.SerializeObject(gs);
            //ConvertAndSaveToXml(gs, "gametype.xml");

            //Read PowerupTraits
            PowerupTraits pt = new();
            if (Settings.Default.DecompiledVersion == 0)
            {
                pt.RedPlayerTraits = new();
                pt.BluePlayerTraits = new();
                pt.YellowPlayerTraits = new();
                pt.RedPlayerTraits = ReadTraits(binaryString, pt.RedPlayerTraits);
                pt.BluePlayerTraits = ReadTraits(binaryString, pt.BluePlayerTraits);
                pt.YellowPlayerTraits = ReadTraits(binaryString, pt.YellowPlayerTraits);
                pt.RedPowerupDuration = ConvertToInt(GetValue(7));
                pt.BluePowerupDuration = ConvertToInt(GetValue(7));
                pt.YellowPowerupDuration = ConvertToInt(GetValue(7));
            }
            else
            {
                pt.RedPlayerTraits = null;
                pt.BluePlayerTraits = null;
                pt.YellowPlayerTraits = null;
                pt.RedPowerupDuration = null;
                pt.BluePowerupDuration = null;
                pt.YellowPowerupDuration = null;
            }

            if (Settings.Default.DecompiledVersion > 0)
            {
                pt.H2AH4 = new();
                pt.H2AH4.DamageTraits = new();
                pt.H2AH4.DamageTraitsRuntime = new();
                pt.H2AH4.SpeedTraits = new();
                pt.H2AH4.SpeedTraitsRuntime = new();
                pt.H2AH4.OverShieldTraits = new();
                pt.H2AH4.OverShieldTraitsRuntime = new();
                pt.H2AH4.CustomTraits = new();
                pt.H2AH4.CustomTraitsRuntime = new();
                pt.H2AH4.DamageTraits = ReadTraits(binaryString, pt.H2AH4.DamageTraits);
                
                
                
                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.DamageTraitsDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.DamageTraitsDuration = ConvertToInt(GetValue(6));
                }
                pt.H2AH4.DamageTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.DamageTraitsRuntime);
                
                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.DamageTraitsRuntimeDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.DamageTraitsRuntimeDuration = ConvertToInt(GetValue(6));
                }
                pt.H2AH4.SpeedTraits = ReadTraits(binaryString, pt.H2AH4.SpeedTraits);

                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.SpeedTraitsDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.SpeedTraitsDuration = ConvertToInt(GetValue(6));
                }
                

                pt.H2AH4.SpeedTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.SpeedTraitsRuntime);
                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.SpeedTraitsRuntimeDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.SpeedTraitsRuntimeDuration = ConvertToInt(GetValue(6));
                }
                

                pt.H2AH4.OverShieldTraits = ReadTraits(binaryString, pt.H2AH4.OverShieldTraits);
                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.OverShieldTraitsDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.OverShieldTraitsDuration = ConvertToInt(GetValue(6));
                }

                pt.H2AH4.OverShieldTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.OverShieldTraitsRuntime);
                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.OverShieldTraitsRuntimeDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.OverShieldTraitsRuntimeDuration = ConvertToInt(GetValue(6));
                }
                

                pt.H2AH4.CustomTraits = ReadTraits(binaryString, pt.H2AH4.DamageTraits);

                
                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.CustomTraitsDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.CustomTraitsDuration = ConvertToInt(GetValue(6));
                }
                pt.H2AH4.CustomTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.CustomTraitsRuntime);
                if (Settings.Default.IsGvar)
                {
                    pt.H2AH4.CustomTraitsRuntimeDuration = ConvertToInt(GetValue(7));
                } else
                {
                    pt.H2AH4.CustomTraitsRuntimeDuration = ConvertToInt(GetValue(6));
                }
            }

            gt.PowerupTraits = Newtonsoft.Json.JsonConvert.SerializeObject(pt);

            //Read TeamSettings
            TeamSettings ts = new();
            ts.TeamScoringMethod = ConvertToInt(GetValue(3));
            if (Settings.Default.DecompiledVersion > 0)
            {
                ts.Unknown1 = ConvertToInt(GetValue(4));
                ts.Unknown2 = ConvertToInt(GetValue(32));
                ts.Unknown3 = ConvertToInt(GetValue(32));
                ts.Unknown4 = ConvertToInt(GetValue(32));
            }
            ts.PlayerSpecies = ConvertToInt(GetValue(3));
            ts.DesignatorSwitchtype = ConvertToInt(GetValue(2));
            ts.Team1Options = ReadTeaMOptions();
            ts.Team2Options = ReadTeaMOptions();
            ts.Team3Options = ReadTeaMOptions();
            ts.Team4Options = ReadTeaMOptions();
            ts.Team5Options = ReadTeaMOptions();
            ts.Team6Options = ReadTeaMOptions();
            ts.Team7Options = ReadTeaMOptions();
            ts.Team8Options = ReadTeaMOptions();
            gt.TeamSettings = Newtonsoft.Json.JsonConvert.SerializeObject(ts);

            //ConvertAndSaveToXml(ts, "gametype.xml");

            //Read LoadoutCluster
            LoadoutCluster lc = new();
            if (Settings.Default.DecompiledVersion > 0)
            {
                lc.MapLoadoutsEnabled = ConvertToInt(GetValue(1));
            }
            lc.EliteLoadoutsEnabled = ConvertToInt(GetValue(1));
            lc.SpartanLoadoutsEnabled = ConvertToInt(GetValue(1));
            if (Settings.Default.DecompiledVersion > 0)
            {
                lc.CustomLoadoutsEnabled = ConvertToInt(GetValue(1));
            }
            lc.Loadout1 = ReadLoadoutOptions();
            lc.Loadout2 = ReadLoadoutOptions();
            lc.Loadout3 = ReadLoadoutOptions();
            lc.Loadout4 = ReadLoadoutOptions();
            lc.Loadout5 = ReadLoadoutOptions();
            lc.Loadout6 = ReadLoadoutOptions();

                lc.Loadout7 = ReadLoadoutOptions();
                lc.Loadout8 = ReadLoadoutOptions();
                lc.Loadout9 = ReadLoadoutOptions();
                lc.Loadout10 = ReadLoadoutOptions();
                lc.Loadout11 = ReadLoadoutOptions();
                lc.Loadout12 = ReadLoadoutOptions();
                lc.Loadout13 = ReadLoadoutOptions();
                lc.Loadout14 = ReadLoadoutOptions();
                lc.Loadout15 = ReadLoadoutOptions();
                lc.Loadout16 = ReadLoadoutOptions();
                lc.Loadout17 = ReadLoadoutOptions();
                lc.Loadout18 = ReadLoadoutOptions();
                lc.Loadout19 = ReadLoadoutOptions();
                lc.Loadout20 = ReadLoadoutOptions();
                lc.Loadout21 = ReadLoadoutOptions();
                lc.Loadout22 = ReadLoadoutOptions();
                lc.Loadout23 = ReadLoadoutOptions();
                lc.Loadout24 = ReadLoadoutOptions();
                lc.Loadout25 = ReadLoadoutOptions();
                lc.Loadout26 = ReadLoadoutOptions();
                lc.Loadout27 = ReadLoadoutOptions();
                lc.Loadout28 = ReadLoadoutOptions();
                lc.Loadout29 = ReadLoadoutOptions();
                lc.Loadout30 = ReadLoadoutOptions();
                //if (Settings.Default.DecompiledVersion > 0)
                //{
                //    lc.Loadout31 = ReadLoadoutOptions();
                //}

            gt.loadoutCluster = Newtonsoft.Json.JsonConvert.SerializeObject(lc);

            if (Settings.Default.DecompiledVersion > 0)
            {
                //Read Ordnance
                Ordinance o = new();
                o.Ordinances2bit = ConvertToInt(GetValue(1));
                o.InitialOrdinance = ConvertToInt(GetValue(1));
                o.RandomOrdinance = ConvertToInt(GetValue(1));
                o.PersonalOrdinance = ConvertToInt(GetValue(1));
                o.OrdinancesEnabled = ConvertToInt(GetValue(1));
                o.Ordinance8Bit = ConvertToInt(GetValue(8));
                o.OrdResupplyTimeMin = ConvertToInt(GetValue(16));
                o.OrdResupplyTimeMax = ConvertToInt(GetValue(16));
                o.Ordinance16Bit1 = ConvertToInt(GetValue(16));
                o.InitialDropsetName = ReadStringFromBits(binaryString, true);
                o.Ordinance16Bit2 = ConvertToInt(GetValue(16));
                o.Ordinance16Bit3 = ConvertToInt(GetValue(16));
                o.RandomDropsetName = ReadStringFromBits(binaryString, true);
                o.PersonalDropsetName = ReadStringFromBits(binaryString, true);
                o.OrdinanceSubstitutionsName = ReadStringFromBits(binaryString, true);
                o.CustomizePersonalOrdinance = ConvertToInt(GetValue(1));

                o.OrdnanceRight = ReadOrdnanceWeights();
                o.OrdnanceLeft = ReadOrdnanceWeights();
                o.OrdnanceDown = ReadOrdnanceWeights();
                o.OrdnanceUnknown = ReadOrdnanceWeights();
                o.OrdnancePointsRequirement = GetValue(30);
                o.OrdnanceIncreaseMultiplier = GetValue(30);

            }

            //ConvertAndSaveToXml(lc, "gametype.xml");

            //Read ScriptedPlayerTraits
            ScriptedPlayerTraits spt = new();
            spt.count = ConvertToInt(GetValue(5));
            for (int i = 0; i < spt.count; i++)
            {
                if (Settings.Default.DecompiledVersion == 0)
                {
                    spt.String1 = ConvertToInt(GetValue(7));
                    spt.String2 = ConvertToInt(GetValue(7));
                }
                if (Settings.Default.DecompiledVersion > 0)
                {
                    spt.H2AH4 = new();
                    spt.String1 = ConvertToInt(GetValue(8));
                    spt.String2 = ConvertToInt(GetValue(8));
                    
                }
                spt.PlayerTraits = new();
                spt.PlayerTraits = ReadTraits(binaryString, spt.PlayerTraits);
                if (Settings.Default.DecompiledVersion > 0 )
                {
                    spt.H2AH4.hidden = ConvertToInt(GetValue(1));
                }
            }

            gt.scriptedPlayerTraits = Newtonsoft.Json.JsonConvert.SerializeObject(spt);

            //ConvertAndSaveToXml(spt, "gametype.xml");

            //Read ScriptOptions
            ScriptOptions so = new();
            so.count = ConvertToInt(GetValue(5));
            for (int i = 0; i < so.count; i++)
            {
                if (Settings.Default.DecompiledVersion == 0)
                {
                    so.String1 = ConvertToInt(GetValue(7));
                    so.String2 = ConvertToInt(GetValue(7));
                }
                if (Settings.Default.DecompiledVersion > 0)
                {
                    so.String1 = ConvertToInt(GetValue(8));
                    so.String2 = ConvertToInt(GetValue(8));
                }
                so.ScriptOption = ConvertToInt(GetValue(1));
                if (so.ScriptOption == 0)
                {
                    if (Settings.Default.DecompiledVersion == 0)
                    {
                        so.ChildIndex = ConvertToInt(GetValue(3));
                        so.ScriptOptionChild = ConvertToInt(GetValue(4));
                    }
                    if (Settings.Default.DecompiledVersion > 0)
                    {
                        so.ChildIndex = ConvertToInt(GetValue(4));
                        so.ScriptOptionChild = ConvertToInt(GetValue(5));
                    }


                    for (int j = 0; j < so.ScriptOptionChild; j++)
                    {
                        so.Value = ConvertToInt(GetValue(10));
                        if (Settings.Default.DecompiledVersion == 0)
                        {
                            so.String1 = ConvertToInt(GetValue(7));
                            so.String2 = ConvertToInt(GetValue(7));
                        }
                        if (Settings.Default.DecompiledVersion > 0)
                        {
                            so.String1 = ConvertToInt(GetValue(8));
                            so.String2 = ConvertToInt(GetValue(8));
                        }

                    }
                    if (Settings.Default.DecompiledVersion == 0)
                    {
                        so.Unknown = ConvertToInt(GetValue(3));
                    }
                    if (Settings.Default.DecompiledVersion > 0)
                    {
                        so.ActualChildIndex = ConvertToInt(GetValue(4));
                    }
                }
                if (so.ScriptOption == 1)
                {
                    so.range1 = ConvertToInt(GetValue(10));
                    so.range2 = ConvertToInt(GetValue(10));
                    so.range3 = ConvertToInt(GetValue(10));
                    so.range4 = ConvertToInt(GetValue(10));
                }

            }
            gt.scriptOptions = Newtonsoft.Json.JsonConvert.SerializeObject(so);

            //ConvertAndSaveToXml(so, "gametype.xml");
            //return;
            //Read Strings
            Strings st = new();
            if (Settings.Default.DecompiledVersion > 0)
            {
                st.Stringtable = ReadLangStrings(16, 8, false);
                st.StringNameIndex = ConvertToInt(GetValue(8));
                st.metanameStrings = ReadLangStrings(11, 1, false);
                st.metadescStrings = ReadLangStrings(13, 1, false);
                st.metaintroStrings = ReadLangStrings(13, 1, false);
                st.metagroupStrings = ReadLangStrings(10, 1, false);
            }

            if (Settings.Default.DecompiledVersion == 0)
            {
                st.Stringtable = ReadLangStrings(15, 7, false);
                st.StringNameIndex = ConvertToInt(GetValue(7));
                st.metanameStrings = ReadLangStrings(9, 1, false);
                st.metadescStrings = ReadLangStrings(12, 1, false);
                st.metagroupStrings = ReadLangStrings(9, 1, false);
            }

            gt.Strings = Newtonsoft.Json.JsonConvert.SerializeObject(st);

            //ConvertAndSaveToXml(st, "gametype.xml");

            //Read Game
            Game g = new();
            g.ActualGameicon = ConvertToInt(GetValue(5));
            g.ActualGamecategory = ConvertToInt(GetValue(5));
            if (Settings.Default.ConvertToForge)
            {
                g.ActualGamecategory = 1;
            }
            gt.Game = Newtonsoft.Json.JsonConvert.SerializeObject(g);

            //ConvertAndSaveToXml(g, "gametype.xml");

            //Read Map
            Map map = new();
            int mapcount = ConvertToInt(GetValue(6));
            for (int i = 0; i < mapcount; i++)
            {
                map.MapID = ConvertToInt(GetValue(16));
            }
            map.mappermsflip = ConvertToInt(GetValue(1));
            gt.Map = map;

            //ConvertAndSaveToXml(map, "gametype.xml");


            //Read PlayerRatings
            PlayerRatings pr = new();
            pr.ratingscale = ConvertToInt(GetValue(32));
            pr.kilweight = ConvertToInt(GetValue(32));
            pr.assistweight = ConvertToInt(GetValue(32));
            pr.betrayalweight = ConvertToInt(GetValue(32));
            pr.deathweight = ConvertToInt(GetValue(32));
            pr.normalizebymaxkills = ConvertToInt(GetValue(32));
            pr.baserating = ConvertToInt(GetValue(32));
            pr.range = ConvertToInt(GetValue(32));
            pr.lossscalar = ConvertToInt(GetValue(32));
            pr.customstat0 = ConvertToInt(GetValue(32));
            pr.customstat1 = ConvertToInt(GetValue(32));
            pr.customstat2 = ConvertToInt(GetValue(32));
            pr.customstat3 = ConvertToInt(GetValue(32));
            pr.expansion0 = ConvertToInt(GetValue(32));
            pr.expansion1 = ConvertToInt(GetValue(32));
            pr.showplayerratings = ConvertToInt(GetValue(1));
            gt.playerratings = pr;

            //ConvertAndSaveToXml(pr, "gametype.xml");
            if (Settings.Default.DecompiledVersion > 0 && Settings.Default.IsGvar == false)
            {

                
                    GetValue(42);
                    int loadouts = ConvertToInt(GetValue(9));
                for (int i = 0; i < loadouts; i++)
                {
                    int size = ConvertToInt(GetValue(2));
                    int enabled = ConvertToInt(GetValue(1));
                    int loadoutName = ConvertToInt(GetValue(1)) == 0 ? ConvertToInt(GetValue(7)) : -1;
                    int primary = ConvertToInt(GetValue(8));
                    int secondary = ConvertToInt(GetValue(8));
                    int armorAbility = ConvertToInt(GetValue(8));
                    int tacticalPackage = ConvertToInt(GetValue(8));
                    int supportUpgrade = ConvertToInt(GetValue(8));
                    int grenadeCount = ConvertToInt(GetValue(5));
                    int unknown = ConvertToInt(GetValue(6));

                }
                    GetValue(1728);

                    

            }
            else if (Settings.Default.DecompiledVersion > 0 && Settings.Default.IsGvar == true)
            {
                GetValue(2126);
            } else
            {
                GetValue(2642);
            }





            //We have now reached the gametype script!


            //Read Conditions
            List<string> ConditionsList = new();
            List<long> ConditionOffsetList = new();
            Conditions c = new();
            c.ConditionCount = ConvertToInt(GetValue(10));

            for (int i = 0; i < c.ConditionCount; i++)
            {
                c.ConditionType = ConvertToInt(GetValue(5));
                
                string subplayer = "";
                string subplayer2 = "";
                string subplayer3 = "";
                string conditionType = "";
                string oper = "";
                switch (c.ConditionType) {
                    case 1:
                        c.NOT = ConvertToInt(GetValue(1)) == 1 ? "not" : "";
                        c.ORSequence = ConvertToInt(GetValue(10));
                        c.ConditionOffset = Convert.ToInt64(GetValue(11));
                        conditionType = "if";
                        c.Vartype1 = ConvertToInt(GetValue(3));
                        (c.SpecificType, subplayer) = GetVarType(c.Vartype1);
                        //c.RefType = ConvertToInt(GetValue(6));
                        c.Vartype2 = ConvertToInt(GetValue(3));
                        (c.SpecificType2, subplayer2) = GetVarType(c.Vartype2);

                        //c.RefType2 = ConvertToInt(GetValue(6));
                        c.Oper = ConvertToInt(GetValue(3));
                        switch (c.Oper)
                        {
                            case 0:
                                oper = "<";
                                break;
                            case 1:
                                oper = ">";
                                break;
                            case 2:
                                oper = "==";
                                break;
                            case 3:
                                oper = "<=";
                                break;
                            case 4:
                                oper = ">=";
                                break;
                            case 5:
                                oper = "!=";
                                break;
                        }

                        //Build condition string
                        string condition = $"condition {c.NOT} {conditionType} {c.SpecificType}.{subplayer} {oper} {c.SpecificType2}.{subplayer2}";
                        ConditionsList.Add(condition);
                        ConditionOffsetList.Add(c.ConditionOffset);

                        break;
                    
                }
                
            }

            gametypeItems.Add(gt);

            //Read Actions


            //List<string> ActionList = new();
            //Actions ac = new();
            //ac.ActionCount = ConvertToInt(GetValue(11));

            //int objectTypeRef = 3;
            //int objectType = 12;
            //int labelRef = 1;
            //int spawnFlags = 1;
            //int offset = 8;
            //int names = 8;
            //bool isInline = false;
            //for (int i = 0; i < ac.ActionCount; i++)
            //{
            //    ac.ActionType = ConvertToInt(GetValue(8));
            //    int type = 0;
            //    string subvalue = "";
            //    string subvalue2 = "";
            //    switch (ac.ActionType)
            //    {

                    
                        
            //        case 2:
            //            //Create Object
                        
            //            ac.Parameter1 = Objects[ConvertToInt(GetValue(objectType))];
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter2, subvalue) = GetRefType(type);
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter3, subvalue2) = GetRefType(type);
            //            ac.Parameter4 = ConvertToInt(GetValue(labelRef));
            //            if (ac.Parameter4 == 0)
            //            {
            //                ac.Parameter4 = ConvertToInt(GetValue(4));
            //            }
            //            ac.Parameter5 = ConvertToInt(GetValue(spawnFlags)) == 1 ? "never_garbage_collect": "";
            //            ac.Parameter6 = ConvertToInt(GetValue(spawnFlags)) == 1 ? "suppress_effect" : "";
            //            ac.Parameter7 = ConvertToInt(GetValue(spawnFlags)) == 1 ? "absolute_orientation" : "";
            //            ac.Parameter8 = ConvertToInt(GetValue(offset));
            //            ac.Parameter9 = ConvertToInt(GetValue(offset));
            //            ac.Parameter10 = ConvertToInt(GetValue(offset));
            //            ac.Parameter11 = Names[ConvertToInt(GetValue(names))];

            //            //Build action string
            //            string action = $"action create_object '{ac.Parameter1}' at {ac.Parameter3}{subvalue} offset {ac.Parameter8} {ac.Parameter9} {ac.Parameter10} set {ac.Parameter2}{subvalue2} {ac.Parameter5} {ac.Parameter6} {ac.Parameter7} variant {ac.Parameter11}";
            //            ActionList.Add(action);
            //            break;

            //        case 3:
            //            //Delete Object
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter1, subvalue) = GetRefType(type);
            //            ActionList.Add($"action delete_object {ac.Parameter1}{subvalue}");
            //            break;
            //        case 4:
            //            //Navpoint Set Visible
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = PlayerSet[ConvertToInt(GetValue(3))];
            //            ActionList.Add($"action navpoint_set_visible {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 5:
            //            //Navpoint Set Icon
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter1, subvalue) = GetRefType(type);
            //            int icon = ConvertToInt(GetValue(5));
            //            ac.Parameter2 = WaypointIcon[icon];
            //            string num = "";
            //            if (icon == 12)
            //            {
            //                icon = ConvertToInt(GetValue(7));
            //                num = GetNumericRefType(icon);
            //            }
            //            ActionList.Add($"action navpoint_set_icon {ac.Parameter1}{subvalue} {ac.Parameter2} {num}");
            //            break;
            //        case 6:
            //            //Navpoint Secondary Icon
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter1, subvalue) = GetRefType(type);
            //            int icon2 = ConvertToInt(GetValue(5));
            //            ac.Parameter2 = WaypointIcon[icon2];
            //            num = "";
            //            if (icon2 == 12)
            //            {
            //                icon2 = ConvertToInt(GetValue(7));
            //                num = GetNumericRefType(icon2);
            //            }
            //            ActionList.Add($"action NavPointSetSecondaryIcon {ac.Parameter1}{subvalue} {ac.Parameter2} {num}");
            //            break;
            //        case 7:
            //            //Navpoint Priority
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter1, subvalue) = GetRefType(type);
            //            ac.Parameter2 = WaypointPriority[ConvertToInt(GetValue(2))];
            //            ActionList.Add($"action navpoint_set_priority {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 8:
            //            //Navpoint Timer
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter1, subvalue) = GetRefType(type);
            //            int timer = ConvertToInt(GetValue(1));
            //            ac.Parameter3 = timer == 0 ? Convert.ToString(ConvertToInt(GetValue(2))) : "none";
            //            Convert.ToString(ac.Parameter4);
            //            ActionList.Add($"action navpoint_set_timer {ac.Parameter1}{subvalue} ObjectTimer{ac.Parameter4}");
            //            break;
            //        case 9:
            //            //Navpoint Range
            //            type = ConvertToInt(GetValue(objectTypeRef));
            //            (ac.Parameter1, subvalue) = GetRefType(type);
            //            type = ConvertToInt(GetValue(7));
            //            ac.Parameter2 = GetNumericRefType(type);
            //            type = ConvertToInt(GetValue(7));
            //            ac.Parameter3 = GetNumericRefType(type);
            //            ActionList.Add($"action navpoint_set_visible_range {ac.Parameter1}{subvalue} {ac.Parameter2} {ac.Parameter3}");
            //            break;
            //        case 10:
            //            //Object Territory
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action NavPointSetIsTerritory {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 11:
            //            //Object Territory Team
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action NavPointSetIsSpawningTerritory {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 12:
            //            //Object Territory Level
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action NavPointSetTerritoryLevel {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 13:
            //            //Object territory max level
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action NavPointSetMaxTerritoryLevel {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 14:
            //            //Object Territory Sort Order
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action NavPointSetTerritorySortOrder {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 15:
            //            //Object Territory Cap Timer
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int timer2 = ConvertToInt(GetValue(1));
            //            ac.Parameter3 = timer2 == 0 ? Convert.ToString(ConvertToInt(GetValue(2))) : "none";
            //            ActionList.Add($"action NavPointSetTerritoryTimer {ac.Parameter1}{subvalue} ObjectTimer{ac.Parameter3}");
            //            break;
            //        case 16:
            //            //Object Nav Template
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = Names[ConvertToInt(GetValue(8))];
            //            ActionList.Add($"action NavPointSetType {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 17:
            //            //Object Action Team
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetTeamRefType(ConvertToInt(GetValue(3)));
            //            ActionList.Add($"action NavPointSetActionTeam {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2}");
            //            break;
            //        case 20:
            //            //Object Boundary Set
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int shape = ConvertToInt(GetValue(2));
            //            string len = "";
            //            string top = "";
            //            string bottom = "";
            //            string width = "";
            //            switch (shape)
            //            {
            //                case 0:
            //                    ac.Parameter2 = "none";
            //                    break;
            //                case 1:
            //                    ac.Parameter2 = "sphere";
            //                    len = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    break;
            //                case 2:
            //                    ac.Parameter2 = "cylinder";
            //                    len = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    bottom = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    top = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    break;
            //                case 3:
            //                    ac.Parameter2 = "box";
            //                    len = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    top = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    bottom = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    width = GetNumericRefType(ConvertToInt(GetValue(7)));
            //                    break;
            //            }
            //            ActionList.Add($"action set_boundary {ac.Parameter1}{subvalue} {ac.Parameter2} {len} {top} {width} {bottom} ");
            //            break;
            //        case 22:
            //            //Object Pickup Perms
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = PlayerSet[ConvertToInt(GetValue(3))];
            //            ActionList.Add($"action set_pickup_filter {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 23:
            //            //Object Spawn Perms
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = PlayerSet[ConvertToInt(GetValue(3))];
            //            ActionList.Add($"action set_respawn_filter {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 24:
            //            //Object Fireteam Spawn Perms
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int filter = ConvertToInt(GetValue(8));
            //            string filterString = "";
            //            switch (filter)
            //            {
            //                case 0:
            //                    filterString = "none";
            //                    break;
            //                case 1:
            //                    filterString = "0";
            //                    break;
            //                case 2:
            //                    filterString = "1";
            //                    break;
            //                case 4:
            //                    filterString = "2";
            //                    break;
            //                case 8:
            //                    filterString = "3";
            //                    break;
            //                case 255:
            //                    filterString = "all";
            //                    break;
            //            }
            //            ActionList.Add($"action set_fireteam_respawn_filter {ac.Parameter1}{subvalue} {filterString}");
            //            break;
            //        case 25:
            //            //Object Progress Bar
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = PlayerSet[ConvertToInt(GetValue(3))];
            //            int timer3 = ConvertToInt(GetValue(1));
            //            ac.Parameter3 = timer3 == 0 ? Convert.ToString(ConvertToInt(GetValue(2))) : "none";
            //            ActionList.Add($"action set_progress_bar {ac.Parameter1}{subvalue} {ac.Parameter2} ObjectTimer{ac.Parameter3}");
            //            break;
            //        case 30:
            //            //Object Carrier Get
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            type = ConvertToInt(GetValue(2));
            //            (ac.Parameter2, subvalue2) = GetPlayerRefType(type);
            //            ActionList.Add($"action get_player_holding_object {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2}");
            //            break;
            //        case 32:
            //            //begin or inline
            //            //Modify the last action string to start with begin and end with end
            //            int conditionOffset = ConvertToInt(GetValue(10));
            //            int conditionCount = ConvertToInt(GetValue(10));
            //            int unknown3 = ConvertToInt(GetValue(11));
            //            int unknown4 = ConvertToInt(GetValue(11));
            //            unknown4 = unknown4 == 0 ? 1 : unknown4;
            //            unknown3 = unknown3 == 0 ? 1 : unknown3;
                        
                        
            //            //ActionList[unknown3 + unknown4 - 2] += " \n\tend";
            //            for (int j = 0; j < conditionCount; j++)
            //            {
            //                int index = (int)ConditionOffsetList[j];

            //                // Ensure the index is within the bounds of the list
            //                if (index >= 0 && index <= ActionList.Count)
            //                {
            //                    string condition = ConditionsList[j + conditionOffset - 1];
            //                    ActionList[unknown4 - 1] = condition + "\n\t" + ActionList[unknown3 - 1];
            //                }
            //            }
            //            ActionList[unknown4 - 1] = "begin\n\t " + ActionList[unknown4 - 1];
            //            break;

            //        case 35:
            //            //Boundary Visibility Perms
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = PlayerSet[ConvertToInt(GetValue(3))];
            //            ActionList.Add($"action boundary_set_visible {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 36:
            //            //Object kill
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = ConvertToInt(GetValue(1)) == 1 ? "no_statistics" : "";
            //            ActionList.Add($"action object_destroy {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 37:
            //            //Object Set Invincible
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action set_object_invincible {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 40:
            //            //Get orientation
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_get_orientation {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 41:
            //            //Get speed
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_get_velocity {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 47:
            //            //Object attach
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int offsetX = ConvertToInt(GetValue(8));
            //            int offsetY = ConvertToInt(GetValue(8));
            //            int offsetZ = ConvertToInt(GetValue(8));
            //            string absolute = ConvertToInt(GetValue(1)) == 1 ? "absolute_orientation" : "";
            //            ActionList.Add($"action object_attach {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2} {offsetX} {offsetY} {offsetZ} {absolute}");
            //            break;
            //        case 48:
            //            //Object Detach
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ActionList.Add($"action object_detach {ac.Parameter1}{subvalue}");
            //            break;
            //        case 63:
            //            //Owner biped get
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ActionList.Add($"action objectGetImmediateParentPlayer {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2}");
            //            break;
            //        case 65:
            //            //Object Pickup Pirority
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int pickupMode = ConvertToInt(GetValue(2));
            //            switch (pickupMode)
            //            {
            //                case 0:
            //                    ac.Parameter2 = "normal";
            //                    break;
            //                case 1:
            //                    ac.Parameter2 = "special";
            //                    break;
            //                case 2:
            //                    ac.Parameter2 = "auto";
            //                    break;
            //            }
            //            ActionList.Add($"action weapon_set_pickup_priority {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 66:
            //            //Object push upwards
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ActionList.Add($"action object_bounce {ac.Parameter1}{subvalue}");
            //            break;
            //        case 74:
            //            //Object set scale
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_set_scale {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 75:
            //            //Navpoint set text
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            break;
            //        case 81:
            //            //Object get shields
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_get_shield {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 82:
            //            //Object get health
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_get_health {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 83:
            //            //Get health fraction
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action ObjectGetHealthAbsolute {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 92:
            //            //Object set shields
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            c.Oper = ConvertToInt(GetValue(4));
            //            string oper = "";
            //            switch (c.Oper)
            //            {
            //                case 0:
            //                    oper = "+=";
            //                    break;
            //                case 1:
            //                    oper = "-=";
            //                    break;
            //                case 2:
            //                    oper = "*=";
            //                    break;
            //                case 3:
            //                    oper = "/=";
            //                    break;
            //                case 4:
            //                    oper = "=";
            //                    break;
            //                case 5:
            //                    oper = "%=";
            //                    break;
            //                case 6:
            //                    oper = "&=";
            //                    break;
            //                case 7:
            //                    oper = "|=";
            //                    break;
            //                case 8:
            //                    oper = "^=";
            //                    break;
            //                case 9:
            //                    oper = "~=";
            //                    break;
            //            }
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_adjust_shield {ac.Parameter1}{subvalue} {oper} {ac.Parameter3}");
            //            break;
            //        case 94:
            //            //Object get distance
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            string dead = Convert.ToInt32(GetValue(1)) == 1 ? "allow_dead" : "";
            //            ActionList.Add($"action object_get_distance {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2} {ac.Parameter3} {dead}");
            //            break;
            //        case 95:
            //            //Max shields get
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            c.Oper = ConvertToInt(GetValue(4));
            //            string oper2 = "";
            //            switch (c.Oper)
            //            {
            //                case 0:
            //                    oper2 = "+=";
            //                    break;
            //                case 1:
            //                    oper2 = "-=";
            //                    break;
            //                case 2:
            //                    oper2 = "*=";
            //                    break;
            //                case 3:
            //                    oper2 = "/=";
            //                    break;
            //                case 4:
            //                    oper2 = "=";
            //                    break;
            //                case 5:
            //                    oper2 = "%=";
            //                    break;
            //                case 6:
            //                    oper2 = "&=";
            //                    break;
            //                case 7:
            //                    oper2 = "|=";
            //                    break;
            //                case 8:
            //                    oper2 = "^=";
            //                    break;
            //                case 9:
            //                    oper2 = "~=";
            //                    break;
            //            }
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_adjust_maximum_shield {ac.Parameter1}{subvalue} {oper2} {ac.Parameter3}");
            //            break;
            //        case 96:
            //            //Max health get
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            c.Oper = ConvertToInt(GetValue(4));
            //            string oper3 = "";
            //            switch (c.Oper)
            //            {
            //                case 0:
            //                    oper3 = "+=";
            //                    break;
            //                case 1:
            //                    oper3 = "-=";
            //                    break;
            //                case 2:
            //                    oper3 = "*=";
            //                    break;
            //                case 3:
            //                    oper3 = "/=";
            //                    break;
            //                case 4:
            //                    oper3 = "=";
            //                    break;
            //                case 5:
            //                    oper3 = "%=";
            //                    break;
            //                case 6:
            //                    oper3 = "&=";
            //                    break;
            //                case 7:
            //                    oper3 = "|=";
            //                    break;
            //                case 8:
            //                    oper3 = "^=";
            //                    break;
            //                case 9:
            //                    oper3 = "~=";
            //                    break;
            //            }
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_adjust_maximum_health {ac.Parameter1}{subvalue} {oper3} {ac.Parameter3}");
            //            break;
            //        case 98:
            //            //Device power set
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action device_set_power {ac.Parameter1}{subvalue} {ac.Parameter3}");
            //            break;
            //        case 99:
            //            //Device power get
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action device_get_power {ac.Parameter1}{subvalue} {ac.Parameter3}");
            //            break;
            //        case 100:
            //            //Device position set
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action device_set_position {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 101:
            //            //Device position get
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action device_get_position {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 106:
            //            //Device track set
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = Names[ConvertToInt(GetValue(8))];
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action device_set_track {ac.Parameter1}{subvalue} {ac.Parameter2} {ac.Parameter3}");
            //            break;
            //        case 107:
            //            //Set animation position
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            string anim = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            string duration = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            string accel = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            string decel = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action device_animate_position {ac.Parameter1}{subvalue} {anim} {duration} {accel} {decel}");
            //            break;
            //        case 108:
            //            //Device immediate set
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action device_set_position_immediate {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 110:
            //            //Set spawn zone enabled
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action respawn_zone_enable {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 113:
            //            //Object cleanup set
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action object_set_cleanup {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 119:
            //            //Object copy rotation
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int absolute2 = Convert.ToInt32(GetValue(1));
            //            ActionList.Add($"action object_copy_rotation {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2} {absolute2}");
            //            break;
            //        case 120:
            //            //Object face object
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int x = ConvertToInt(GetValue(8));
            //            int y = ConvertToInt(GetValue(8));
            //            int z = ConvertToInt(GetValue(8));
            //            ActionList.Add($"action object_face_object {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2} offset {x} {y} {z}");
            //            break;
            //        case 121:
            //            //Object give weapon
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = Objects[ConvertToInt(GetValue(objectType))];
            //            int mode = Convert.ToInt32(GetValue(2));
            //            string modeString = "";
            //            switch (mode)
            //            {
            //                case 0:
            //                    modeString = "primary";
            //                    break;
            //                case 1:
            //                    modeString = "secondary";
            //                    break;
            //                case 2:
            //                    modeString = "force";
            //                    break;
            //            }
            //            ActionList.Add($"action biped_give_weapon {ac.Parameter1}{subvalue} '{ac.Parameter2}' {modeString}");
            //            break;
            //        case 122:
            //            //Object drop weapon
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int mode2 = Convert.ToInt32(GetValue(2));
            //            string modeString2 = "";
            //            switch (mode2)
            //            {
            //                case 0:
            //                    modeString2 = "primary";
            //                    break;
            //                case 1:
            //                    modeString2 = "secondary";
            //                    break;
            //            }
            //            string delete = Convert.ToInt32(GetValue(1)) == 1 ? "delete_on_drop" : "";
            //            ActionList.Add($"action biped_drop_weapon {ac.Parameter1}{subvalue} {modeString2} {delete}");
            //            break;
            //        case 126:
            //            //Object shape player color set
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int playerIndex = ConvertToInt(GetValue(1)) == 0 ? ConvertToInt(GetValue(2)) : 1;
            //            ActionList.Add($"action boundary_set_player_color {ac.Parameter1}{subvalue}Player{playerIndex}");
            //            break;
            //        case 133:
            //            //Object hide
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            int hide = Convert.ToInt32(GetValue(1));
            //            ActionList.Add($"action hide_object {ac.Parameter1}{subvalue} {hide}");
            //            break;
            //        case 134:
            //            //Object set turret
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ac.Parameter3 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            string par4 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action SetAutoTurret {ac.Parameter1}{subvalue} {ac.Parameter2} {ac.Parameter3} {par4}");
            //            break;
            //        case 135:
            //            //Set turret range
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            ac.Parameter2 = GetNumericRefType(ConvertToInt(GetValue(7)));
            //            ActionList.Add($"action SetAutoTurretRange {ac.Parameter1}{subvalue} {ac.Parameter2}");
            //            break;
            //        case 145:
            //            //Device get user
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetPlayerRefType(ConvertToInt(GetValue(2)));
            //            ActionList.Add($"action DeviceGetPlayerUser {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2}");
            //            break;
            //        case 146:
            //            //Device get interracting player user
            //            (ac.Parameter1, subvalue) = GetRefType(ConvertToInt(GetValue(objectTypeRef)));
            //            (ac.Parameter2, subvalue2) = GetPlayerRefType(ConvertToInt(GetValue(2)));
            //            ActionList.Add($"action DeviceGetInteractingPlayerUser {ac.Parameter1}{subvalue} {ac.Parameter2}{subvalue2}");
            //            break;



            //        default:
            //            Debug.WriteLine("Failed to read action type: " + ac.ActionType);
            //            return;

            //    }
            //}



            ////Read Triggers
            //Triggers tr = new();
            //tr.TriggerCount = ConvertToInt(GetValue(8));
            //for (int i = 0; i < tr.TriggerCount; i++)
            //{
            //    tr.TriggerType = ConvertToInt(GetValue(3));
            //    string type = GetTriggerType(tr.TriggerType);
            //    int attribute = ConvertToInt(GetValue(4));
            //    string attributeString = GetTriggerAttribute(attribute);
            //    int conditionOffset = ConvertToInt(GetValue(10));
            //    int conditionCount = ConvertToInt(GetValue(10));
            //    int actionOffset = ConvertToInt(GetValue(11));
            //    int actionCount = ConvertToInt(GetValue(11));
            //    int unk1 = ConvertToInt(GetValue(8));
            //    int unk2 = ConvertToInt(GetValue(8));

            //    //Build trigger block

            //    string conditions = "";

            //    return;
            //    for (int j = 0; j < conditionCount; j++)
            //    {
            //        int index = (int)ConditionOffsetList[j];

            //        // Ensure the index is within the bounds of the list
            //        //if (index >= 0 && index <= ActionList.Count)
            //        //{
            //            //string condition = ConditionsList[j + conditionOffset - 1];
            //            //ActionList.Insert(index, condition);
            //        //}
            //    }


            //    //string actions = "";
            //    //for (int j = 0; j < actionCount; j++)
            //    //{
            //    //    actions += $"{ActionList[j + actionOffset - 1]}\n";
            //    //}

            //    //Build a string of actions and conditions using the sum of action and condition count
            //    string actions = "";
            //    for (int j = 0; j < actionCount + conditionCount; j++)
            //    {
            //        //actions += $"\t{ActionList[j]}\n";
            //    }


                

            //    //Build trigger string
            //    string trigger = type + "\n" + actions + "\n" + "end\n";
            //    //Remove the first entries from ActionsList equal to the action and condition count
            //    //ActionList.RemoveRange(0, actionCount + conditionCount);

            //    //Get the filename from the filepath variable
            //    string filename = Path.GetFileNameWithoutExtension(filePath);
            //    //Get only the file path without the filename
            //    string filepath = Path.GetDirectoryName(filePath);
            //    //Append the trigger to a new text file with the filename at the file pat locaation and overwrite the existing
                
            //    File.AppendAllText($"{filepath}\\{filename}.txt", trigger);
            //}



        }


        private (string, string) GetVarType(int value)
        {
            string SpecificType = "";
            string subplayer = "";
            switch (value)
            {
                case 0:
                    SpecificType = GetNumericRefType(ConvertToInt(GetValue(7)));
                    break;
                case 1:
                    int type = ConvertToInt(GetValue(2));
                    switch (type)
                    {
                        case 0:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            break;
                        case 1:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 2:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 3:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                    }
                    break;
                case 2:
                    type = ConvertToInt(GetValue(3));
                    switch (type)
                    {
                        case 0:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            break;
                        case 1:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 2:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 3:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            break;
                        case 4:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 5:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 6:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                    }
                    break;
                case 3:
                    type = ConvertToInt(GetValue(2));
                    switch (type)
                    {
                        case 0:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            break;
                        case 1:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 2:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 3:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 4:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            break;
                        case 5:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            break;
                    }
                    break;
                case 4:
                    type = ConvertToInt(GetValue(2));
                    switch (type)
                    {
                        case 0:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            break;
                        case 1:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 2:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 3:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 4:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            break;
                        case 5:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            break;
                    }
                    break;
                case 5:
                    type = ConvertToInt(GetValue(2));
                    switch (type)
                    {
                        case 0:
                            SpecificType = $"GlobalTimer{ConvertToInt(GetValue(3))}";
                            break;
                        case 1:
                            SpecificType = PlayerTypeRef[ConvertToInt(GetValue(6))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 2:
                            SpecificType = TeamTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                        case 3:
                            SpecificType = ObjectTypeRef[ConvertToInt(GetValue(5))];
                            subplayer = Convert.ToString(ConvertToInt(GetValue(2)));
                            break;
                    }
                    break;
                    
            }
            return (SpecificType, subplayer);
        }


        private string GetTriggerAttribute(int value)
        {
            switch (value)
            {
                case 0:
                    return "";
                case 1:
                    return "call";
                case 2:
                    return "trigger initialization";
                case 3:
                    return "trigger local_initialization";
                case 4:
                    return "trigger host_migration";
                case 5:
                    return "trigger object_death";
                case 6:
                    return "trigger local";
                case 7:
                    return "trigger pregame";
                case 8:
                    return "trigger incident";
                default:
                    return "Unknown Trigger Attribute";
            }
        }

        private string GetTriggerType(int value)
        {
            switch (value)
            {
                case 0:
                    return "trigger general";
                case 1:
                    return "trigger player";
                case 2:
                    return "trigger random_player";
                case 3:
                    return "trigger team";
                case 4:
                    return "trigger object";
                case 5:
                    return "trigger label";
                case 6:
                    return "trigger filter";
                default:
                    return "Unknown Trigger Type";
                
            }
        }
        private string GetNumericRefType(int value)
        {
            switch(value)
            {
                case 0:
                    return Convert.ToString(ConvertToInt(GetValue(16)));
                case 1:
                    string player = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return  player + ".Num" + Convert.ToString(ConvertToInt(GetValue(4)));
                case 2:
                    string obj = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    return obj + ".Num" + Convert.ToString(ConvertToInt(GetValue(4)));
                case 3:
                    string team = TeamTypeRef[ConvertToInt(GetValue(5))];
                    return team + ".Num" + Convert.ToString(ConvertToInt(GetValue(4)));
                case 4:
                    return "Global.Num" + Convert.ToString(ConvertToInt(GetValue(5)));
                case 5:
                    return ScratchNumbers[ConvertToInt(GetValue(4))];
                case 6:
                    return "ScriptOption.Option" + Convert.ToString(ConvertToInt(GetValue(4)));
                case 7:
                    string obj2 = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    return obj2 + ".SpawnSeq";
                case 8:
                    string obj3 = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    return obj3 + ".UserData";
                case 9:
                    string obj4 = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    return obj4 + ".Unk9";
                case 10:
                    string team2 = TeamTypeRef[ConvertToInt(GetValue(5))];
                    return team2 + ".Score";
                case 11:
                    string player2 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player2 + ".Score";
                case 12:
                    string player3 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player3 + ".Money";
                case 13:
                    string player4 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player4 + ".Rating";
                case 14:
                    string obj5 = PlayerTypeRef[ConvertToInt(GetValue(5))];
                    return obj5 + ".Stat" + Convert.ToString(ConvertToInt(GetValue(4)));
                case 15:
                    string team3 = TeamTypeRef[ConvertToInt(GetValue(5))];
                    return team3 + ".Stat" + Convert.ToString(ConvertToInt(GetValue(4)));
                case 16:
                    return "Unk16";
                case 17:
                    return "CurrentRound";
                case 18:
                    return "SymmetricMode";
                case 19:
                    return "SymmetricModeWritable";
                case 20:
                    return "Gamemode Controls Victory Enabled";
                case 21:
                    return "score_to_win_this_round";
                case 22:
                    string team4 = TeamTypeRef[ConvertToInt(GetValue(5))];
                    return team4 + ".remaining_lives";
                case 23:
                    string player5 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player5 + ".remaining_lives";
                case 24:
                    string player6 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player6 + ".spawn_delay";
                case 25:
                    string player7 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player7 + ".UnkVal25";
                case 26:
                    string player8 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player8 + ".UnkVal26";
                case 27:
                    string player9 = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    return player9 + ".UnkVal27";
                case 28:
                    string team5 = TeamTypeRef[ConvertToInt(GetValue(5))];
                    return team5 + ".UnkVal28";
                case 29:
                    string team6 = TeamTypeRef[ConvertToInt(GetValue(5))];
                    return team6 + ".UnkVal29";
                case 30:
                    string team7 = TeamTypeRef[ConvertToInt(GetValue(5))];
                    return team7 + ".UnkVal30";
                case 31:
                    string obj6 = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    return obj6 + ".UnkVal31";
                case 32:
                    string obj7 = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    return obj7 + ".UnkVal32";
                case 33:
                    return "score_to_win";
                case 34:
                    return "Fireteams Enabled";
                case 35:
                    return "Teams Enabled";
                case 36:
                    return "Round Time Limit";
                case 37:
                    return "Round Limit";
                case 38:
                    return "Perfection Enabled";
                case 39:
                    return "Early Victory Win Count";
                case 40:
                    return "Player.Lives";
                case 41:
                    return "Team.Lives";
                case 42:
                    return "RespawnTime";
                case 43:
                    return "Suicide Respawn Penalty";
                case 44:
                    return "Betrayal Respawn Penalty";
                case 45:
                    return "Respawn Growth Time";
                case 46:
                    return "Initial Loadout Selection Time";
                case 47:
                    return "Respawn Traits Duration";
                case 48:
                    return "Friendly Fire Enabled";
                case 49:
                    return "Betrayal Booting Enabled";
                case 50:
                    return "Enemy Voice Enabled";
                case 51:
                    return "Open Channel Voice Enabled";
                case 52:
                    return "Dead Player Voice Enabled";
                case 53:
                    return "Grenades on Map";
                case 54:
                    return "Indestructible Vehicles Enabled";
                case 55:
                    return "Damage Boost Traits Duration";
                case 56:
                    return "Speed Boost Traits Duration";
                case 57:
                    return "Overshield Traits Duration";
                case 58:
                    return "Custom Traits Duration";
                case 59:
                    return "Damage Boost Traits DurationRuntime";
                case 60:
                    return "Speed Boost Traits DurationRuntime";
                case 61:
                    return "Overshield Traits DurationRuntime";
                case 62:
                    return "Custom Traits DurationRuntime";
                case 63:
                    return "Map Loadouts Enabled";
                case 64:
                    return "Initial Ordance Enabled";
                case 65:
                    return "Random Ordance Enabled";
                case 66:
                    return "Object Ordance Enabled";
                case 67:
                    return "Personal Ordance Enabled";
                case 68:
                    return "Ordance Enabled";
                case 69:
                    return "Killcam Enabled";
                case 70:
                    return "Final Killcam Enabled";
                case 71:
                    return "Sudden Death Time Limit";
                case 72:
                    return "Object Death Damage Type";
                default:
                    return "Unlabelled";
            }
        }

 
        private (string, string) GetRefType(int type)
        {
            string subvalue = "";
            string value = "";
            switch (type)
            {
                case 0:
                    value = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    break;
                case 1:
                    value = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 2:
                    value = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 3:
                    value = TeamTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 4:
                    value = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    break;
                case 5:
                    value = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 6:
                    value = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 7:
                    value = TeamTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;

            }
            return (value, subvalue);
        }

        private (string, string) GetTeamRefType(int type)
        {
            string subvalue = "";
            string value = "";
            switch (type)
            {
                case 0:
                    value = TeamTypeRef[ConvertToInt(GetValue(5))];
                    break;
                case 1:
                    value = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 2:
                    value = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 3:
                    value = TeamTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 4:
                    value = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    subvalue = ".team";
                    break;
                case 5:
                    value = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = ".team";
                    break;

            }
            return (value, subvalue);
        }

        private (string, string) GetPlayerRefType(int type)
        {
            string subvalue = "";
            string value = "";
            switch (type)
            {
                case 0:
                    value = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    break;
                case 1:
                    value = PlayerTypeRef[ConvertToInt(GetValue(6))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 2:
                    value = ObjectTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
                case 3:
                    value = TeamTypeRef[ConvertToInt(GetValue(5))];
                    subvalue = "." + Convert.ToString(ConvertToInt(GetValue(2)));
                    break;
            }
            return (value, subvalue);
        }





        private OrdnanceWeights ReadOrdnanceWeights()
        {

            OrdnanceWeights ow = new();
            ow.PosibilityName1 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight1 = ConvertToInt(GetValue(30));
            ow.PosibilityName2 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight2 = ConvertToInt(GetValue(30));
            ow.PosibilityName3 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight3 = ConvertToInt(GetValue(30));
            ow.PosibilityName4 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight4 = ConvertToInt(GetValue(30));
            ow.PosibilityName5 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight5 = ConvertToInt(GetValue(30));
            ow.PosibilityName6 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight6 = ConvertToInt(GetValue(30));
            ow.PosibilityName7 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight7 = ConvertToInt(GetValue(30));
            ow.PosibilityName8 = ReadStringFromBits(binaryString, true);
            ow.PossibilityWeight8 = ConvertToInt(GetValue(30));
            return ow;






        }

        private LoadoutOptions ReadLoadoutOptions()
        {
            LoadoutOptions lo = new();
            lo.LoadoutVisibleingame = ConvertToInt(GetValue(1));
            lo.LoadoutName = ConvertToInt(GetValue(1)) == 0 ? ConvertToInt(GetValue(7)) : -1;
            lo.PrimaryWeapon = ConvertToInt(GetValue(8));
            lo.SecondaryWeapon = ConvertToInt(GetValue(8));
            lo.Armorability = ConvertToInt(GetValue(8));
            if (Settings.Default.DecompiledVersion > 0)
            {
                lo.TacticalPackage = ConvertToInt(GetValue(8));
                lo.SupportUpgrade = ConvertToInt(GetValue(8));
                lo.Grenades = ConvertToInt(GetValue(5));
                lo.Unknown = ConvertToInt(GetValue(6));
            }

            if (Settings.Default.DecompiledVersion == 0)
            {
                lo.Grenades = ConvertToInt(GetValue(4));
            }

            return lo;

        }

        private TeamOptions ReadTeaMOptions()
        {
            TeamOptions to = new();
            if (Settings.Default.DecompiledVersion == 0)
            {
                to.TertiarycolorOverride = ConvertToInt(GetValue(1));
                to.SecondarycolorOverride = ConvertToInt(GetValue(1));
                to.PrimarycolorOverride = ConvertToInt(GetValue(1));
                to.TeamEnabled = ConvertToInt(GetValue(1));
                
                to.Teamstring = ReadLangStrings(5, 1, true);
                to.InitialDesignator = ConvertToInt(GetValue(4));
                to.Elitespecies = ConvertToInt(GetValue(1));
                to.PrimaryColor = ConvertToInt(GetValue(32));
                to.SecondaryColor = ConvertToInt(GetValue(32));
                to.TertiaryColor = ConvertToInt(GetValue(32));
                to.FireteamCount = ConvertToInt(GetValue(5));
            }

            if (Settings.Default.DecompiledVersion > 0)
            {
                to.H2AH4 = new();
                to.H2AH4.EmblemOverride = ConvertToInt(GetValue(1));
                to.H2AH4.Unknown1 = ConvertToInt(GetValue(1));
                to.TertiarycolorOverride = ConvertToInt(GetValue(1));
                to.SecondarycolorOverride = ConvertToInt(GetValue(1));
                to.PrimarycolorOverride = ConvertToInt(GetValue(1));
                to.TeamEnabled = ConvertToInt(GetValue(1));
                //to.Unknown2 = ConvertToInt(GetValue(2));
                to.Teamstring = ReadLangStrings(10, 1, false);
                to.InitialDesignator = ConvertToInt(GetValue(4));
                to.Elitespecies = ConvertToInt(GetValue(1));
                to.PrimaryColor = ConvertToInt(GetValue(32));
                to.SecondaryColor = ConvertToInt(GetValue(32));
                to.H2AH4.TextColor = ConvertToInt(GetValue(32));
                to.H2AH4.InterfaceColor = ConvertToInt(GetValue(32));
                to.FireteamCount = ConvertToInt(GetValue(5));
                to.H2AH4.EmblemForeground = ConvertToInt(GetValue(8));
                to.H2AH4.EmblemBackground = ConvertToInt(GetValue(8));
                to.H2AH4.EmblemUnknown = ConvertToInt(GetValue(3));
                to.H2AH4.EmblemPrimaryColor = ConvertToInt(GetValue(6));
                to.H2AH4.EmblemSecondaryColor = ConvertToInt(GetValue(6));
                to.H2AH4.EmblemBackgroundColor = ConvertToInt(GetValue(6));

            }

            return to;
        }

        private LanguageStrings ReadLangStrings(int bits, int chars, bool teamString)
        {
            int stringPresent = ConvertToInt(GetValue(chars));
            List<LanguageStrings> indexes = new();
            LanguageStrings ls = new();
            for (int i=0; i<stringPresent; i++) 
            { 
                ls.English = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Japanese = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.German = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.French = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Spanish = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.LatinAmericanSpanish = ConvertToInt(GetValue(1)) == 0 ? "1" : GetValue(bits);
                ls.Italian = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Korean = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.ChineseTraditional = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.ChineseSimplified = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Portuguese = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Polish = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                if (Settings.Default.DecompiledVersion > 0)
                {
                    ls.H2AH4 = new();
                    ls.H2AH4.Russian = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                    ls.H2AH4.Danish = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                    ls.H2AH4.Finnish = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                    ls.H2AH4.Dutch = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                    ls.H2AH4.Norwegian = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                }
                indexes.Add(ls);
            }
            
            
            
            string compressedChunk = "";
            bool compression = true;

            if (stringPresent > 0)
            {
                int m3;
                if (teamString && Settings.Default.DecompiledVersion == 0)
                {
                    m3 = ConvertToInt(GetValue(bits+1));
                }
                else
                {
                    //GetValue(1);
                    m3 = ConvertToInt(GetValue(bits));
                }
                    
                int d = ConvertToInt(GetValue(1));
                if (d == 0)
                {
                    compressedChunk = GetValue(m3*8);
                    compression = false;
                }
                else
                {
                    int m1 = ConvertToInt(GetValue(bits));
                    string b = ConvertToHex(GetValue(m1 * 8));

                    
                    byte[] b2 = Convert.FromHexString(b);
                    var bytes = LowLevelDecompress(b2, m3);
                    //convert to hex string
                    compressedChunk = BitConverter.ToString(bytes).Replace("-", "");
                    //Convert compressedChunk to binary
                    compressedChunk = ConvertToBinary(compressedChunk);
                }   
            }

                
            for (int i=0;i<indexes.Count; i++)
            {
                LanguageStrings currentString = indexes[i];

                string result = "";
                result = currentString.English == "-1" ? currentString.English = "" : FindLangString(currentString.English, compressedChunk);
                currentString.English = result;
                result = currentString.French == "-1" ? currentString.French = "" : FindLangString(currentString.French, compressedChunk);
                currentString.French = result;
                result = currentString.Spanish == "-1" ? currentString.Spanish = "" : FindLangString(currentString.Spanish, compressedChunk);
                currentString.Spanish = result;
                result = currentString.LatinAmericanSpanish == "-1" ? currentString.LatinAmericanSpanish = "" : FindLangString(currentString.LatinAmericanSpanish, compressedChunk);
                currentString.LatinAmericanSpanish = result;
                result = currentString.German == "-1" ? currentString.German = "" : FindLangString(currentString.German, compressedChunk);
                currentString.German = result;
                result = currentString.Italian == "-1" ? currentString.Italian = "" : FindLangString(currentString.Italian, compressedChunk);
                currentString.Italian = result;
                result = currentString.Korean == "-1" ? currentString.Korean = "" : FindLangString(currentString.Korean, compressedChunk);
                currentString.Korean = result;
                result = currentString.Japanese == "-1" ? currentString.Japanese = "" : FindLangString(currentString.Japanese, compressedChunk);
                currentString.Japanese = result;
                result = currentString.ChineseTraditional == "-1" ? currentString.ChineseTraditional = "" : FindLangString(currentString.ChineseTraditional, compressedChunk);
                currentString.ChineseTraditional = result;
                result = currentString.ChineseSimplified == "-1" ? currentString.ChineseSimplified = "" : FindLangString(currentString.ChineseSimplified, compressedChunk);
                currentString.ChineseSimplified = result;
                result = currentString.Portuguese == "-1" ? currentString.Portuguese = "" : FindLangString(currentString.Portuguese, compressedChunk);
                currentString.Portuguese = result;
                result = currentString.Polish == "-1" ? currentString.Polish = "" : FindLangString(currentString.Polish, compressedChunk);
                currentString.Polish = result;
                if (Settings.Default.DecompiledVersion > 0)
                {
                    result = currentString.H2AH4.Russian == "-1" ? currentString.H2AH4.Russian = "" : FindLangString(currentString.H2AH4.Russian, compressedChunk);
                    currentString.H2AH4.Russian = result;
                    result = currentString.H2AH4.Danish == "-1" ? currentString.H2AH4.Danish = "" : FindLangString(currentString.H2AH4.Danish, compressedChunk);
                    currentString.H2AH4.Danish = result;
                    result = currentString.H2AH4.Finnish == "-1" ? currentString.H2AH4.Finnish = "" : FindLangString(currentString.H2AH4.Finnish, compressedChunk);
                    currentString.H2AH4.Finnish = result;
                    result = currentString.H2AH4.Dutch == "-1" ? currentString.H2AH4.Dutch = "" : FindLangString(currentString.H2AH4.Dutch, compressedChunk);
                    currentString.H2AH4.Dutch = result;
                    result = currentString.H2AH4.Norwegian == "-1" ? currentString.H2AH4.Norwegian = "" : FindLangString(currentString.H2AH4.Norwegian, compressedChunk);
                    currentString.H2AH4.Norwegian = result;
                }

                return currentString;
            }


            return ls;
        }

        public string ConvertToHex(string bits)
        {
            return string.Concat(bits
                .Select((c, index) => new { c, index })
                .GroupBy(x => x.index / 8)
                .Select(g => Convert.ToByte(new string(g.Select(x => x.c).ToArray()), 2).ToString("X2")));
        }


        private string FindLangString(string rawBits, string compressedChunk)
        {

            return ReadStringFromBits(compressedChunk, false);
            
            //bool searching = true;
            //string hexString = "";
            //int depth = ConvertToInt(rawBits);

            //while (searching)
            //{
            //    string currentByte = compressedChunk.Substring(depth*2, 2);
            //    depth++;
            //    if (currentByte == "00")
            //    {
            //        searching = false;
            //    }
            //    else
            //    {
            //        hexString += currentByte;
            //    }
            //}
            //var bytes = new byte[hexString.Length / 2];
            //for (var i = 0; i < bytes.Length; i++)
            //{
            //    string s = hexString.Substring(i * 2, 2);
            //    bytes[i] = Convert.ToByte(s, 16);
            //}
            //return Encoding.UTF8.GetString(bytes);

        }


        private string ConvertToBinary(string hex)
        {
            //Convert hex to binary
            string binary = string.Join(string.Empty,
            hex.Select(
                c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );
            return binary;
        }



        public static byte[] LowLevelDecompress(byte[] bytes, int uncompressedSize, int skipHeaderLength = sizeof(uint))
        {

            byte[] result = new byte[uncompressedSize];
            var zip = new Inflater();
            {
                zip.SetInput(bytes, skipHeaderLength, bytes.Length - skipHeaderLength); // skip the decompressed size header
                zip.Inflate(result);
            }
            return result;
        }



        private PlayerTraits ReadTraits(string binary, PlayerTraits pt)
        {
            //Use the PlayerTraits struct to read the traits
            //Return the traits

            //PlayerTraits pt = new();
            if (Settings.Default.DecompiledVersion == 0)
            {
                pt.DamageResistance = ConvertToInt(GetValue(4));
                pt.Healthmultiplyer = ConvertToInt(GetValue(3));
                pt.Healthregenrate = ConvertToInt(GetValue(4));
                pt.ShieldMultiplyer = ConvertToInt(GetValue(3));
                pt.ShieldRegenrate = ConvertToInt(GetValue(4));
                pt.Overshieldregenrate = ConvertToInt(GetValue(4));
                pt.HeadshotImmunity = ConvertToInt(GetValue(2));
                pt.shieldvampirism = ConvertToInt(GetValue(3));
                pt.Assasinationimmunity = ConvertToInt(GetValue(2));
                pt.invincible = ConvertToInt(GetValue(2));
                pt.WeaponDamagemultiplier = ConvertToInt(GetValue(4));
                pt.MeleeDamagemultiplier = ConvertToInt(GetValue(4));
                pt.Primaryweapon = ConvertToInt(GetValue(8));
                pt.Secondaryweapon = ConvertToInt(GetValue(8));
                pt.Grenades = ConvertToInt(GetValue(4));
                pt.Infiniteammo = ConvertToInt(GetValue(2));
                pt.Grenaderegen = ConvertToInt(GetValue(2));
                pt.WeaponPickup = ConvertToInt(GetValue(2));
                pt.AbilityUsage = ConvertToInt(GetValue(2));
                pt.Abilitiesdropondeath = ConvertToInt(GetValue(2));
                pt.InfiniteAbility = ConvertToInt(GetValue(2));
                pt.ArmorAbility = ConvertToInt(GetValue(8));
                pt.MovementSpeed = ConvertToInt(GetValue(5));
                pt.Playergravity = ConvertToInt(GetValue(4));
                pt.VehicleUse = ConvertToInt(GetValue(4));
                pt.Unknown = ConvertToInt(GetValue(2));
                pt.JumpHeight = ConvertToInt(GetValue(1));
                if (pt.JumpHeight == 1)
                {
                    pt.JumpOverride = ConvertToInt(GetValue(9));
                }
                pt.Camo = ConvertToInt(GetValue(3));
                pt.Visiblewaypoint = ConvertToInt(GetValue(2));
                pt.VisibleName = ConvertToInt(GetValue(2));
                pt.Aura = ConvertToInt(GetValue(3));
                pt.Forcedcolor = ConvertToInt(GetValue(4));
                pt.Motiontrackermode = ConvertToInt(GetValue(3));
                pt.MotiontrackerRange = ConvertToInt(GetValue(3));
                pt.DirectionalDamageindicator = ConvertToInt(GetValue(2));

            }

            if (Settings.Default.DecompiledVersion > 0)
            {
                pt.H2AH4 = new();
                pt.DamageResistance = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.ShieldMultiplyer = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.Healthmultiplyer = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.shieldstunduration = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.ShieldRegenrate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.Healthregenrate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.Overshieldregenrate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.shieldvampirism = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.explosivedamageresistance = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.wheelmanvehiclestuntime = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.wheelmanvehiclerechargetime =  ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.wheelmanvehicleemp = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.falldamage = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.HeadshotImmunity = ConvertToInt(GetValue(2));
                pt.Assasinationimmunity = ConvertToInt(GetValue(2));
                pt.invincible = ConvertToInt(GetValue(2));
                pt.H2AH4.fasttrackarmor = ConvertToInt(GetValue(2));
                pt.H2AH4.powerupcancelation = ConvertToInt(GetValue(2));
                pt.WeaponDamagemultiplier = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.MeleeDamagemultiplier = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.GrenadeRechargeFrag = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.GrenadeRechargePlasma = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.GrenadeRechargeSpike = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.HeroEquipmentEnergyUse = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.HeroEquipmentEnergyRechargeDelay = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.HeroEquipmentEnergyRechargeRate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.HeroEquipmentInitialEnergy = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.EquipmentEnergyUse = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.EquipmentEnergyRechargeDelay = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.EquipmentEnergyRechargeRate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.EquipmentInitialEnergy = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.SwitchSpeed = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.ReloadSpeed = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.OrdinancePoints = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.ExplosiveAOE = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.GunnerArmor = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.StabilityArmor = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.DropReconWarning = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.DropReconDistance = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.AssassinationSpeed = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.WeaponPickup = ConvertToInt(GetValue(2));
                pt.Grenades = ConvertToInt(GetValue(5));
                pt.Infiniteammo = ConvertToInt(GetValue(2));
                pt.AbilityUsage = ConvertToInt(GetValue(2));
                pt.H2AH4.UsageSansAutoTurret = ConvertToInt(GetValue(2));
                pt.Abilitiesdropondeath = ConvertToInt(GetValue(2));
                pt.InfiniteAbility = ConvertToInt(GetValue(2));
                pt.H2AH4.AmmoPack = ConvertToInt(GetValue(2));
                pt.H2AH4.Grenadier = ConvertToInt(GetValue(2));
                pt.H2AH4.DropGrenadeDeath = ConvertToInt(GetValue(2));
                pt.H2AH4.OrdinanceMarkerVisibility = ConvertToInt(GetValue(2));
                pt.H2AH4.ScavengeGrenades = ConvertToInt(GetValue(2));
                pt.H2AH4.Firepower = ConvertToInt(GetValue(2));
                pt.H2AH4.OrdinanceReroll = ConvertToInt(GetValue(2));
                pt.H2AH4.OrdinanceDisabled = ConvertToInt(GetValue(2));
                pt.Primaryweapon = ConvertToInt(GetValue(8));
                pt.Secondaryweapon = ConvertToInt(GetValue(8));
                pt.ArmorAbility = ConvertToInt(GetValue(8));
                pt.H2AH4.TacticalPackage = ConvertToInt(GetValue(8));
                pt.H2AH4.SupportPackage = ConvertToInt(GetValue(8));
                pt.H2AH4.Speed = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.Gravity = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.JumpHeight = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.TurnSpeed = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.VehicleUse = ConvertToInt(GetValue(4));
                pt.H2AH4.DoubleJump = ConvertToInt(GetValue(2));
                pt.H2AH4.Sprint = ConvertToInt(GetValue(2));
                pt.H2AH4.AutoMomentum = ConvertToInt(GetValue(2));
                pt.H2AH4.Vaulting = ConvertToInt(GetValue(2));
                pt.H2AH4.Stealthy = ConvertToInt(GetValue(2));
                pt.H2AH4.Unknown2 = ConvertToInt(GetValue(1));
                pt.H2AH4.Unknown3 = ConvertToInt(GetValue(1));
                pt.H2AH4.Scale = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.Camo = ConvertToInt(GetValue(3));
                pt.Visiblewaypoint = ConvertToInt(GetValue(2));
                pt.VisibleName = ConvertToInt(GetValue(2));
                pt.Aura = ConvertToInt(GetValue(3));
                pt.H2AH4.ForcedPrimaryColor = ConvertToInt(GetValue(5));
                pt.H2AH4.ForcedSecondaryColor = ConvertToInt(GetValue(5));
                pt.H2AH4.OverridePrimaryColor = ConvertToInt(GetValue(1));
                pt.H2AH4.PrimaryRed = ConvertToInt(GetValue(8));
                pt.H2AH4.PrimaryGreen = ConvertToInt(GetValue(8));
                pt.H2AH4.PrimaryBlue = ConvertToInt(GetValue(8));
                pt.H2AH4.OverrideSecondaryColor = ConvertToInt(GetValue(1));
                pt.H2AH4.SecondaryRed = ConvertToInt(GetValue(8));
                pt.H2AH4.SecondaryGreen = ConvertToInt(GetValue(8));
                pt.H2AH4.SecondaryBlue = ConvertToInt(GetValue(8));
                pt.H2AH4.OverridePlayerModel = ConvertToInt(GetValue(1));
                pt.H2AH4.Name = ConvertToInt(GetValue(8));
                pt.H2AH4.DeathEffect = ConvertToInt(GetValue(32));
                pt.H2AH4.LoopingEffect = ConvertToInt(GetValue(32));
                pt.H2AH4.ShieldHud = ConvertToInt(GetValue(2));
                pt.MotiontrackerRange = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.NemesisDuration = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                if (Settings.Default.IsGvar == false)
                {
                    pt.H2AH4.Unknown4 = ConvertToInt(GetValue(1));
                }
                pt.H2AH4.MotionTrackerEnabled = ConvertToInt(GetValue(3));
                pt.H2AH4.MotionTrackerUsageZoomed = ConvertToInt(GetValue(2));
                pt.DirectionalDamageindicator = ConvertToInt(GetValue(2));
                pt.H2AH4.VisionMode = ConvertToInt(GetValue(2));
                pt.H2AH4.BattleAwareness = ConvertToInt(GetValue(2));
                pt.H2AH4.ThreadView = ConvertToInt(GetValue(2));
                pt.H2AH4.Aura2 = ConvertToInt(GetValue(2));
                pt.H2AH4.Nemesis = ConvertToInt(GetValue(2));
            }

            return pt;
        }

        private string GetValue(int bits)
        {
            string value = binaryString.Substring(0, bits);
            binaryString = binaryString.Substring(bits);
            return value;
        }

        private string ReadStringFromBits(string binary, bool countForward)
        {
            //Read the string from the binary 8 characters at a time and return the string value for those 8 characters
            //Stop reading and combine the strings when the binary value is 00000000
            //Return the string value

            string value = "";
            string parse = binary;
            while (parse.Length >= 8 && parse.Substring(0, 8) != "00000000")
            {
                string binaryChar = parse.Substring(0, 8);
                parse = parse.Substring(8);
                value += ConvertToASCII(binaryChar);
            }
            if (countForward)
            {
                binaryString = parse.Substring(8);
            }
            return value;
        }

        private string ReadUStringFromBits(string binary)
        {
            //The string to be read is a null-terminated string of 16-bit ASCII characters
            //Read the string from the binary 8 characters at a time and skip the value if it is 00000000
            //Stop reading and combine the strings when the binary value is 0000000000000000
            //Return the string value

            string value = "";
            string parse = binary;
            while (parse.Length >= 16 && parse.Substring(0, 16) != "0000000000000000")
            {
                string binaryChar = parse.Substring(0, 8);
                parse = parse.Substring(8);
                if (binaryChar != "00000000")
                {
                    value += ConvertToASCII(binaryChar);
                }
            }
            binaryString = parse.Substring(16);
            return value;
        }



        static string GetBinaryString(byte[] binaryData, int offset, int bits)
        {
            StringBuilder binaryString = new StringBuilder(bits);

            for (int i = 0; i < bits; i++)
            {
                int byteIndex = offset + (i / 8);
                int bitIndex = i % 8;

                byte currentByte = binaryData[byteIndex];
                int bitValue = (currentByte >> (7 - bitIndex)) & 1;

                binaryString.Append(bitValue);
            }

            return binaryString.ToString();
        }

        static int ConvertToInt(string binaryString)
        {
            return Convert.ToInt32(binaryString, 2);
        }

        static string ConvertToASCII(string binaryString)
        {
            int numChars = binaryString.Length / 8;
            byte[] bytes = new byte[numChars];

            for (int i = 0; i < numChars; i++)
            {
                string byteString = binaryString.Substring(i * 8, 8);
                bytes[i] = Convert.ToByte(byteString, 2);
            }

            return Encoding.ASCII.GetString(bytes);
        }

        static string ConvertToUnicode(string binaryString)
        {
            int numChars = binaryString.Length / 16;
            byte[] bytes = new byte[numChars * 2];

            for (int i = 0; i < numChars; i++)
            {
                string byteString = binaryString.Substring(i * 16, 16);
                bytes[i * 2] = Convert.ToByte(byteString.Substring(0, 8), 2);
                bytes[i * 2 + 1] = Convert.ToByte(byteString.Substring(8, 8), 2);
            }

            return Encoding.Unicode.GetString(bytes);
        }





}
}
