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

namespace UniversalGametypeEditor
{
    
    public class ReadGametype
    {

        public struct Gametype
        {
            public FileHeader FileHeader;
            public GametypeHeader GametypeHeader;
            public ModeSettings ModeSettings;
            public SpawnSettings SpawnSettings;
            public GameSettings GameSettings;
            public PowerupTraits PowerupTraits;
            public TeamSettings TeamSettings;
            public LoadoutCluster loadoutCluster;
            public ScriptedPlayerTraits scriptedPlayerTraits;
            public ScriptOptions scriptOptions;
            public Strings Strings;
            public Game Game;
            public Map Map;
            public PlayerRatings playerratings;
        }

        public struct FileHeader
        {
            public string mpvr;
            public int megaloversion;
            public int Unknown0x2F8;
            public int Unknown0x2FA;
            public string UnknownHash0x2FC;
            public string Blank0x310;
            public int Fileusedsize;
            public int Unknown0x318;
            public int Unknown0x319;
            public int Unknown0x31D;
            public int FileLength;
        }

        public struct GametypeHeader
        {
            public string ID0x48;
            public string ID0x50;
            public string ID0x58;
            public string Blank0x60;
            public int UnknownFlags;
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

  
        public struct ModeSettings
        {
            public int UnknownFlag2;
            public int Teamsenabled;
            public int Resetmaponnewroundunused;
            public int Resetplayersonnewroundunused;
            public int Perfectionmedalenabled;
            public int RoundTimeLimit;
            public int NumberOfRounds;
            public int RoundsToWin;
            public int SuddenDeathTime;
            public int GracePeriod;
        }


        public struct SpawnSettings
        {
            public int RespawnOnKills;
            public int respawnatlocationunused;
            public int respawnwithteammateunused;
            public int RespawnSyncwithteam;
            public int LivesPerround;
            public int TeamLivesPerround;
            public int RespawnTime;
            public int Suicidepenalty;
            public int Betrayalpenalty;
            public int RespawnTimegrowth;
            public int LoadoutCamTime;
            public int Respawntraitsduration;
            public PlayerTraits RespawnPlayerTraits;
        }


        public struct PlayerTraits
        {
            public int DamageResistance;
            public int Healthmultiplyer;
            public int Healthregenrate;
            public int ShieldMultiplyer;
            public int ShieldRegenrate;
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
        }


        public struct TeamOptions
        {
            public int TertiarycolorOverride;
            public int SecondarycolorOverride;
            public int PrimarycolorOverride;
            public int TeamEnabled;
            public int Unknown;
            public LanguageStrings Teamstring;
            public int InitialDesignator;
            public int Elitespecies;
            public int PrimaryColor;
            public int SecondaryColor;
            public int TertiaryColor;
            public int FireteamCount;
        }

        

        public struct GameSettings
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
            public PlayerTraits BasePlayerTraits;
            public int WeaponSet;
            public int VehicleSet;
        }


        public struct PowerupTraits
        {
            public PlayerTraits RedPlayerTraits;
            public PlayerTraits BluePlayerTraits;
            public PlayerTraits YellowPlayerTraits;
            public int RedPowerupDuration;
            public int BluePowerupDuration;
            public int YellowPowerupDuration;
        }

 

        public struct TeamSettings
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
        }

        public struct LanguageStrings
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
        }


        public struct LoadoutOptions
        {
            public int LoadoutVisibleingame;
            public int LoadoutName;
            public int LoadoutNameIndex;
            public int NameIndex;
            public int PrimaryWeapon;
            public int SecondaryWeapon;
            public int Armorability;
            public int Grenades;
        }


        public struct LoadoutCluster
        {
            public int EliteLoadoutsEnabled;
            public int SpartanLoadoutsEnabled;
            public LoadoutOptions Loadout1;
            public LoadoutOptions Loadout2;
            public LoadoutOptions Loadout3;
            public LoadoutOptions Loadout4;
            public LoadoutOptions Loadout5;
            public LoadoutOptions Loadout6;
            public LoadoutOptions Loadout7;
            public LoadoutOptions Loadout8;
            public LoadoutOptions Loadout9;
            public LoadoutOptions Loadout10;
            public LoadoutOptions Loadout11;
            public LoadoutOptions Loadout12;
            public LoadoutOptions Loadout13;
            public LoadoutOptions Loadout14;
            public LoadoutOptions Loadout15;
            public LoadoutOptions Loadout16;
            public LoadoutOptions Loadout17;
            public LoadoutOptions Loadout18;
            public LoadoutOptions Loadout19;
            public LoadoutOptions Loadout20;
            public LoadoutOptions Loadout21;
            public LoadoutOptions Loadout22;
            public LoadoutOptions Loadout23;
            public LoadoutOptions Loadout24;
            public LoadoutOptions Loadout25;
            public LoadoutOptions Loadout26;
            public LoadoutOptions Loadout27;
            public LoadoutOptions Loadout28;
            public LoadoutOptions Loadout29;
            public LoadoutOptions Loadout30;
        }


        public struct ScriptedPlayerTraits
        {
            public int count;
            public int String1;
            public int String2;
            public PlayerTraits PlayerTraits;
        }


        public struct ScriptOptions
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
        }


        public struct Strings
        {
            public LanguageStrings Stringtable;
            public int StringNameIndex;
            public LanguageStrings metanameStrings;
            public LanguageStrings metadescStrings;
            public LanguageStrings metagroupStrings;
        }


        public struct Game
        {
            public int ActualGameicon;
            public int ActualGamecategory;
        }


        public struct PlayerRatings
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



        public struct Map
        {
            public int MapID;
            public int mappermsflip;
        }


        public static void ConvertAndSaveToXml<T>(T data, string filePath) where T : struct
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root;

            try
            {
                xmlDoc.Load(filePath);
                root = xmlDoc.DocumentElement ?? xmlDoc.CreateElement("Root");
            }
            catch
            {
                root = xmlDoc.CreateElement("Root");
                xmlDoc.AppendChild(root);
            }

            XmlElement structNode = xmlDoc.CreateElement(typeof(T).Name);
            root.AppendChild(structNode);

            foreach (var field in typeof(T).GetFields())
            {
                object fieldValue = field.GetValue(data);

                if (fieldValue.GetType().IsValueType || fieldValue is string)
                {
                    XmlElement fieldNode = xmlDoc.CreateElement(field.Name);
                    fieldNode.InnerText = fieldValue.ToString();
                    structNode.AppendChild(fieldNode);
                }
                else
                {
                    ConvertAndSaveToXml((T)fieldValue, filePath);
                }
            }

            xmlDoc.Save(filePath);
        }



        public FileHeader fh = new();
        public GametypeHeader gth = new();
        public ModeSettings ms = new();
        public SpawnSettings ss = new();
        public GameSettings gs = new();
        public string binaryString = "";
        public void ReadBinary(string filePath)
        {

            
            byte[] binaryData = File.ReadAllBytes(filePath);

            //Convert binaryData to a string of 1s and 0s
            binaryString = GetBinaryString(binaryData, 752, binaryData.Length*7);


            Gametype gt = new();



            //Read FileHeader
            
            fh.mpvr = GetValue(32);
            fh.megaloversion = ConvertToInt(GetValue(32));
            fh.Unknown0x2F8 = ConvertToInt(GetValue(16));
            fh.Unknown0x2FA = ConvertToInt(GetValue(16));
            fh.UnknownHash0x2FC = GetValue(160);
            fh.Blank0x310 = GetValue(32);
            fh.Fileusedsize = ConvertToInt(GetValue(32));
            fh.Unknown0x318 = ConvertToInt(GetValue(8));
            fh.Unknown0x319 = ConvertToInt(GetValue(32));
            fh.Unknown0x31D = ConvertToInt(GetValue(32));
            fh.FileLength = ConvertToInt(GetValue(32));
            gt.FileHeader = fh;

            //ConvertAndSaveToXml(fh, "gametype.xml");

            //Read GametypeHeader
            
            gth.ID0x48 = GetValue(64);
            gth.ID0x50 = GetValue(64);
            gth.ID0x58 = GetValue(64);
            gth.Blank0x60 = GetValue(64);
            gth.UnknownFlags = ConvertToInt(GetValue(9));
            gth.Unknown_1 = ConvertToInt(GetValue(32));
            gth.Unknown0x1 = ConvertToInt(GetValue(8));
            gth.Blank04 = ConvertToInt(GetValue(32));
            gth.TimeStampUint = ConvertToInt(GetValue(32));
            gth.XUID = GetValue(64);
            gth.Gamertag = ReadStringFromBits(binaryString, true);
            gth.Blank041bit = GetValue(33);
            gth.EditTimeStampUint = ConvertToInt(GetValue(32));
            gth.EditXUID = GetValue(64);
            gth.EditGamertag = ReadStringFromBits(binaryString, true);
            gth.UnknownFlag1 = ConvertToInt(GetValue(1));
            gth.Title = ReadUStringFromBits(binaryString);
            gth.Description = ReadUStringFromBits(binaryString);
            gth.GameIcon = ConvertToInt(GetValue(8));
            gt.GametypeHeader = gth;

            //ConvertAndSaveToXml(gth, "gametype.xml");

            //Read ModeSettings
            
            ms.UnknownFlag2 = ConvertToInt(GetValue(1));
            ms.Teamsenabled = ConvertToInt(GetValue(1));
            ms.Resetmaponnewroundunused = ConvertToInt(GetValue(1));
            ms.Resetplayersonnewroundunused = ConvertToInt(GetValue(1));
            ms.Perfectionmedalenabled = ConvertToInt(GetValue(1));
            ms.RoundTimeLimit = ConvertToInt(GetValue(8));
            ms.NumberOfRounds = ConvertToInt(GetValue(5));
            ms.RoundsToWin = ConvertToInt(GetValue(4));
            ms.SuddenDeathTime = ConvertToInt(GetValue(7));
            ms.GracePeriod = ConvertToInt(GetValue(5));
            gt.ModeSettings = ms;

            //ConvertAndSaveToXml(ms, "gametype.xml");

            //Read SpawnSettings
            
            ss.RespawnOnKills = ConvertToInt(GetValue(1));
            ss.respawnatlocationunused = ConvertToInt(GetValue(1));
            ss.respawnwithteammateunused = ConvertToInt(GetValue(1));
            ss.RespawnSyncwithteam = ConvertToInt(GetValue(1));
            ss.LivesPerround = ConvertToInt(GetValue(6));
            ss.TeamLivesPerround = ConvertToInt(GetValue(7));
            ss.RespawnTime = ConvertToInt(GetValue(8));
            ss.Suicidepenalty = ConvertToInt(GetValue(8));
            ss.Betrayalpenalty = ConvertToInt(GetValue(8));
            ss.RespawnTimegrowth = ConvertToInt(GetValue(4));
            ss.LoadoutCamTime = ConvertToInt(GetValue(4));
            ss.Respawntraitsduration = ConvertToInt(GetValue(6));
            ss.RespawnPlayerTraits = ReadTraits(binaryString);
            gt.SpawnSettings = ss;

            //ConvertAndSaveToXml(ss, "gametype.xml");

            //Read GameSettings
            
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
            gs.BasePlayerTraits = ReadTraits(binaryString);
            gs.WeaponSet = ConvertToInt(GetValue(8));
            gs.VehicleSet = ConvertToInt(GetValue(8));
            gt.GameSettings = gs;

            //ConvertAndSaveToXml(gs, "gametype.xml");

            //Read PowerupTraits
            PowerupTraits pt = new();
            pt.RedPlayerTraits = ReadTraits(binaryString);
            pt.BluePlayerTraits = ReadTraits(binaryString);
            pt.YellowPlayerTraits = ReadTraits(binaryString);
            pt.RedPowerupDuration = ConvertToInt(GetValue(7));
            pt.BluePowerupDuration = ConvertToInt(GetValue(7));
            pt.YellowPowerupDuration = ConvertToInt(GetValue(7));
            gt.PowerupTraits = pt;

            //ConvertAndSaveToXml(pt, "gametype.xml");

            //Read TeamSettings
            TeamSettings ts = new();
            ts.TeamScoringMethod = ConvertToInt(GetValue(3));
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
            gt.TeamSettings = ts;

            //ConvertAndSaveToXml(ts, "gametype.xml");

            //Read LoadoutCluster
            LoadoutCluster lc = new();
            lc.EliteLoadoutsEnabled = ConvertToInt(GetValue(1));
            lc.SpartanLoadoutsEnabled = ConvertToInt(GetValue(1));
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
            gt.loadoutCluster = lc;

            //ConvertAndSaveToXml(lc, "gametype.xml");
            
            //Read ScriptedPlayerTraits
            ScriptedPlayerTraits spt = new();
            spt.count = ConvertToInt(GetValue(5));
            for (int i = 0; i < spt.count; i++)
            {
                spt.String1 = ConvertToInt(GetValue(7));
                spt.String2 = ConvertToInt(GetValue(7));
                spt.PlayerTraits = ReadTraits(binaryString);
            }

            //ConvertAndSaveToXml(spt, "gametype.xml");

            //Read ScriptOptions
            ScriptOptions so = new();
            so.count = ConvertToInt(GetValue(5));
            for (int i = 0; i < so.count; i++)
            {
                so.String1 = ConvertToInt(GetValue(7));
                so.String2 = ConvertToInt(GetValue(7));
                so.ScriptOption = ConvertToInt(GetValue(1));
                if (so.ScriptOption == 0)
                {
                    so.ChildIndex = ConvertToInt(GetValue(3));
                    so.ScriptOptionChild = ConvertToInt(GetValue(4));
                    for (int j = 0; j < so.ScriptOptionChild; j++)
                    {
                        so.Value = ConvertToInt(GetValue(10));
                        so.String1 = ConvertToInt(GetValue(7));
                        so.String2 = ConvertToInt(GetValue(7));
                    }
                    so.Unknown = ConvertToInt(GetValue(3));
                }
                if (so.ScriptOption == 1)
                {
                    so.range1 = ConvertToInt(GetValue(10));
                    so.range2 = ConvertToInt(GetValue(10));
                    so.range3 = ConvertToInt(GetValue(10));
                    so.range4 = ConvertToInt(GetValue(10));
                }

            }
            gt.scriptOptions = so;

            //ConvertAndSaveToXml(so, "gametype.xml");

            //Read Strings
            Strings st = new();
            st.Stringtable = ReadLangStrings(15, 7, false);
            st.StringNameIndex = ConvertToInt(GetValue(7));
            st.metanameStrings = ReadLangStrings(9, 1, false);
            st.metadescStrings = ReadLangStrings(12, 1, false);
            st.metagroupStrings = ReadLangStrings(9, 1, false);
            gt.Strings = st;

            //ConvertAndSaveToXml(st, "gametype.xml");

            //Read Game
            Game g = new();
            g.ActualGameicon = ConvertToInt(GetValue(5));
            g.ActualGamecategory = ConvertToInt(GetValue(5));
            gt.Game = g;

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

            


            //We have now reached the gametype script!
        }

        private LoadoutOptions ReadLoadoutOptions()
        {
            LoadoutOptions lo = new();
            lo.LoadoutVisibleingame = ConvertToInt(GetValue(1));
            lo.LoadoutName = ConvertToInt(GetValue(1));
            if (lo.LoadoutName == 0)
            {
                lo.LoadoutNameIndex = ConvertToInt(GetValue(7));
            }
            lo.PrimaryWeapon = ConvertToInt(GetValue(8));
            lo.SecondaryWeapon = ConvertToInt(GetValue(8));
            lo.Armorability = ConvertToInt(GetValue(8));
            lo.Grenades = ConvertToInt(GetValue(4));

            return lo;

        }

        private TeamOptions ReadTeaMOptions()
        {
            TeamOptions to = new();
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
                indexes.Add(ls);
            }
            
            
            
            string compressedChunk = "";
            bool compression = true;

            if (stringPresent > 0)
            {
                int m3;
                if (teamString)
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
            var zip = new ICSharpCode.SharpZipLib.Zip.Compression.Inflater();
            {
                zip.SetInput(bytes, skipHeaderLength, bytes.Length - skipHeaderLength); // skip the decompressed size header
                zip.Inflate(result);
            }
            return result;
        }



        private PlayerTraits ReadTraits(string binary)
        {
            //Use the PlayerTraits struct to read the traits
            //Return the traits

            PlayerTraits pt = new();
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
