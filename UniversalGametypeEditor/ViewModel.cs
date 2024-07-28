using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using UniversalGametypeEditor.Properties;
using static UniversalGametypeEditor.ReadGametype;

namespace UniversalGametypeEditor
{

    //public class FileHeader
    //{
    //    public string? mpvr;
    //    public int megaloversion;
    //    public int Unknown0x2F8;
    //    public int Unknown0x2FA;
    //    public string? UnknownHash0x2FC;
    //    public string? Blank0x310;
    //    public int Fileusedsize;
    //    public int Unknown0x318;
    //    public int Unknown0x319;
    //    public int Unknown0x31D;
    //    public int FileLength;

    //    public string? JSON;

    //    public string? fileHeaderBits;
    //}

    public class SharedProperties
    {
        public dynamic Value { get; set; }
        public int Bits { get; set; }

        public dynamic EnumTranslations { get; set; }
        


        public SharedProperties(int bits)
        {
            Bits = bits;
        }

        public static implicit operator int(SharedProperties p)
        {
            return p.Value;
        }

        public static implicit operator SharedProperties(int value)
        {
            return new SharedProperties(0) { Value = value };
        }
    }


    public class FileHeaderViewModel : INotifyPropertyChanged
    {
        private FileHeader data;
        private SharedProperties _mpvr;
        private SharedProperties _megaloVersion;
        private SharedProperties _unknown0x2F8;
        private SharedProperties _unknown0x2FA;
        private SharedProperties _unknownHash0x2FC;
        private SharedProperties _Blank0x310;
        private SharedProperties _Fileusedsize;
        private SharedProperties _Unknown0x318;
        private SharedProperties _variantType;
        private SharedProperties _Unknown0x319 = new(4);
        private SharedProperties _Unknown0x31D = new(32);
        private SharedProperties _Unknown0x31C = new(32);
        private SharedProperties _FileLength = new(32);


        public FileHeaderViewModel(object data)
        {
            if (data is FileHeader fileHeaderData)
            {
                this.data = fileHeaderData;
            }
            else
            {
                throw new ArgumentException("Invalid data type. Expected FileHeader type.", nameof(data));
            }
        }


        public SharedProperties Mpvr
        {
            get { 
                if (_mpvr == null)
                {
                    _mpvr = new SharedProperties(32)
                    {
                        Value = data.mpvr
                    };
                }
                return _mpvr;
                ; }
            set
            {
                if (data.mpvr != value.Value)
                {
                    data.mpvr = value.Value;
                    _mpvr = value;
                    OnPropertyChanged(nameof(Mpvr));
                }
            }
        }

        public SharedProperties MegaloVersion
        {
            get
            {
                if (_megaloVersion == null)
                {
                    _megaloVersion = new SharedProperties(32)
                    {
                        Value = data.megaloversion
                    };
                }
                return _megaloVersion;
            }
            set
            {
                if (data.megaloversion != value.Value)
                {
                    data.megaloversion = value.Value;
                    _megaloVersion = value;
                    OnPropertyChanged(nameof(MegaloVersion));
                }
            }
        }


        public SharedProperties Unknown0x2F8
        {
            get {
                if (_unknown0x2F8 == null)
                {
                    _unknown0x2F8 = new SharedProperties(16)
                    {
                        Value = data.Unknown0x2F8
                    };
                }
                return _unknown0x2F8;
            }
            set
            {
                if (data.Unknown0x2F8 != value.Value)
                {
                    data.Unknown0x2F8 = value.Value;
                    _unknown0x2F8 = value;
                    OnPropertyChanged(nameof(Unknown0x2F8));
                }
            }
        }

        public int Unknown0x2FA
        {
            get { 
                if (_unknown0x2FA == null)
                {
                    _unknown0x2FA = new SharedProperties(16)
                    {
                        Value = data.Unknown0x2FA
                    };
                }
                return _unknown0x2FA;
            }
            set
            {
                if (data.Unknown0x2FA != value)
                {
                    data.Unknown0x2FA = value;
                    _unknown0x2FA = value;
                    OnPropertyChanged(nameof(Unknown0x2FA));
                }
            }
        }

        public SharedProperties UnknownHash0x2FC
        {
            get { 
                if (_unknownHash0x2FC == null)
                {
                    _unknownHash0x2FC = new SharedProperties(160)
                    {
                        Value = data.UnknownHash0x2FC
                    };
                }
                return _unknownHash0x2FC;
            }
            set
            {
                if (data.UnknownHash0x2FC != value.Value)
                {
                    data.UnknownHash0x2FC = value.Value;
                    _unknownHash0x2FC = value;
                    OnPropertyChanged(nameof(UnknownHash0x2FC));
                }
            }
        }

        public SharedProperties Blank0x310
        {
            get { 
                if (_Blank0x310 == null)
                {
                    _Blank0x310 = new SharedProperties(32)
                    {
                        Value = data.Blank0x310
                    };
                }
                return _Blank0x310;
            }
            set
            {
                if (data.Blank0x310 != value.Value)
                {
                    data.Blank0x310 = value.Value;
                    _Blank0x310 = value;
                    OnPropertyChanged(nameof(Blank0x310));
                }
            }
        }

        public SharedProperties FileUsedSize
        {
            get { 
                if (_Fileusedsize == null)
                {
                    _Fileusedsize = new SharedProperties(32)
                    {
                        Value = data.Fileusedsize
                    };
                }
                return _Fileusedsize;
            }
            set
            {
                if (data.Fileusedsize != value.Value)
                {
                    data.Fileusedsize = value.Value;
                    _Fileusedsize = value;
                    OnPropertyChanged(nameof(FileUsedSize));
                }
            }
        }

        public SharedProperties Unknown0x318
        {
            get { 
                if (_Unknown0x318 == null)
                {
                    _Unknown0x318 = new SharedProperties(2)
                    {
                        Value = data.Unknown0x318
                    };
                }  
                return _Unknown0x318;
            }
            set
            {
                if (data.Unknown0x318 != value.Value)
                {
                    data.Unknown0x318 = value.Value;
                    _Unknown0x318 = value;
                    OnPropertyChanged(nameof(Unknown0x318));
                }
            }
        }

        

        public SharedProperties VariantType
        {
            get
            {
                return (VariantTypeEnum)data.VariantType;
            }
            set
            {
                VariantTypeEnum newValue = VariantTypeStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.VariantType != (int)newValue)
                {
                    data.VariantType = (int)value;
                    OnPropertyChanged(nameof(VariantType));
                }
            }
        }
        


        public int Unknown0x319
        {
            get { return data.Unknown0x319; }
            set
            {
                if (data.Unknown0x319 != value)
                {
                    data.Unknown0x319 = value;
                    OnPropertyChanged(nameof(Unknown0x319));
                }
            }
        }

        public int Unknown0x31D
        {
            get { return data.Unknown0x31D; }
            set
            {
                if (data.Unknown0x31D != value)
                {
                    data.Unknown0x31D = value;
                    OnPropertyChanged(nameof(Unknown0x31D));
                }
            }
        }

        public int Unknown0x31C
        {
            get { return data.Unknown0x31C; }
            set
            {
                if (data.Unknown0x31C != value)
                {
                    data.Unknown0x31C = value;
                    OnPropertyChanged(nameof(Unknown0x31C));
                }
            }
        }

        public int FileLength
        {
            get { return data.FileLength; }
            set
            {
                if (data.FileLength != value)
                {
                    data.FileLength = value;
                    OnPropertyChanged(nameof(FileLength));
                }
            }
        }



        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //public class GametypeHeader
    //{
    //    public string ID0x48;
    //    public string ID0x50;
    //    public string ID0x58;
    //    public string Blank0x60;
    //    public string UnknownFlags;
    //    public int Unknown_1;
    //    public int Unknown0x1;
    //    public int Blank04;
    //    public int TimeStampUint;
    //    public string XUID;
    //    public string Gamertag;
    //    public string Blank041bit;
    //    public int EditTimeStampUint;
    //    public string EditXUID;
    //    public string EditGamertag;
    //    public int UnknownFlag1;
    //    public string Title;
    //    public string Description;
    //    public int GameIcon;
    //}
    public class GametypeHeaderViewModel : INotifyPropertyChanged
    {
        private GametypeHeader data;
        private int gamertagLength;
        private int editGamertagLength;
        private int titleLength;
        private int descriptionLength;

        public GametypeHeaderViewModel(GametypeHeader data)
        {
            this.data = data;
            this.gamertagLength = data.Gamertag.Length;
            this.editGamertagLength = data.EditGamertag.Length;
            this.titleLength = data.Title.Length;
            this.descriptionLength = data.Description.Length;
            
        }

        public string ID0x48
        {
            get { return data.ID0x48; }
            set
            {
                if (data.ID0x48 != value)
                {
                    data.ID0x48 = value;
                    OnPropertyChanged(nameof(ID0x48));
                }
            }
        }

        public string ID0x50
        {
            get { return data.ID0x50; }
            set
            {
                if (data.ID0x50 != value)
                {
                    data.ID0x50 = value;
                    OnPropertyChanged(nameof(ID0x50));
                }
            }
        }

        public string ID0x58
        {
            get { return data.ID0x58; }
            set
            {
                if (data.ID0x58 != value)
                {
                    data.ID0x58 = value;
                    OnPropertyChanged(nameof(ID0x58));
                }
            }
        }

        public string Blank0x60
        {
            get { return data.Blank0x60; }
            set
            {
                if (data.Blank0x60 != value)
                {
                    data.Blank0x60 = value;
                    OnPropertyChanged(nameof(Blank0x60));
                }
            }
        }

        public string UnknownFlags
        {
            get { return data.UnknownFlags; }
            set
            {
                if (data.UnknownFlags != value)
                {
                    data.UnknownFlags = value;
                    OnPropertyChanged(nameof(UnknownFlags));
                }
            }
        }

        public int Unknown_1
        {
            get { return data.Unknown_1; }
            set
            {
                if (data.Unknown_1 != value)
                {
                    data.Unknown_1 = value;
                    OnPropertyChanged(nameof(Unknown_1));
                }
            }
        }

        public int Unknown0x1
        {
            get { return data.Unknown0x1; }
            set
            {
                if (data.Unknown0x1 != value)
                {
                    data.Unknown0x1 = value;
                    OnPropertyChanged(nameof(Unknown0x1));
                }
            }
        }

        public int Blank04
        {
            get { return data.Blank04; }
            set
            {
                if (data.Blank04 != value)
                {
                    data.Blank04 = value;
                    OnPropertyChanged(nameof(Blank04));
                }
            }
        }

        public int TimeStampUint
        {
            get { return data.TimeStampUint; }
            set
            {
                if (data.TimeStampUint != value)
                {
                    data.TimeStampUint = value;
                    OnPropertyChanged(nameof(TimeStampUint));
                }
            }
        }

        public string XUID
        {
            get { return data.XUID; }
            set
            {
                if (data.XUID != value)
                {
                    data.XUID = value;
                    OnPropertyChanged(nameof(XUID));
                }
            }
        }

        public string Gamertag
        {
            get { return data.Gamertag; }
            set
            {
                if (data.Gamertag != value)
                {
                    data.Gamertag = value;
                    OnPropertyChanged(nameof(Gamertag));
                }
            }
        }

        public string Blank041bit
        {
            get { return data.Blank041bit; }
            set
            {
                if (data.Blank041bit != value)
                {
                    data.Blank041bit = value;
                    OnPropertyChanged(nameof(Blank041bit));
                }
            }
        }

        public int EditTimeStampUint
        {
            get { return data.EditTimeStampUint; }
            set
            {
                if (data.EditTimeStampUint != value)
                {
                    data.EditTimeStampUint = value;
                    OnPropertyChanged(nameof(EditTimeStampUint));
                }
            }
        }

        public string EditXUID
        {
            get { return data.EditXUID; }
            set
            {
                if (data.EditXUID != value)
                {
                    data.EditXUID = value;
                    OnPropertyChanged(nameof(EditXUID));
                }
            }
        }

        public string EditGamertag
        {
            get { return data.EditGamertag; }
            set
            {
                if (data.EditGamertag != value)
                {
                    data.EditGamertag = value;
                    OnPropertyChanged(nameof(EditGamertag));
                }
            }
        }

        public int UnknownFlag1
        {
            get { return data.UnknownFlag1; }
            set
            {
                if (data.UnknownFlag1 != value)
                {
                    data.UnknownFlag1 = value;
                    OnPropertyChanged(nameof(UnknownFlag1));
                }
            }
        }

        public string Title
        {
            get { return data.Title; }
            set
            {
                if (data.Title != value)
                {
                    data.Title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public string Description
        {
            get { return data.Description; }
            set
            {
                if (data.Description != value)
                {
                    data.Description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        private SharedProperties _gameIcon;
        public SharedProperties GameIcon
        {
            get
            {
                if (_gameIcon == null)
                {
                    _gameIcon = new SharedProperties(5) { Value = (IconEnum)data.GameIcon, EnumTranslations = IconStrings };
                }
                return _gameIcon;
            }
            set
            {
                IconEnum newValue = IconStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.GameIcon != (int)newValue)
                {
                    data.GameIcon = (int)newValue;
                    _gameIcon = new SharedProperties(5) { Value = newValue };
                    OnPropertyChanged(nameof(GameIcon));
                }
            }
        }

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    //public class ModeSettings
    //{
    //    public int UnknownFlag2;
    //    public int Teamsenabled;
    //    public int Resetmaponnewroundunused;
    //    public int Resetplayersonnewroundunused;
    //    public int Perfectionmedalenabled;
    //    public int RoundTimeLimit;
    //    public int NumberOfRounds;
    //    public int RoundsToWin;
    //    public int? SuddenDeathTime;
    //    public ReachSettings Reach { get; set; } // Fields for Reach

    //    public class ReachSettings
    //    {
    //        public int? GracePeriod;
    //    }


    //    public H2AH4Settings H2AH4 { get; set; } // Fields exclusively for H2A+H4

    //    public class H2AH4Settings
    //    {
    //        public int? Bit4;
    //        public int? Bit5;
    //        public int? MoshDifficulty;
    //        public int? ProtoMode;
    //        public int? Unknown4;
    //        public int? ClassColorOverride;
    //        public int? InheritRespawnTime;
    //        public int? Unknown42;
    //        public int? KillCamEnabled;
    //        public int? PointsSystemEnabled;
    //        public int? FinalKillCamEnabled;
    //        public int? Unknown2;
    //    }
    //}

    public class ModeSettingsViewModel : INotifyPropertyChanged
    {
        private ModeSettings data;
        private SharedProperties _unknownFlag2;
        private SharedProperties _teamsEnabled;
        private SharedProperties _resetMapOnNewRoundUnused;
        private SharedProperties _resetPlayersOnNewRoundUnused;
        private SharedProperties _perfectionMedalEnabled;
        private SharedProperties _roundTimeLimit;
        private SharedProperties _numberOfRounds;
        private SharedProperties _roundsToWin;
        private SharedProperties _suddenDeathTime;

        public ModeSettingsViewModel(ModeSettings data)
        {
            this.data = data;
            Reach = new ReachSettingsViewModel(data.Reach);
            H2AH4 = new H2AH4SettingsViewModel(data.H2AH4);
        }

        public SharedProperties UnknownFlag2
        {
            get
            {
                if (_unknownFlag2 == null)
                {
                    if (Settings.Default.DecompiledVersion == 0)
                    {
                        _unknownFlag2 = new SharedProperties(1)
                        {
                            Value = Convert.ToBoolean(data.UnknownFlag2)
                        };
                    }
                    else
                    {
                        _unknownFlag2 = new SharedProperties(2)
                        {
                            Value = Convert.ToBoolean(data.UnknownFlag2)
                        };
                    }

                }
                return _unknownFlag2;
            }
            set
            {
                if (Convert.ToBoolean(data.UnknownFlag2) != Convert.ToBoolean(value.Value))
                {
                    data.UnknownFlag2 = Convert.ToInt32(value.Value);
                    _unknownFlag2 = value;
                    OnPropertyChanged(nameof(UnknownFlag2));
                }
            }
        }

        

        public SharedProperties TeamsEnabled
        {
            get
            {
                if (_teamsEnabled == null)
                {
                    _teamsEnabled = new SharedProperties(1)
                    {
                        Value = Convert.ToBoolean(data.Teamsenabled)
                    };
                }
                return _teamsEnabled;
            }
            set
            {
                if (Convert.ToBoolean(data.Teamsenabled) != Convert.ToBoolean(value.Value))
                {
                    data.Teamsenabled = Convert.ToInt32(value.Value);
                    _teamsEnabled = value;
                    OnPropertyChanged(nameof(TeamsEnabled));
                }
            }
        }

        public SharedProperties ResetMapOnNewRoundUnused
        {
            get
            {
                if (_resetMapOnNewRoundUnused == null)
                {
                    _resetMapOnNewRoundUnused = new SharedProperties(1)
                    {
                        Value = Convert.ToBoolean(data.Resetmaponnewroundunused)
                    };
                }
                return _resetMapOnNewRoundUnused;
            }
            set
            {
                if (Convert.ToBoolean(data.Resetmaponnewroundunused) != Convert.ToBoolean(value.Value))
                {
                    data.Resetmaponnewroundunused = Convert.ToInt32(value.Value);
                    _resetMapOnNewRoundUnused = value;
                    OnPropertyChanged(nameof(ResetMapOnNewRoundUnused));
                }
            }
        }

        public SharedProperties ResetPlayersOnNewRoundUnused
        {
            get
            {
                if (_resetPlayersOnNewRoundUnused == null)
                {
                    _resetPlayersOnNewRoundUnused = new SharedProperties(1)
                    {
                        Value = Convert.ToBoolean(data.Resetplayersonnewroundunused)
                    };
                }
                return _resetPlayersOnNewRoundUnused;
            }
            set
            {
                if (Convert.ToBoolean(data.Resetplayersonnewroundunused) != Convert.ToBoolean(value.Value))
                {
                    data.Resetplayersonnewroundunused = Convert.ToInt32(value.Value);
                    _resetPlayersOnNewRoundUnused = value;
                    OnPropertyChanged(nameof(ResetPlayersOnNewRoundUnused));
                }
            }
        }

        

        public SharedProperties PerfectionMedalEnabled
        {
            get
            {
                if (_perfectionMedalEnabled == null)
                {
                    _perfectionMedalEnabled = new SharedProperties(1)
                    {
                        Value = Convert.ToBoolean(data.Perfectionmedalenabled)
                    };
                }
                return _perfectionMedalEnabled;
            }
            set
            {
                if (Convert.ToBoolean(data.Perfectionmedalenabled) != Convert.ToBoolean(value.Value))
                {
                    data.Perfectionmedalenabled = Convert.ToInt32(value.Value);
                    _perfectionMedalEnabled = value;
                    OnPropertyChanged(nameof(PerfectionMedalEnabled));
                }
            }
        }

        public SharedProperties RoundTimeLimit
        {
            get
            {
                if (_roundTimeLimit == null)
                {
                    _roundTimeLimit = new SharedProperties(8)
                    {
                        Value = data.RoundTimeLimit
                    };
                }
                return _roundTimeLimit;
            }
            set
            {
                if (data.RoundTimeLimit != Convert.ToInt32(value.Value))
                {
                    data.RoundTimeLimit = Convert.ToInt32(value.Value);
                    _roundTimeLimit = value;
                    OnPropertyChanged(nameof(RoundTimeLimit));
                }
            }
        }

        public SharedProperties NumberOfRounds
        {
            get
            {
                if (_numberOfRounds == null)
                {
                    _numberOfRounds = new SharedProperties(5)
                    {
                        Value = data.NumberOfRounds
                    };
                }
                return _numberOfRounds;
            }
            set
            {
                if (data.NumberOfRounds != Convert.ToInt32(value.Value))
                {
                    data.NumberOfRounds = Convert.ToInt32(value.Value);
                    _numberOfRounds = value;
                    OnPropertyChanged(nameof(NumberOfRounds));
                }
            }
        }

        public SharedProperties RoundsToWin
        {
            get
            {
                if (_roundsToWin == null)
                {
                    _roundsToWin = new SharedProperties(4)
                    {
                        Value = data.RoundsToWin
                    };
                }
                return _roundsToWin;
            }
            set
            {
                if (data.RoundsToWin != Convert.ToInt32(value.Value))
                {
                    data.RoundsToWin = Convert.ToInt32(value.Value);
                    _roundsToWin = value;
                    OnPropertyChanged(nameof(RoundsToWin));
                }
            }
        }

        public SharedProperties SuddenDeathTime
        {
            get
            {
                if (_suddenDeathTime == null)
                {
                    _suddenDeathTime = new SharedProperties(7)
                    {
                        Value = data.SuddenDeathTime
                    };
                }
                return _suddenDeathTime;
            }
            set
            {
                if (data.SuddenDeathTime != Convert.ToInt32(value.Value))
                {
                    data.SuddenDeathTime = Convert.ToInt32(value.Value);
                    _suddenDeathTime = value;
                    OnPropertyChanged(nameof(SuddenDeathTime));
                }
            }
        }

        public ReachSettingsViewModel Reach { get; set; }
        public H2AH4SettingsViewModel H2AH4 { get; set; }

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public class SpawnSettingsViewModel : INotifyPropertyChanged
    {
        private SpawnSettings data;
        private SharedProperties _livesPerround;
        private SharedProperties _respawnTime;
        private SharedProperties _teamLivesPerround;
        private SharedProperties _suicidepenalty;
        private SharedProperties _betrayalpenalty;

        public SpawnSettingsViewModel(SpawnSettings data)
        { 
            this.data = data;
            RespawnPlayerTraits = new PlayerTraitsViewModel(data.RespawnPlayerTraits);
            H2AH4 = new SpawnH2AH4SettingsViewModel(data.H2AH4);
            Reach = new SpawnReachSettingsViewModel(data.Reach);
        }

        public SpawnReachSettingsViewModel Reach { get; set; }

        public SharedProperties LivesPerround
        {
            get { 
                if (_livesPerround == null)
                {
                    _livesPerround = new SharedProperties(6) { Value = data.LivesPerround };
                }
                return _livesPerround;
            }
            set
            {
                if (data.LivesPerround != Convert.ToInt32(value.Value))
                {
                    
                    data.LivesPerround = Convert.ToInt32(value.Value);
                    OnPropertyChanged(nameof(LivesPerround));
                }
            }
        }

        public SharedProperties TeamLivesPerround
        {
            get { 
                if (_teamLivesPerround == null)
                {
                    _teamLivesPerround = new SharedProperties(7) { Value = data.TeamLivesPerround };
                }
                return _teamLivesPerround;
            }
            set
            {
                if (data.TeamLivesPerround != value.Value)
                {
                    data.TeamLivesPerround = value.Value;
                    OnPropertyChanged(nameof(TeamLivesPerround));
                }
            }
        }

        public SpawnH2AH4SettingsViewModel H2AH4 { get; set; }

        public SharedProperties RespawnTime
        {
            get { 
                if (_respawnTime == null)
                {
                    _respawnTime = new SharedProperties(8) { Value = data.RespawnTime };
                }
                return _respawnTime;
            }
            set
            {
                if (data.RespawnTime != value.Value)
                {
                    data.RespawnTime = value.Value;
                    OnPropertyChanged(nameof(RespawnTime));
                }
            }
        }

        

        public SharedProperties Suicidepenalty
        {
            get
            {
                if (_suicidepenalty == null)
                {
                    _suicidepenalty = new SharedProperties(8) { Value = data.Suicidepenalty };
                }
                return _suicidepenalty;
            }
            set
            {
                if (data.Suicidepenalty != Convert.ToInt32(value.Value))
                {
                    data.Suicidepenalty = Convert.ToInt32(value.Value);
                    _suicidepenalty = value;
                    OnPropertyChanged(nameof(Suicidepenalty));
                }
            }
        }

        public SharedProperties Betrayalpenalty
        {
            get
            {
                if (_betrayalpenalty == null)
                {
                    _betrayalpenalty = new SharedProperties(8) { Value = data.Betrayalpenalty };
                }
                return _betrayalpenalty;
            }
            set
            {
                if (data.Betrayalpenalty != Convert.ToInt32(value.Value))
                {
                    data.Betrayalpenalty = Convert.ToInt32(value.Value);
                    _betrayalpenalty = value;
                    OnPropertyChanged(nameof(Betrayalpenalty));
                }
            }
        }

        private SharedProperties _respawnTimegrowth;
        private SharedProperties _loadoutCamTime;
        private SharedProperties _respawntraitsduration;

        public SharedProperties RespawnTimegrowth
        {
            get
            {
                if (_respawnTimegrowth == null)
                {
                    _respawnTimegrowth = new SharedProperties(4) { Value = data.RespawnTimegrowth };
                }
                return _respawnTimegrowth;
            }
            set
            {
                if (data.RespawnTimegrowth != Convert.ToInt32(value.Value))
                {
                    data.RespawnTimegrowth = Convert.ToInt32(value.Value);
                    _respawnTimegrowth = value;
                    OnPropertyChanged(nameof(RespawnTimegrowth));
                }
            }
        }

        public SharedProperties LoadoutCamTime
        {
            get
            {
                if (_loadoutCamTime == null)
                {
                    _loadoutCamTime = new SharedProperties(4) { Value = data.LoadoutCamTime };
                }
                return _loadoutCamTime;
            }
            set
            {
                if (data.LoadoutCamTime != Convert.ToInt32(value.Value))
                {
                    data.LoadoutCamTime = Convert.ToInt32(value.Value);
                    _loadoutCamTime = value;
                    OnPropertyChanged(nameof(LoadoutCamTime));
                }
            }
        }

        public SharedProperties Respawntraitsduration
        {
            get
            {
                if (_respawntraitsduration == null)
                {
                    _respawntraitsduration = new SharedProperties(6) { Value = data.Respawntraitsduration };
                }
                return _respawntraitsduration;
            }
            set
            {
                if (data.Respawntraitsduration != Convert.ToInt32(value.Value))
                {
                    data.Respawntraitsduration = Convert.ToInt32(value.Value);
                    _respawntraitsduration = value;
                    OnPropertyChanged(nameof(Respawntraitsduration));
                }
            }
        }

        public PlayerTraitsViewModel RespawnPlayerTraits { get; set; }

       

        
        

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //public class PlayerTraits
    //{

    //    public int Healthmultiplyer;
    //    public int Healthregenrate;


    //    public int DamageResistance;
    //    public int ShieldRegenrate;

    //    public int ShieldMultiplyer;

    //    public int Overshieldregenrate;
    //    public int HeadshotImmunity;
    //    public int shieldvampirism;
    //    public int Assasinationimmunity;
    //    public int invincible;
    //    public int WeaponDamagemultiplier;
    //    public int MeleeDamagemultiplier;
    //    public int Primaryweapon;
    //    public int Secondaryweapon;
    //    public int Grenades;
    //    public int Infiniteammo;
    //    public int Grenaderegen;
    //    public int WeaponPickup;
    //    public int AbilityUsage;
    //    public int Abilitiesdropondeath;
    //    public int InfiniteAbility;
    //    public int ArmorAbility;
    //    public int MovementSpeed;
    //    public int Playergravity;
    //    public int VehicleUse;
    //    public int Unknown;
    //    public int JumpHeight;
    //    public int JumpOverride;
    //    public int Camo;
    //    public int Visiblewaypoint;
    //    public int VisibleName;
    //    public int Aura;
    //    public int Forcedcolor;
    //    public int Motiontrackermode;
    //    public int MotiontrackerRange;
    //    public int DirectionalDamageindicator;

        public class PlayerTraitsViewModel
    {
        private PlayerTraits data;

        private SharedProperties _healthMultiplyer;
        private SharedProperties _healthRegenRate;
        private SharedProperties _damageResistance;
        private SharedProperties _shieldRegenRate;
        private SharedProperties _shieldMultiplyer;
        private SharedProperties _overshieldRegenRate;
        private SharedProperties _headshotImmunity;
        private SharedProperties _shieldVampirism;


        public PlayerTraitsViewModel(PlayerTraits data)
        {
            this.data = data;
            H2AH4 = new TraitsH2AH4SettingsViewModel(data.H2AH4);
            
        }



        public SharedProperties DamageResistance
        {
            
            get
            {
                if (_damageResistance == null)
                {
                    _damageResistance = new SharedProperties(4) { Value = (DamageResistanceEnum)data.DamageResistance, EnumTranslations=DamageResistanceStrings };
                }
                return _damageResistance;
            }
            set
            {
                DamageResistanceEnum newValue = DamageResistanceStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.DamageResistance != (int)newValue)
                {
                    data.DamageResistance = (int)newValue;
                    _damageResistance = new SharedProperties(4) { Value = newValue };
                    OnPropertyChanged(nameof(DamageResistance));
                }
            }
        }

        public SharedProperties HealthMultiplyer
        {
            get
            {
                if (_healthMultiplyer == null)
                {
                    _healthMultiplyer = new SharedProperties(3) { Value = (HealthMultiplierEnum)data.Healthmultiplyer, EnumTranslations=HealthMultiplierStrings };
                }
                return _healthMultiplyer;
            }
            set
            {
                HealthMultiplierEnum newValue = HealthMultiplierStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.Healthmultiplyer != (int)newValue)
                {
                    data.Healthmultiplyer = (int)newValue;
                    _healthMultiplyer = new SharedProperties(3) { Value = newValue };
                    OnPropertyChanged(nameof(HealthMultiplyer));
                }
            }
        }

        public SharedProperties HealthRegenRate
        {
            get
            {
                if (_healthRegenRate == null)
                {
                    _healthRegenRate = new SharedProperties(4) { Value = (RegenEnum)data.Healthregenrate, EnumTranslations = RegenStrings };
                }
                return _healthRegenRate;
            }
            set
            {
                RegenEnum newValue = RegenStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.Healthregenrate != (int)newValue)
                {
                    data.Healthregenrate = (int)newValue;
                    _healthRegenRate = new SharedProperties(4) { Value = newValue };
                    OnPropertyChanged(nameof(HealthRegenRate));
                }
            }
        }



        public SharedProperties ShieldMultiplyer
        {
            get
            {
                if (_shieldMultiplyer == null)
                {
                    _shieldMultiplyer = new SharedProperties(3) { Value = (ShieldMultiplyerEnum)data.ShieldMultiplyer, EnumTranslations = ShieldMultiplyerStrings };
                }
                return _shieldMultiplyer;
            }
            set
            {
                ShieldMultiplyerEnum newValue = ShieldMultiplyerStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.ShieldMultiplyer != (int)newValue)
                {
                    data.ShieldMultiplyer = (int)newValue;
                    _shieldMultiplyer = new SharedProperties(3) { Value = newValue };
                    OnPropertyChanged(nameof(ShieldMultiplyer));
                }
            }
        }

        public SharedProperties ShieldRegenRate
        {
            get
            {
                if (_shieldRegenRate == null)
                {
                    _shieldRegenRate = new SharedProperties(4) { Value = (RegenEnum)data.ShieldRegenrate, EnumTranslations = RegenStrings };
                }
                return _shieldRegenRate;
            }
            set
            {
                RegenEnum newValue = RegenStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.ShieldRegenrate != (int)newValue)
                {
                    data.ShieldRegenrate = (int)newValue;
                    _shieldRegenRate = new SharedProperties(4) { Value = newValue };
                    OnPropertyChanged(nameof(ShieldRegenRate));
                }
            }
        }


        public SharedProperties OvershieldRegenRate
        {
            get
            {
                if (_overshieldRegenRate == null)
                {
                    _overshieldRegenRate = new SharedProperties(4) { Value = (RegenEnum)data.Overshieldregenrate, EnumTranslations = RegenStrings };
                }
                return _overshieldRegenRate;
            }
            set
            {
                RegenEnum newValue = RegenStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.Overshieldregenrate != (int)newValue)
                {
                    data.Overshieldregenrate = (int)newValue;
                    _overshieldRegenRate = new SharedProperties(4) { Value = newValue };
                    OnPropertyChanged(nameof(OvershieldRegenRate));
                }
            }
        }


        public SharedProperties HeadshotImmunity
        {
            get
            {
                if (_headshotImmunity == null)
                {
                    _headshotImmunity = new SharedProperties(2) { Value = (ToggleEnum)data.HeadshotImmunity, EnumTranslations = ToggleEnumStrings };
                }
                return _headshotImmunity;
            }
            set
            {
                ToggleEnum newValue = ToggleEnumStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.HeadshotImmunity != (int)newValue)
                {
                    data.HeadshotImmunity = (int)newValue;
                    _headshotImmunity = new SharedProperties(2) { Value = newValue };
                    OnPropertyChanged(nameof(HeadshotImmunity));
                }
            }
        }


        public SharedProperties ShieldVampirism
        {
            get
            {
                if (_shieldVampirism == null)
                {
                    _shieldVampirism = new SharedProperties(3) { Value = (VampirismEnum)data.shieldvampirism, EnumTranslations = VamparismStrings };
                }
                return _shieldVampirism;
            }
            set
            {
                VampirismEnum newValue = VamparismStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.shieldvampirism != (int)newValue)
                {
                    data.shieldvampirism = (int)newValue;
                    _shieldVampirism = new SharedProperties(3) { Value = newValue };
                    OnPropertyChanged(nameof(ShieldVampirism));
                }
            }
        }


        private SharedProperties _assassinationImmunity;

        public SharedProperties AssassinationImmunity
        {
            get
            {
                if (_assassinationImmunity == null)
                {
                    _assassinationImmunity = new SharedProperties(2) { Value = (ToggleEnum)data.Assasinationimmunity, EnumTranslations = ToggleEnumStrings };
                }
                return _assassinationImmunity;
            }
            set
            {
                ToggleEnum newValue = ToggleEnumStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.Assasinationimmunity != (int)newValue)
                {
                    data.Assasinationimmunity = (int)newValue;
                    _assassinationImmunity = new SharedProperties(2) { Value = newValue };
                    OnPropertyChanged(nameof(AssassinationImmunity));
                }
            }
        }

        private SharedProperties _invincible;

        public SharedProperties Invincible
        {
            get
            {
                if (_invincible == null)
                {
                    _invincible = new SharedProperties(2) { Value = (ToggleEnum)data.invincible, EnumTranslations = ToggleEnumStrings };
                }
                return _invincible;
            }
            set
            {
                ToggleEnum newValue = ToggleEnumStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.invincible != (int)newValue)
                {
                    data.invincible = (int)newValue;
                    _invincible = new SharedProperties(2) { Value = newValue };
                    OnPropertyChanged(nameof(Invincible));
                }
            }
        }

        private SharedProperties _weaponDamageMultiplier;
        public SharedProperties WeaponDamageMultiplier
        {
            get
            {
                if (_weaponDamageMultiplier == null)
                {
                    _weaponDamageMultiplier = new SharedProperties(4) { Value = (DamageEnum)data.WeaponDamagemultiplier, EnumTranslations = DamageStrings };
                }
                return _weaponDamageMultiplier;
            }
            set
            {
                DamageEnum newValue = DamageStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.WeaponDamagemultiplier != (int)newValue)
                {
                    data.WeaponDamagemultiplier = (int)newValue;
                    _weaponDamageMultiplier = new SharedProperties(4) { Value = newValue };
                    OnPropertyChanged(nameof(WeaponDamageMultiplier));
                }
            }
        }

        private SharedProperties _meleeDamageMultiplier;
        public SharedProperties MeleeDamageMultiplier
        {
            get
            {
                if (_meleeDamageMultiplier == null)
                {
                    _meleeDamageMultiplier = new SharedProperties(4) { Value = (DamageEnum)data.MeleeDamagemultiplier, EnumTranslations = DamageStrings };
                }
                return _meleeDamageMultiplier;
            }
            set
            {
                DamageEnum newValue = DamageStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.MeleeDamagemultiplier != (int)newValue)
                {
                    data.MeleeDamagemultiplier = (int)newValue;
                    _meleeDamageMultiplier = new SharedProperties(4) { Value = newValue };
                    OnPropertyChanged(nameof(MeleeDamageMultiplier));
                }
            }
        }

        private SharedProperties _primaryWeapon;
        public SharedProperties PrimaryWeapon
        {
            get
            {
                if (_primaryWeapon == null)
                {
                    _primaryWeapon = new SharedProperties(8) { Value = (WeaponEnum)data.Primaryweapon, EnumTranslations = WeaponStrings };
                }
                return _primaryWeapon;
            }
            set
            {
                WeaponEnum newValue = WeaponStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.Primaryweapon != (int)newValue)
                {
                    data.Primaryweapon = (int)newValue;
                    _primaryWeapon = new SharedProperties(8) { Value = newValue };
                    OnPropertyChanged(nameof(PrimaryWeapon));
                }
            }
        }

        private SharedProperties _secondaryWeapon;
        public SharedProperties SecondaryWeapon
        {
            get
            {
                if (_secondaryWeapon == null)
                {
                    _secondaryWeapon = new SharedProperties(8) { Value = (WeaponEnum)data.Secondaryweapon, EnumTranslations = WeaponStrings };
                }
                return _secondaryWeapon;
            }
            set
            {
                WeaponEnum newValue = WeaponStrings.FirstOrDefault(x => x.Value == value.Value.ToString()).Key;
                if (data.Secondaryweapon != (int)newValue)
                {
                    data.Secondaryweapon = (int)newValue;
                    _secondaryWeapon = new SharedProperties(8) { Value = newValue };
                    OnPropertyChanged(nameof(SecondaryWeapon));
                }
            }
        }

        public int Grenades
        {
            get { return data.Grenades; }
            set
            {
                if (data.Grenades != value)
                {
                    data.Grenades = value;
                    OnPropertyChanged(nameof(Grenades));
                }
            }
        }

        public int InfiniteAmmo
        {
            get { return data.Infiniteammo; }
            set
            {
                if (data.Infiniteammo != value)
                {
                    data.Infiniteammo = value;
                    OnPropertyChanged(nameof(InfiniteAmmo));
                }
            }
        }

        public int GrenadeRegen
        {
            get { return data.Grenaderegen; }
            set
            {
                if (data.Grenaderegen != value)
                {
                    data.Grenaderegen = value;
                    OnPropertyChanged(nameof(GrenadeRegen));
                }
            }
        }

        public int WeaponPickup
        {
            get { return data.WeaponPickup; }
            set
            {
                if (data.WeaponPickup != value)
                {
                    data.WeaponPickup = value;
                    OnPropertyChanged(nameof(WeaponPickup));
                }
            }
        }

        public int AbilityUsage
        {
            get { return data.AbilityUsage; }
            set
            {
                if (data.AbilityUsage != value)
                {
                    data.AbilityUsage = value;
                    OnPropertyChanged(nameof(AbilityUsage));
                }
            }
        }

        public int AbilitiesDropOnDeath
        {
            get { return data.Abilitiesdropondeath; }
            set
            {
                if (data.Abilitiesdropondeath != value)
                {
                    data.Abilitiesdropondeath = value;
                    OnPropertyChanged(nameof(AbilitiesDropOnDeath));
                }
            }
        }

        public int InfiniteAbility
        {
            get { return data.InfiniteAbility; }
            set
            {
                if (data.InfiniteAbility != value)
                {
                    data.InfiniteAbility = value;
                    OnPropertyChanged(nameof(InfiniteAbility));
                }
            }
        }

        public int ArmorAbility
        {
            get { return data.ArmorAbility; }
            set
            {
                if (data.ArmorAbility != value)
                {
                    data.ArmorAbility = value;
                    OnPropertyChanged(nameof(ArmorAbility));
                }
            }
        }

        public int MovementSpeed
        {
            get { return data.MovementSpeed; }
            set
            {
                if (data.MovementSpeed != value)
                {
                    data.MovementSpeed = value;
                    OnPropertyChanged(nameof(MovementSpeed));
                }
            }
        }

        public int PlayerGravity
        {
            get { return data.Playergravity; }
            set
            {
                if (data.Playergravity != value)
                {
                    data.Playergravity = value;
                    OnPropertyChanged(nameof(PlayerGravity));
                }
            }
        }

        public int VehicleUse
        {
            get { return data.VehicleUse; }
            set
            {
                if (data.VehicleUse != value)
                {
                    data.VehicleUse = value;
                    OnPropertyChanged(nameof(VehicleUse));
                }
            }
        }

        public int Unknown
        {
            get { return data.Unknown; }
            set
            {
                if (data.Unknown != value)
                {
                    data.Unknown = value;
                    OnPropertyChanged(nameof(Unknown));
                }
            }
        }

        public int JumpHeight
        {
            get { return data.JumpHeight; }
            set
            {
                if (data.JumpHeight != value)
                {
                    data.JumpHeight = value;
                    OnPropertyChanged(nameof(JumpHeight));
                }
            }
        }

        public int JumpOverride
        {
            get { return data.JumpOverride; }
            set
            {
                if (data.JumpOverride != value)
                {
                    data.JumpOverride = value;
                    OnPropertyChanged(nameof(JumpOverride));
                }
            }
        }

        public int Camo
        {
            get { return data.Camo; }
            set
            {
                if (data.Camo != value)
                {
                    data.Camo = value;
                    OnPropertyChanged(nameof(Camo));
                }
            }
        }

        public int VisibleWaypoint
        {
            get { return data.Visiblewaypoint; }
            set
            {
                if (data.Visiblewaypoint != value)
                {
                    data.Visiblewaypoint = value;
                    OnPropertyChanged(nameof(VisibleWaypoint));
                }
            }
        }

        public int VisibleName
        {
            get { return data.VisibleName; }
            set
            {
                if (data.VisibleName != value)
                {
                    data.VisibleName = value;
                    OnPropertyChanged(nameof(VisibleName));
                }
            }
        }

        public int Aura
        {
            get { return data.Aura; }
            set
            {
                if (data.Aura != value)
                {
                    data.Aura = value;
                    OnPropertyChanged(nameof(Aura));
                }
            }
        }

        public int ForcedColor
        {
            get { return data.Forcedcolor; }
            set
            {
                if (data.Forcedcolor != value)
                {
                    data.Forcedcolor = value;
                    OnPropertyChanged(nameof(ForcedColor));
                }
            }
        }

        public int MotionTrackerMode
        {
            get { return data.Motiontrackermode; }
            set
            {
                if (data.Motiontrackermode != value)
                {
                    data.Motiontrackermode = value;
                    OnPropertyChanged(nameof(MotionTrackerMode));
                }
            }
        }

        public int MotionTrackerRange
        {
            get { return data.MotiontrackerRange; }
            set
            {
                if (data.MotiontrackerRange != value)
                {
                    data.MotiontrackerRange = value;
                    OnPropertyChanged(nameof(MotionTrackerRange));
                }
            }
        }

        public int DirectionalDamageIndicator
        {
            get { return data.DirectionalDamageindicator; }
            set
            {
                if (data.DirectionalDamageindicator != value)
                {
                    data.DirectionalDamageindicator = value;
                    OnPropertyChanged(nameof(DirectionalDamageIndicator));
                }
            }
        }

        public TraitsH2AH4SettingsViewModel H2AH4 { get; set; }



        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    //public class H2AH4Settings
    //{
    //    public int explosivedamageresistance;
    //    public int falldamage;
    //    public int fasttrackarmor;
    //    public int powerupcancelation;
    //    public int shieldstunduration;
    //    public int wheelmanvehicleemp;
    //    public int wheelmanvehiclerechargetime;
    //    public int wheelmanvehiclestuntime;
    //    public int GrenadeRechargeFrag;
    //    public int GrenadeRechargePlasma;
    //    public int GrenadeRechargeSpike;
    //    public int HeroEquipmentEnergyUse;
    //    public int HeroEquipmentEnergyRechargeDelay;
    //    public int HeroEquipmentEnergyRechargeRate;
    //    public int HeroEquipmentInitialEnergy;
    //    public int EquipmentEnergyUse;
    //    public int EquipmentEnergyRechargeDelay;
    //    public int EquipmentEnergyRechargeRate;
    //    public int EquipmentInitialEnergy;
    //    public int SwitchSpeed;
    //    public int ReloadSpeed;
    //    public int OrdinancePoints;
    //    public int ExplosiveAOE;
    //    public int GunnerArmor;
    //    public int StabilityArmor;
    //    public int DropReconWarning;
    //    public int DropReconDistance;
    //    public int AssassinationSpeed;
    //    public int UsageSansAutoTurret;
    //    public int AmmoPack;
    //    public int Grenadier;
    //    public int DropGrenadeDeath;
    //    public int OrdinanceMarkerVisibility;
    //    public int ScavengeGrenades;
    //    public int Firepower;
    //    public int OrdinanceReroll;
    //    public int OrdinanceDisabled;
    //    public int TacticalPackage;
    //    public int Nemesis;
    //    public int Aura2;
    //    public int Unknown2;
    //    public int Unknown3;
    //    public int Unknown4;
    //    public int AutoMomentum;
    //    public int BattleAwareness;
    //    public int DeathEffect;
    //    public int DoubleJump;
    //    public int ForcedPrimaryColor;
    //    public int ForcedSecondaryColor;
    //    public int Gravity;
    //    public int LoopingEffect;
    //    public int MotionTrackerEnabled;
    //    public int MotionTrackerUsageZoomed;
    //    public int Name;
    //    public int NemesisDuration;
    //    public int OverridePlayerModel;
    //    public int OverridePrimaryColor;
    //    public int OverrideSecondaryColor;
    //    public int PrimaryBlue;
    //    public int PrimaryGreen;
    //    public int PrimaryRed;
    //    public int SecondaryBlue;
    //    public int SecondaryGreen;
    //    public int SecondaryRed;
    //    public int Scale;
    //    public int ShieldHud;
    //    public int Speed;
    //    public int Sprint;
    //    public int Stealthy;
    //    public int SupportPackage;
    //    public int ThreadView;
    //    public int TurnSpeed;
    //    public int Vaulting;
    //    public int VisionMode;
    //}

    public class TraitsH2AH4SettingsViewModel : INotifyPropertyChanged
    {
        private PlayerTraits.H2AH4Settings data;
        public TraitsH2AH4SettingsViewModel(PlayerTraits.H2AH4Settings data)
        {
            this.data = data;
        }

        public int ExplosiveDamageResistance
        {
            get { return data.explosivedamageresistance; }
            set
            {
                if (data.explosivedamageresistance != value)
                {
                    data.explosivedamageresistance = value;
                    OnPropertyChanged(nameof(ExplosiveDamageResistance));
                }
            }
        }

        public int FallDamage
        {
            get { return data.falldamage; }
            set
            {
                if (data.falldamage != value)
                {
                    data.falldamage = value;
                    OnPropertyChanged(nameof(FallDamage));
                }
            }
        }

        public int FastTrackArmor
        {
            get { return data.fasttrackarmor; }
            set
            {
                if (data.fasttrackarmor != value)
                {
                    data.fasttrackarmor = value;
                    OnPropertyChanged(nameof(FastTrackArmor));
                }
            }
        }

        public int PowerupCancelation
        {
            get { return data.powerupcancelation; }
            set
            {
                if (data.powerupcancelation != value)
                {
                    data.powerupcancelation = value;
                    OnPropertyChanged(nameof(PowerupCancelation));
                }
            }
        }

        public int ShieldStunDuration
        {
            get { return data.shieldstunduration; }
            set
            {
                if (data.shieldstunduration != value)
                {
                    data.shieldstunduration = value;
                    OnPropertyChanged(nameof(ShieldStunDuration));
                }
            }
        }

        public int WheelmanVehicleEMP
        {
            get { return data.wheelmanvehicleemp; }
            set
            {
                if (data.wheelmanvehicleemp != value)
                {
                    data.wheelmanvehicleemp = value;
                    OnPropertyChanged(nameof(WheelmanVehicleEMP));
                }
            }
        }

        public int WheelmanVehicleRechargeTime
        {
            get { return data.wheelmanvehiclerechargetime; }
            set
            {
                if (data.wheelmanvehiclerechargetime != value)
                {
                    data.wheelmanvehiclerechargetime = value;
                    OnPropertyChanged(nameof(WheelmanVehicleRechargeTime));
                }
            }
        }

        public int WheelmanVehicleStunTime
        {
            get { return data.wheelmanvehiclestuntime; }
            set
            {
                if (data.wheelmanvehiclestuntime != value)
                {
                    data.wheelmanvehiclestuntime = value;
                    OnPropertyChanged(nameof(WheelmanVehicleStunTime));
                }
            }
        }

        public int GrenadeRechargeFrag
        {
            get { return data.GrenadeRechargeFrag; }
            set
            {
                if (data.GrenadeRechargeFrag != value)
                {
                    data.GrenadeRechargeFrag = value;
                    OnPropertyChanged(nameof(GrenadeRechargeFrag));
                }
            }
        }

        public int GrenadeRechargePlasma
        {
            get { return data.GrenadeRechargePlasma; }
            set
            {
                if (data.GrenadeRechargePlasma != value)
                {
                    data.GrenadeRechargePlasma = value;
                    OnPropertyChanged(nameof(GrenadeRechargePlasma));
                }
            }
        }

        public int GrenadeRechargeSpike
        {
            get { return data.GrenadeRechargeSpike; }
            set
            {
                if (data.GrenadeRechargeSpike != value)
                {
                    data.GrenadeRechargeSpike = value;
                    OnPropertyChanged(nameof(GrenadeRechargeSpike));
                }
            }
        }

        public int HeroEquipmentEnergyUse
        {
            get { return data.HeroEquipmentEnergyUse; }
            set
            {
                if (data.HeroEquipmentEnergyUse != value)
                {
                    data.HeroEquipmentEnergyUse = value;
                    OnPropertyChanged(nameof(HeroEquipmentEnergyUse));
                }
            }
        }

        public int HeroEquipmentEnergyRechargeDelay
        {
            get { return data.HeroEquipmentEnergyRechargeDelay; }
            set
            {
                if (data.HeroEquipmentEnergyRechargeDelay != value)
                {
                    data.HeroEquipmentEnergyRechargeDelay = value;
                    OnPropertyChanged(nameof(HeroEquipmentEnergyRechargeDelay));
                }
            }
        }

        public int HeroEquipmentEnergyRechargeRate
        {
            get { return data.HeroEquipmentEnergyRechargeRate; }
            set
            {
                if (data.HeroEquipmentEnergyRechargeRate != value)
                {
                    data.HeroEquipmentEnergyRechargeRate = value;
                    OnPropertyChanged(nameof(HeroEquipmentEnergyRechargeRate));
                }
            }
        }

        public int HeroEquipmentInitialEnergy
        {
            get { return data.HeroEquipmentInitialEnergy; }
            set
            {
                if (data.HeroEquipmentInitialEnergy != value)
                {
                    data.HeroEquipmentInitialEnergy = value;
                    OnPropertyChanged(nameof(HeroEquipmentInitialEnergy));
                }
            }
        }

        public int EquipmentEnergyUse
        {
            get { return data.EquipmentEnergyUse; }
            set
            {
                if (data.EquipmentEnergyUse != value)
                {
                    data.EquipmentEnergyUse = value;
                    OnPropertyChanged(nameof(EquipmentEnergyUse));
                }
            }
        }

        public int EquipmentEnergyRechargeDelay
        {
            get { return data.EquipmentEnergyRechargeDelay; }
            set
            {
                if (data.EquipmentEnergyRechargeDelay != value)
                {
                    data.EquipmentEnergyRechargeDelay = value;
                    OnPropertyChanged(nameof(EquipmentEnergyRechargeDelay));
                }
            }
        }

        public int EquipmentEnergyRechargeRate
        {
            get { return data.EquipmentEnergyRechargeRate; }
            set
            {
                if (data.EquipmentEnergyRechargeRate != value)
                {
                    data.EquipmentEnergyRechargeRate = value;
                    OnPropertyChanged(nameof(EquipmentEnergyRechargeRate));
                }
            }
        }

        public int EquipmentInitialEnergy
        {
            get { return data.EquipmentInitialEnergy; }
            set
            {
                if (data.EquipmentInitialEnergy != value)
                {
                    data.EquipmentInitialEnergy = value;
                    OnPropertyChanged(nameof(EquipmentInitialEnergy));
                }
            }
        }

        public int SwitchSpeed
        {
            get { return data.SwitchSpeed; }
            set
            {
                if (data.SwitchSpeed != value)
                {
                    data.SwitchSpeed = value;
                    OnPropertyChanged(nameof(SwitchSpeed));
                }
            }
        }

        public int ReloadSpeed
        {
            get { return data.ReloadSpeed; }
            set
            {
                if (data.ReloadSpeed != value)
                {
                    data.ReloadSpeed = value;
                    OnPropertyChanged(nameof(ReloadSpeed));
                }
            }
        }

        public int OrdinancePoints
        {
            get { return data.OrdinancePoints; }
            set
            {
                if (data.OrdinancePoints != value)
                {
                    data.OrdinancePoints = value;
                    OnPropertyChanged(nameof(OrdinancePoints));
                }
            }
        }

        public int ExplosiveAOE
        {
            get { return data.ExplosiveAOE; }
            set
            {
                if (data.ExplosiveAOE != value)
                {
                    data.ExplosiveAOE = value;
                    OnPropertyChanged(nameof(ExplosiveAOE));
                }
            }
        }

        public int GunnerArmor
        {
            get { return data.GunnerArmor; }
            set
            {
                if (data.GunnerArmor != value)
                {
                    data.GunnerArmor = value;
                    OnPropertyChanged(nameof(GunnerArmor));
                }
            }
        }

        public int StabilityArmor
        {
            get { return data.StabilityArmor; }
            set
            {
                if (data.StabilityArmor != value)
                {
                    data.StabilityArmor = value;
                    OnPropertyChanged(nameof(StabilityArmor));
                }
            }
        }

        public int DropReconWarning
        {
            get { return data.DropReconWarning; }
            set
            {
                if (data.DropReconWarning != value)
                {
                    data.DropReconWarning = value;
                    OnPropertyChanged(nameof(DropReconWarning));
                }
            }
        }

        public int DropReconDistance
        {
            get { return data.DropReconDistance; }
            set
            {
                if (data.DropReconDistance != value)
                {
                    data.DropReconDistance = value;
                    OnPropertyChanged(nameof(DropReconDistance));
                }
            }
        }

        public int AssassinationSpeed
        {
            get { return data.AssassinationSpeed; }
            set
            {
                if (data.AssassinationSpeed != value)
                {
                    data.AssassinationSpeed = value;
                    OnPropertyChanged(nameof(AssassinationSpeed));
                }
            }
        }

        public int UsageSansAutoTurret
        {
            get { return data.UsageSansAutoTurret; }
            set
            {
                if (data.UsageSansAutoTurret != value)
                {
                    data.UsageSansAutoTurret = value;
                    OnPropertyChanged(nameof(UsageSansAutoTurret));
                }
            }
        }

        public int AmmoPack
        {
            get { return data.AmmoPack; }
            set
            {
                if (data.AmmoPack != value)
                {
                    data.AmmoPack = value;
                    OnPropertyChanged(nameof(AmmoPack));
                }
            }
        }

        public int Grenadier
        {
            get { return data.Grenadier; }
            set
            {
                if (data.Grenadier != value)
                {
                    data.Grenadier = value;
                    OnPropertyChanged(nameof(Grenadier));
                }
            }
        }

        public int DropGrenadeDeath
        {
            get { return data.DropGrenadeDeath; }
            set
            {
                if (data.DropGrenadeDeath != value)
                {
                    data.DropGrenadeDeath = value;
                    OnPropertyChanged(nameof(DropGrenadeDeath));
                }
            }
        }

        public int OrdinanceMarkerVisibility
        {
            get { return data.OrdinanceMarkerVisibility; }
            set
            {
                if (data.OrdinanceMarkerVisibility != value)
                {
                    data.OrdinanceMarkerVisibility = value;
                    OnPropertyChanged(nameof(OrdinanceMarkerVisibility));
                }
            }
        }

        public int ScavengeGrenades
        {
            get { return data.ScavengeGrenades; }
            set
            {
                if (data.ScavengeGrenades != value)
                {
                    data.ScavengeGrenades = value;
                    OnPropertyChanged(nameof(ScavengeGrenades));
                }
            }
        }

        public int Firepower
        {
            get { return data.Firepower; }
            set
            {
                if (data.Firepower != value)
                {
                    data.Firepower = value;
                    OnPropertyChanged(nameof(Firepower));
                }
            }
        }

        public int OrdinanceReroll
        {
            get { return data.OrdinanceReroll; }
            set
            {
                if (data.OrdinanceReroll != value)
                {
                    data.OrdinanceReroll = value;
                    OnPropertyChanged(nameof(OrdinanceReroll));
                }
            }
        }

        public int OrdinanceDisabled
        {
            get { return data.OrdinanceDisabled; }
            set
            {
                if (data.OrdinanceDisabled != value)
                {
                    data.OrdinanceDisabled = value;
                    OnPropertyChanged(nameof(OrdinanceDisabled));
                }
            }
        }

        public int TacticalPackage
        {
            get { return data.TacticalPackage; }
            set
            {
                if (data.TacticalPackage != value)
                {
                    data.TacticalPackage = value;
                    OnPropertyChanged(nameof(TacticalPackage));
                }
            }
        }

        public int Nemesis
        {
            get { return data.Nemesis; }
            set
            {
                if (data.Nemesis != value)
                {
                    data.Nemesis = value;
                    OnPropertyChanged(nameof(Nemesis));
                }
            }
        }

        public int Aura2
        {
            get { return data.Aura2; }
            set
            {
                if (data.Aura2 != value)
                {
                    data.Aura2 = value;
                    OnPropertyChanged(nameof(Aura2));
                }
            }
        }

        public int Unknown2
        {
            get { return data.Unknown2; }
            set
            {
                if (data.Unknown2 != value)
                {
                    data.Unknown2 = value;
                    OnPropertyChanged(nameof(Unknown2));
                }
            }
        }

        public int Unknown3
        {
            get { return data.Unknown3; }
            set
            {
                if (data.Unknown3 != value)
                {
                    data.Unknown3 = value;
                    OnPropertyChanged(nameof(Unknown3));
                }
            }
        }

        public int Unknown4
        {
            get { return data.Unknown4; }
            set
            {
                if (data.Unknown4 != value)
                {
                    data.Unknown4 = value;
                    OnPropertyChanged(nameof(Unknown4));
                }
            }
        }

        public int AutoMomentum
        {
            get { return data.AutoMomentum; }
            set
            {
                if (data.AutoMomentum != value)
                {
                    data.AutoMomentum = value;
                    OnPropertyChanged(nameof(AutoMomentum));
                }
            }
        }

        public int BattleAwareness
        {
            get { return data.BattleAwareness; }
            set
            {
                if (data.BattleAwareness != value)
                {
                    data.BattleAwareness = value;
                    OnPropertyChanged(nameof(BattleAwareness));
                }
            }
        }

        public int DeathEffect
        {
            get { return data.DeathEffect; }
            set
            {
                if (data.DeathEffect != value)
                {
                    data.DeathEffect = value;
                    OnPropertyChanged(nameof(DeathEffect));
                }
            }
        }

        public int DoubleJump
        {
            get { return data.DoubleJump; }
            set
            {
                if (data.DoubleJump != value)
                {
                    data.DoubleJump = value;
                    OnPropertyChanged(nameof(DoubleJump));
                }
            }
        }

        public int ForcedPrimaryColor
        {
            get { return data.ForcedPrimaryColor; }
            set
            {
                if (data.ForcedPrimaryColor != value)
                {
                    data.ForcedPrimaryColor = value;
                    OnPropertyChanged(nameof(ForcedPrimaryColor));
                }
            }
        }

        public int ForcedSecondaryColor
        {
            get { return data.ForcedSecondaryColor; }
            set
            {
                if (data.ForcedSecondaryColor != value)
                {
                    data.ForcedSecondaryColor = value;
                    OnPropertyChanged(nameof(ForcedSecondaryColor));
                }
            }
        }

        public int Gravity
        {
            get { return data.Gravity; }
            set
            {
                if (data.Gravity != value)
                {
                    data.Gravity = value;
                    OnPropertyChanged(nameof(Gravity));
                }
            }
        }

        public int LoopingEffect
        {
            get { return data.LoopingEffect; }
            set
            {
                if (data.LoopingEffect != value)
                {
                    data.LoopingEffect = value;
                    OnPropertyChanged(nameof(LoopingEffect));
                }
            }
        }

        public int MotionTrackerEnabled
        {
            get { return data.MotionTrackerEnabled; }
            set
            {
                if (data.MotionTrackerEnabled != value)
                {
                    data.MotionTrackerEnabled = value;
                    OnPropertyChanged(nameof(MotionTrackerEnabled));
                }
            }
        }

        public int MotionTrackerUsageZoomed
        {
            get { return data.MotionTrackerUsageZoomed; }
            set
            {
                if (data.MotionTrackerUsageZoomed != value)
                {
                    data.MotionTrackerUsageZoomed = value;
                    OnPropertyChanged(nameof(MotionTrackerUsageZoomed));
                }
            }
        }

        public int Name
        {
            get { return data.Name; }
            set
            {
                if (data.Name != value)
                {
                    data.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public int NemesisDuration
        {
            get { return data.NemesisDuration; }
            set
            {
                if (data.NemesisDuration != value)
                {
                    data.NemesisDuration = value;
                    OnPropertyChanged(nameof(NemesisDuration));
                }
            }
        }

        public int OverridePlayerModel
        {
            get { return data.OverridePlayerModel; }
            set
            {
                if (data.OverridePlayerModel != value)
                {
                    data.OverridePlayerModel = value;
                    OnPropertyChanged(nameof(OverridePlayerModel));
                }
            }
        }

        public int OverridePrimaryColor
        {
            get { return data.OverridePrimaryColor; }
            set
            {
                if (data.OverridePrimaryColor != value)
                {
                    data.OverridePrimaryColor = value;
                    OnPropertyChanged(nameof(OverridePrimaryColor));
                }
            }
        }

        public int OverrideSecondaryColor
        {
            get { return data.OverrideSecondaryColor; }
            set
            {
                if (data.OverrideSecondaryColor != value)
                {
                    data.OverrideSecondaryColor = value;
                    OnPropertyChanged(nameof(OverrideSecondaryColor));
                }
            }
        }

        public int PrimaryBlue
        {
            get { return data.PrimaryBlue; }
            set
            {
                if (data.PrimaryBlue != value)
                {
                    data.PrimaryBlue = value;
                    OnPropertyChanged(nameof(PrimaryBlue));
                }
            }
        }

        public int PrimaryGreen
        {
            get { return data.PrimaryGreen; }
            set
            {
                if (data.PrimaryGreen != value)
                {
                    data.PrimaryGreen = value;
                    OnPropertyChanged(nameof(PrimaryGreen));
                }
            }
        }

        public int PrimaryRed
        {
            get { return data.PrimaryRed; }
            set
            {
                if (data.PrimaryRed != value)
                {
                    data.PrimaryRed = value;
                    OnPropertyChanged(nameof(PrimaryRed));
                }
            }
        }

        public int SecondaryBlue
        {
            get { return data.SecondaryBlue; }
            set
            {
                if (data.SecondaryBlue != value)
                {
                    data.SecondaryBlue = value;
                    OnPropertyChanged(nameof(SecondaryBlue));
                }
            }
        }

        public int SecondaryGreen
        {
            get { return data.SecondaryGreen; }
            set
            {
                if (data.SecondaryGreen != value)
                {
                    data.SecondaryGreen = value;
                    OnPropertyChanged(nameof(SecondaryGreen));
                }
            }
        }

        public int SecondaryRed
        {
            get { return data.SecondaryRed; }
            set
            {
                if (data.SecondaryRed != value)
                {
                    data.SecondaryRed = value;
                    OnPropertyChanged(nameof(SecondaryRed));
                }
            }
        }

        public int Scale
        {
            get { return data.Scale; }
            set
            {
                if (data.Scale != value)
                {
                    data.Scale = value;
                    OnPropertyChanged(nameof(Scale));
                }
            }
        }

        public int ShieldHud
        {
            get { return data.ShieldHud; }
            set
            {
                if (data.ShieldHud != value)
                {
                    data.ShieldHud = value;
                    OnPropertyChanged(nameof(ShieldHud));
                }
            }
        }

        public int Speed
        {
            get { return data.Speed; }
            set
            {
                if (data.Speed != value)
                {
                    data.Speed = value;
                    OnPropertyChanged(nameof(Speed));
                }
            }
        }

        public int Sprint
        {
            get { return data.Sprint; }
            set
            {
                if (data.Sprint != value)
                {
                    data.Sprint = value;
                    OnPropertyChanged(nameof(Sprint));
                }
            }
        }

        public int Stealthy
        {
            get { return data.Stealthy; }
            set
            {
                if (data.Stealthy != value)
                {
                    data.Stealthy = value;
                    OnPropertyChanged(nameof(Stealthy));
                }
            }
        }

        public int SupportPackage
        {
            get { return data.SupportPackage; }
            set
            {
                if (data.SupportPackage != value)
                {
                    data.SupportPackage = value;
                    OnPropertyChanged(nameof(SupportPackage));
                }
            }
        }

        public int ThreadView
        {
            get { return data.ThreadView; }
            set
            {
                if (data.ThreadView != value)
                {
                    data.ThreadView = value;
                    OnPropertyChanged(nameof(ThreadView));
                }
            }
        }

        public int TurnSpeed
        {
            get { return data.TurnSpeed; }
            set
            {
                if (data.TurnSpeed != value)
                {
                    data.TurnSpeed = value;
                    OnPropertyChanged(nameof(TurnSpeed));
                }
            }
        }

        public int Vaulting
        {
            get { return data.Vaulting; }
            set
            {
                if (data.Vaulting != value)
                {
                    data.Vaulting = value;
                    OnPropertyChanged(nameof(Vaulting));
                }
            }
        }

        public int VisionMode
        {
            get { return data.VisionMode; }
            set
            {
                if (data.VisionMode != value)
                {
                    data.VisionMode = value;
                    OnPropertyChanged(nameof(VisionMode));
                }
            }
        }

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }


    //    public class H2AH4Settings
    //    {
    //        public int? Bit4;
    //        public int? Bit5;
    //        public int? MoshDifficulty;
    //        public int? ProtoMode;
    //        public int? Unknown4;
    //        public int? ClassColorOverride;
    //        public int? InheritRespawnTime;
    //        public int? Unknown42;
    //        public int? KillCamEnabled;
    //        public int? PointsSystemEnabled;
    //        public int? FinalKillCamEnabled;
    //        public int? Unknown2;
    //    }

    public class ReachSettingsViewModel : INotifyPropertyChanged
    {
        private ModeSettings.ReachSettings reachSettings;


        public ReachSettingsViewModel(ModeSettings.ReachSettings reachSettings)
        {
            this.reachSettings = reachSettings;
        }

        private SharedProperties _gracePeriod;

        public SharedProperties GracePeriod
        {
            get
            {
                if (_gracePeriod == null && reachSettings != null)
                {
                    _gracePeriod = new SharedProperties(5) { Value = Convert.ToInt32(reachSettings.GracePeriod) };
                }
                return _gracePeriod;
            }
            set
            {
                int newValue = Convert.ToInt32(value.Value);
                if (reachSettings != null && Convert.ToInt32(reachSettings.GracePeriod) != newValue)
                {
                    reachSettings.GracePeriod = Convert.ToInt32(newValue);
                    _gracePeriod = new SharedProperties(5) { Value = newValue };
                    OnPropertyChanged(nameof(GracePeriod));
                }
            }
        }

        // Repeat the pattern for other properties in ReachSettings
        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SpawnH2AH4Settings
    {

        private SpawnSettings.H2AH4Settings spawnSettings;

        public SpawnH2AH4Settings(SpawnSettings.H2AH4Settings spawnSettings)
        {
            this.spawnSettings = spawnSettings;
        }

        public int? MinRespawnTime
        {
            get { return spawnSettings?.MinRespawnTime; }
            set
            {
                if (spawnSettings != null && spawnSettings.MinRespawnTime != value)
                {
                    spawnSettings.MinRespawnTime = value;
                    OnPropertyChanged(nameof(MinRespawnTime));
                }
            }
        }

        // Repeat the pattern for other properties in ReachSettings
        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class SpawnReachSettings
    {
        public bool? RespawnOnKills;
        public int? respawnatlocationunused;
        public int? respawnwithteammateunused;
        public bool? RespawnSyncwithteam;
    }

    public class SpawnReachSettingsViewModel : INotifyPropertyChanged
    {
        private SpawnSettings.ReachSettings reachSettings;

        public SpawnReachSettingsViewModel(SpawnSettings.ReachSettings reachSettings)
        {
            this.reachSettings = reachSettings;
        }

        private SharedProperties _respawnOnKills;
        private SharedProperties _respawnAtLocationUnused;
        private SharedProperties _respawnWithTeammateUnused;
        private SharedProperties _respawnSyncWithTeam;

        public SharedProperties RespawnOnKills
        {
            get
            {
                if (_respawnOnKills == null && reachSettings != null)
                {
                    _respawnOnKills = new SharedProperties(1) { Value = Convert.ToBoolean(reachSettings?.RespawnOnKills) };
                }
                return _respawnOnKills;
            }
            set
            {
                bool newValue = Convert.ToBoolean(value.Value);
                if (reachSettings != null && Convert.ToBoolean(reachSettings.RespawnOnKills) != newValue)
                {
                    reachSettings.RespawnOnKills = Convert.ToInt32(newValue);
                    _respawnOnKills = new SharedProperties(1) { Value = newValue };
                    OnPropertyChanged(nameof(RespawnOnKills));
                }
            }
        }

        

        public SharedProperties RespawnAtLocationUnused
        {
            get
            {
                if (_respawnAtLocationUnused == null && reachSettings != null)
                {
                    _respawnAtLocationUnused = new SharedProperties(1) { Value = Convert.ToBoolean(reachSettings?.respawnatlocationunused) };
                }
                return _respawnAtLocationUnused;
            }
            set
            {
                bool newValue = Convert.ToBoolean(value.Value);
                if (reachSettings != null && Convert.ToBoolean(reachSettings.respawnatlocationunused) != newValue)
                {
                    reachSettings.respawnatlocationunused = Convert.ToInt32(newValue);
                    _respawnAtLocationUnused = new SharedProperties(1) { Value = newValue };
                    OnPropertyChanged(nameof(RespawnAtLocationUnused));
                }
            }
        }

        public SharedProperties RespawnWithTeammateUnused
        {
            get
            {
                if (_respawnWithTeammateUnused == null && reachSettings != null)
                {
                    _respawnWithTeammateUnused = new SharedProperties(1) { Value = Convert.ToBoolean(reachSettings?.respawnwithteammateunused) };
                }
                return _respawnWithTeammateUnused;
            }
            set
            {
                bool newValue = Convert.ToBoolean(value.Value);
                if (reachSettings != null && Convert.ToBoolean(reachSettings.respawnwithteammateunused) != newValue)
                {
                    reachSettings.respawnwithteammateunused = Convert.ToInt32(newValue);
                    _respawnWithTeammateUnused = new SharedProperties(1) { Value = newValue };
                    OnPropertyChanged(nameof(RespawnWithTeammateUnused));
                }
            }
        }

        public SharedProperties RespawnSyncWithTeam
        {
            get
            {
                if (_respawnSyncWithTeam == null && reachSettings != null)
                {
                    _respawnSyncWithTeam = new SharedProperties(1) { Value = Convert.ToBoolean(reachSettings?.RespawnSyncwithteam) };
                }
                return _respawnSyncWithTeam;
            }
            set
            {
                bool newValue = Convert.ToBoolean(value.Value);
                if (reachSettings != null && Convert.ToBoolean(reachSettings.RespawnSyncwithteam) != newValue)
                {
                    reachSettings.RespawnSyncwithteam = Convert.ToInt32(newValue);
                    _respawnSyncWithTeam = new SharedProperties(1) { Value = newValue };
                    OnPropertyChanged(nameof(RespawnSyncWithTeam));
                }
            }
        }

        // Repeat the pattern for other properties in ReachSettings
        // Implement INotifyPropertyChanged

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SpawnH2AH4SettingsViewModel : INotifyPropertyChanged
    {
        private SpawnSettings.H2AH4Settings h2AH4Settings;

        public SpawnH2AH4SettingsViewModel(SpawnSettings.H2AH4Settings h2AH4Settings)
        {
            this.h2AH4Settings = h2AH4Settings;
        }

        public int? MinRespawnTime
        {
            get { return h2AH4Settings?.MinRespawnTime; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.MinRespawnTime != value)
                {
                    h2AH4Settings.MinRespawnTime = value;
                    OnPropertyChanged(nameof(MinRespawnTime));
                }
            }
        }

        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class H2AH4SettingsViewModel : INotifyPropertyChanged
    {
        private ModeSettings.H2AH4Settings h2AH4Settings;

        public H2AH4SettingsViewModel(ModeSettings.H2AH4Settings h2AH4Settings)
        {
            this.h2AH4Settings = h2AH4Settings;
        }


        public int? Bit4
        {
            get { return h2AH4Settings?.Bit4; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.Bit4 != value)
                {
                    h2AH4Settings.Bit4 = value;
                    OnPropertyChanged(nameof(Bit4));
                }
            }
        }

        public int? Bit5
        {
            get { return h2AH4Settings?.Bit5; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.Bit5 != value)
                {
                    h2AH4Settings.Bit5 = value;
                    OnPropertyChanged(nameof(Bit5));
                }
            }
        }

        public int? MoshDifficulty
        {
            get { return h2AH4Settings?.MoshDifficulty; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.MoshDifficulty != value)
                {
                    h2AH4Settings.MoshDifficulty = value;
                    OnPropertyChanged(nameof(MoshDifficulty));
                }
            }
        }

        public int? ProtoMode
        {
            get { return h2AH4Settings?.ProtoMode; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.ProtoMode != value)
                {
                    h2AH4Settings.ProtoMode = value;
                    OnPropertyChanged(nameof(ProtoMode));
                }
            }
        }

        public int? ProtoVersion
        {
            get { return h2AH4Settings?.Unknown4; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.Unknown4 != value)
                {
                    h2AH4Settings.Unknown4 = value;
                    OnPropertyChanged(nameof(ProtoVersion));
                }
            }
        }

        public int? ClassColorOverride
        {
            get { return h2AH4Settings?.ClassColorOverride; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.ClassColorOverride != value)
                {
                    h2AH4Settings.ClassColorOverride = value;
                    OnPropertyChanged(nameof(ClassColorOverride));
                }
            }
        }

        public int? InheritRespawnTime
        {
            get { return h2AH4Settings?.InheritRespawnTime; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.InheritRespawnTime != value)
                {
                    h2AH4Settings.InheritRespawnTime = value;
                    OnPropertyChanged(nameof(InheritRespawnTime));
                }
            }
        }

        public int? Unknown4
        {
            get { return h2AH4Settings?.Unknown4; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.Unknown4 != value)
                {
                    h2AH4Settings.Unknown4 = value;
                    OnPropertyChanged(nameof(Unknown4));
                }
            }
        }

        public int? KillCamEnabled
        {
            get { return h2AH4Settings?.KillCamEnabled; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.KillCamEnabled != value)
                {
                    h2AH4Settings.KillCamEnabled = value;
                    OnPropertyChanged(nameof(KillCamEnabled));
                }
            }
        }

        public int? PointsSystemEnabled
        {
            get { return h2AH4Settings?.PointsSystemEnabled; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.PointsSystemEnabled != value)
                {
                    h2AH4Settings.PointsSystemEnabled = value;
                    OnPropertyChanged(nameof(PointsSystemEnabled));
                }
            }
        }

        public int? FinalKillCamEnabled
        {
            get { return h2AH4Settings?.FinalKillCamEnabled; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.FinalKillCamEnabled != value)
                {
                    h2AH4Settings.FinalKillCamEnabled = value;
                    OnPropertyChanged(nameof(FinalKillCamEnabled));
                }
            }
        }

        public int? Unknown2
        {
            get { return h2AH4Settings?.Unknown2; }
            set
            {
                if (h2AH4Settings != null && h2AH4Settings.Unknown2 != value)
                {
                    h2AH4Settings.Unknown2 = value;
                    OnPropertyChanged(nameof(Unknown2));
                }
            }
        }


        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //public class GameSettings
    //{
    //    public int EnableObservers;
    //    public int Teamchanging;
    //    public int FriendlyFire;
    //    public int? BetrayalBooting;
    //    public int? ProximityVoice;
    //    public int Dontrestrictteamvoicechat;
    //    public int? allowdeadplayerstotalk;
    //    public int Indestructiblevehicles;
    //    public int turretsonmap;
    //    public int powerupsonmap;
    //    public int abilitiesonmap;
    //    public int shortcutsonmap;
    //    public int? grenadesonmap;
    //    public PlayerTraits BasePlayerTraits { get; set; }
    //    public int WeaponSet;
    //    public int VehicleSet;
    //    public int? EquipmentSet;
    //    public int? Unknown1;
    //    public int? Unknown2;
    //    public int? Unknown3;
    //    public string? Unknown4;




    //}

    public class H2AH4GameSettingsViewModel
    {
        
        public GameSettings.H2AH4Settings gameSettings;
        public int EquipmentSet
        {
            get { return gameSettings.EquipmentSet; }
            set
            {
                if (gameSettings.EquipmentSet != value)
                {
                    gameSettings.EquipmentSet = value;
                    OnPropertyChanged(nameof(EquipmentSet));
                }
            }
        }

        public int Unknown1
        {
            get { return gameSettings.Unknown1; }
            set
            {
                if (gameSettings.Unknown1 != value)
                {
                    gameSettings.Unknown1 = value;
                    OnPropertyChanged(nameof(Unknown1));
                }
            }
        }

        public int Unknown2
        {
            get { return gameSettings.Unknown2; }
            set
            {
                if (gameSettings.Unknown2 != value)
                {
                    gameSettings.Unknown2 = value;
                    OnPropertyChanged(nameof(Unknown2));
                }
            }
        }

        public int Unknown3
        {
            get { return gameSettings.Unknown3; }
            set
            {
                if (gameSettings.Unknown3 != value)
                {
                    gameSettings.Unknown3 = value;
                    OnPropertyChanged(nameof(Unknown3));
                }
            }
        }

        public string? Unknown4
        {
            get { return gameSettings.Unknown4; }
            set
            {
                if (gameSettings.Unknown4 != value)
                {
                    gameSettings.Unknown4 = value;
                    OnPropertyChanged(nameof(Unknown4));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GameSettingsViewModel : INotifyPropertyChanged
    {
        private GameSettings gameSettings;

        public GameSettingsViewModel(GameSettings gameSettings)
        {
            this.gameSettings = gameSettings;
            BasePlayerTraits = new PlayerTraitsViewModel(gameSettings.BasePlayerTraits);
        }

        public bool EnableObservers
        {
            get { return Convert.ToBoolean(gameSettings.EnableObservers); }
            set
            {
                if (Convert.ToBoolean(gameSettings.EnableObservers) != value)
                {
                    gameSettings.EnableObservers = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(EnableObservers));
                }
            }
        }

        public bool TeamChanging
        {
            get { return Convert.ToBoolean(gameSettings.Teamchanging); }
            set
            {
                if (Convert.ToBoolean(gameSettings.Teamchanging) != value)
                {
                    gameSettings.Teamchanging = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(TeamChanging));
                }
            }
        }

        public bool FriendlyFire
        {
            get { return Convert.ToBoolean(gameSettings.FriendlyFire); }
            set
            {
                if (Convert.ToBoolean(gameSettings.FriendlyFire) != value)
                {
                    gameSettings.FriendlyFire = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(FriendlyFire));
                }
            }
        }

        public bool BetrayalBooting
        {
            get { return Convert.ToBoolean(gameSettings.BetrayalBooting); }
            set
            {
                if (Convert.ToBoolean(gameSettings.BetrayalBooting) != value)
                {
                    gameSettings.BetrayalBooting = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(BetrayalBooting));
                }
            }
        }

        public bool ProximityVoice
        {
            get { return Convert.ToBoolean(gameSettings.ProximityVoice); }
            set
            {
                if (Convert.ToBoolean(gameSettings.ProximityVoice) != value)
                {
                    gameSettings.ProximityVoice = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(ProximityVoice));
                }
            }
        }

        public bool DontRestrictTeamVoiceChat
        {
            get { return Convert.ToBoolean(gameSettings.Dontrestrictteamvoicechat); }
            set
            {
                if (Convert.ToBoolean(gameSettings.Dontrestrictteamvoicechat) != value)
                {
                    gameSettings.Dontrestrictteamvoicechat = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(DontRestrictTeamVoiceChat));
                }
            }
        }

        public bool AllowDeadPlayersToTalk
        {
            get { return Convert.ToBoolean(gameSettings.allowdeadplayerstotalk); }
            set
            {
                if (Convert.ToBoolean(gameSettings.allowdeadplayerstotalk) != value)
                {
                    gameSettings.allowdeadplayerstotalk = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(AllowDeadPlayersToTalk));
                }
            }
        }

        public bool IndestructibleVehicles
        {
            get { return Convert.ToBoolean(gameSettings.Indestructiblevehicles); }
            set
            {
                if (Convert.ToBoolean(gameSettings.Indestructiblevehicles) != value)
                {
                    gameSettings.Indestructiblevehicles = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(IndestructibleVehicles));
                }
            }
        }

        public bool TurretsOnMap
        {
            get { return Convert.ToBoolean(gameSettings.turretsonmap); }
            set
            {
                if (Convert.ToBoolean(gameSettings.turretsonmap) != value)
                {
                    gameSettings.turretsonmap = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(TurretsOnMap));
                }
            }
        }

        public bool PowerupsOnMap
        {
            get { return Convert.ToBoolean(gameSettings.powerupsonmap); }
            set
            {
                if (Convert.ToBoolean(gameSettings.powerupsonmap) != value)
                {
                    gameSettings.powerupsonmap = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(PowerupsOnMap));
                }
            }
        }

        public bool AbilitiesOnMap
        {
            get { return Convert.ToBoolean(gameSettings.abilitiesonmap); }
            set
            {
                if (Convert.ToBoolean(gameSettings.abilitiesonmap) != value)
                {
                    gameSettings.abilitiesonmap = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(AbilitiesOnMap));
                }
            }
        }

        public bool ShortcutsOnMap
        {
            get { return Convert.ToBoolean(gameSettings.shortcutsonmap); }
            set
            {
                if (Convert.ToBoolean(gameSettings.shortcutsonmap) != value)
                {
                    gameSettings.shortcutsonmap = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(ShortcutsOnMap));
                }
            }
        }

        public bool GrenadesOnMap
        {
            get { return Convert.ToBoolean(gameSettings.grenadesonmap); }
            set
            {
                if (Convert.ToBoolean(gameSettings.grenadesonmap) != value)
                {
                    gameSettings.grenadesonmap = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(GrenadesOnMap));
                }
            }
        }

        public PlayerTraitsViewModel BasePlayerTraits { get; set; }

        public WeaponSetEnum WeaponSet
        {
            get { return (WeaponSetEnum)gameSettings.WeaponSet; }
            set
            {
                if ((WeaponSetEnum)gameSettings.WeaponSet != value)
                {
                    gameSettings.WeaponSet = (int)value;
                    OnPropertyChanged(nameof(WeaponSet));
                }
            }
        }

        public VehicleSetEnum VehicleSet
        {
            get { return (VehicleSetEnum)gameSettings.VehicleSet; }
            set
            {
                if ((VehicleSetEnum)gameSettings.VehicleSet != value)
                {
                    gameSettings.VehicleSet = (int)value;
                    OnPropertyChanged(nameof(VehicleSet));
                }
            }
        }



        

        // Implement INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}
