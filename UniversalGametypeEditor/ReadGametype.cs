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

        public enum IconEnum
        {
            Flag = 0,
            Slayer = 1,
            Skull = 2,
            KOTH = 3,
            Juggernaut = 4,
            Territories = 5,
            Assault = 6,
            Infection = 7,
            VIP = 8,
            Invasion = 9,
            EliteandSkull = 10,
            Stockpile = 11,
            ActionSack = 12,
            Lightning = 13,
            Rocket = 14,
            Griffball = 15,
            Biohazard = 16,
            Headhunter = 17,
            SpikySwirl = 18,
            Wings = 19,
            Swirl = 20,
            Castle = 21,
            Plus = 22,
            Shield = 23,
            Arrow = 24,
            Infinity = 25,
            EightBall = 26,
            UNSC = 27,
            Unknown = 28,
            Diamond = 29
        }

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
                public int? GracePeriod;
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
            public int? BetrayalBooting;
            public int? ProximityVoice;
            public int Dontrestrictteamvoicechat;
            public int? allowdeadplayerstotalk;
            public int Indestructiblevehicles;
            public int turretsonmap;
            public int powerupsonmap;
            public int abilitiesonmap;
            public int shortcutsonmap;
            public int? grenadesonmap;
            public PlayerTraits BasePlayerTraits { get; set;}
            public int WeaponSet;
            public int VehicleSet;
            public int? EquipmentSet;
            public int? Unknown1;
            public int? Unknown2;
            public int? Unknown3;
            public string? Unknown4;


            

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

        public struct Conditions
        {
            public int ConditionCount;
            public int ConditionType;
        }

        public struct Actions
        {
            public int ActionCount;
            public int ActionType;
        }

        public struct Triggers
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
            } else
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
                ms.Reach = new()
                {
                    GracePeriod = ConvertToInt(GetValue(5))
                };
                ms.SuddenDeathTime = ConvertToInt(GetValue(7));
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
            ss.RespawnPlayerTraits = ReadTraits(binaryString, ss.RespawnPlayerTraits);
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

                gs.Unknown1 = null;
                gs.Unknown2 = null;
                gs.Unknown3 = null;
                gs.EquipmentSet = null;
                gs.Unknown4 = null;
            }

            if (Settings.Default.DecompiledVersion > 0)
            {
                gs.EnableObservers = ConvertToInt(GetValue(1));
                gs.Teamchanging = ConvertToInt(GetValue(2));
                gs.FriendlyFire = ConvertToInt(GetValue(1));
                gs.Unknown1 = ConvertToInt(GetValue(1));
                gs.Dontrestrictteamvoicechat = ConvertToInt(GetValue(1));
                gs.Unknown2 = ConvertToInt(GetValue(1));
                gs.Unknown3 = ConvertToInt(GetValue(1));
                gs.Indestructiblevehicles = ConvertToInt(GetValue(1));
                gs.turretsonmap = ConvertToInt(GetValue(1));
                gs.powerupsonmap = ConvertToInt(GetValue(1));
                gs.abilitiesonmap = ConvertToInt(GetValue(1));
                gs.shortcutsonmap = ConvertToInt(GetValue(1));
                gs.BasePlayerTraits = new();
                gs.BasePlayerTraits = ReadTraits(binaryString, gs.BasePlayerTraits);
                gs.WeaponSet = ConvertToInt(GetValue(8));
                gs.VehicleSet = ConvertToInt(GetValue(8));
                gs.EquipmentSet = ConvertToInt(GetValue(8));
                gs.Unknown4 = GetValue(55);

                gs.BetrayalBooting = null;
                gs.ProximityVoice = null;
                gs.allowdeadplayerstotalk = null;
                gs.grenadesonmap = null;
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
            } else
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
                pt.H2AH4.DamageTraitsDuration = ConvertToInt(GetValue(6));
                pt.H2AH4.DamageTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.DamageTraitsRuntime);
                pt.H2AH4.DamageTraitsRuntimeDuration = ConvertToInt(GetValue(6));

                pt.H2AH4.SpeedTraits = ReadTraits(binaryString, pt.H2AH4.SpeedTraits);
                pt.H2AH4.SpeedTraitsDuration = ConvertToInt(GetValue(6));
                pt.H2AH4.SpeedTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.SpeedTraitsRuntime);
                pt.H2AH4.SpeedTraitsRuntimeDuration = ConvertToInt(GetValue(6));

                pt.H2AH4.OverShieldTraits = ReadTraits(binaryString, pt.H2AH4.OverShieldTraits);
                pt.H2AH4.OverShieldTraitsDuration = ConvertToInt(GetValue(6));
                pt.H2AH4.OverShieldTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.OverShieldTraitsRuntime);
                pt.H2AH4.OverShieldTraitsRuntimeDuration = ConvertToInt(GetValue(6));

                pt.H2AH4.CustomTraits = ReadTraits(binaryString, pt.H2AH4.DamageTraits);
                pt.H2AH4.CustomTraitsDuration = ConvertToInt(GetValue(6));
                pt.H2AH4.CustomTraitsRuntime = ReadTraits(binaryString, pt.H2AH4.CustomTraitsRuntime);
                pt.H2AH4.CustomTraitsRuntimeDuration = ConvertToInt(GetValue(6));
            } 

            gt.PowerupTraits = Newtonsoft.Json.JsonConvert.SerializeObject(pt);

            //ConvertAndSaveToXml(pt, "gametype.xml");

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
            if (Settings.Default.DecompiledVersion > 0 )
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
            if (Settings.Default.DecompiledVersion == 0)
            {
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
            }
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
                    spt.H2AH4.hidden = ConvertToInt(GetValue(1));
                }
                spt.PlayerTraits = new();
                spt.PlayerTraits = ReadTraits(binaryString, spt.PlayerTraits);
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
            GetValue(2642);

            


            //We have now reached the gametype script!
            

            //Read Conditions
            Conditions c = new();
            c.ConditionCount = ConvertToInt(GetValue(10));
            for (int i=0; i< c.ConditionCount; i++)
            {
                c.ConditionType = ConvertToInt(GetValue(5));
                if (c.ConditionType == 1) //if condition
                {
                    
                }
            }

            gametypeItems.Add(gt);
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
            }
            if (Settings.Default.DecompiledVersion > 0)
            {
                lo.Grenades = ConvertToInt(GetValue(5));
            }

            if (Settings.Default.DecompiledVersion == 0)
            {
                lo.Grenades = ConvertToInt(GetValue(4));
            }
            if (Settings.Default.DecompiledVersion > 0)
            {
                lo.Unknown = ConvertToInt(GetValue(6));
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
                ls.French = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Spanish = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.LatinAmericanSpanish = ConvertToInt(GetValue(1)) == 0 ? "1" : GetValue(bits);
                ls.German = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Italian = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Korean = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
                ls.Japanese = ConvertToInt(GetValue(1)) == 0 ? "-1" : GetValue(bits);
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
                pt.DamageResistance = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)): -1;
                pt.ShieldMultiplyer = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.Healthmultiplyer = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.shieldstunduration = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.ShieldRegenrate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.Healthregenrate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.Overshieldregenrate = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.shieldvampirism = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.explosivedamageresistance = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.wheelmanvehiclestuntime = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
                pt.H2AH4.wheelmanvehiclerechargetime = ConvertToInt(GetValue(1)) == 1 ? ConvertToInt(GetValue(16)) : -1;
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
                pt.H2AH4.Unknown4 = ConvertToInt(GetValue(1));
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
