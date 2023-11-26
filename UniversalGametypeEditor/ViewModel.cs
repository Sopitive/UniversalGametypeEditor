using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class YourDataViewModel : INotifyPropertyChanged
    {
        private FileHeader data;

        public YourDataViewModel(FileHeader data)
        {
            this.data = data;
        }

        public string Mpvr
        {
            get { return data.mpvr; }
            set
            {
                if (data.mpvr != value)
                {
                    data.mpvr = value;
                    OnPropertyChanged(nameof(Mpvr));
                }
            }
        }

        public int MegaloVersion
        {
            get { return data.megaloversion; }
            set
            {
                if (data.megaloversion != value)
                {
                    data.megaloversion = value;
                    OnPropertyChanged(nameof(MegaloVersion));
                }
            }
        }

        public int Unknown0x2F8
        {
            get { return data.Unknown0x2F8; }
            set
            {
                if (data.Unknown0x2F8 != value)
                {
                    data.Unknown0x2F8 = value;
                    OnPropertyChanged(nameof(Unknown0x2F8));
                }
            }
        }

        public int Unknown0x2FA
        {
            get { return data.Unknown0x2FA; }
            set
            {
                if (data.Unknown0x2FA != value)
                {
                    data.Unknown0x2FA = value;
                    OnPropertyChanged(nameof(Unknown0x2FA));
                }
            }
        }

        public string UnknownHash0x2FC
        {
            get { return data.UnknownHash0x2FC; }
            set
            {
                if (data.UnknownHash0x2FC != value)
                {
                    data.UnknownHash0x2FC = value;
                    OnPropertyChanged(nameof(UnknownHash0x2FC));
                }
            }
        }

        public string Blank0x310
        {
            get { return data.Blank0x310; }
            set
            {
                if (data.Blank0x310 != value)
                {
                    data.Blank0x310 = value;
                    OnPropertyChanged(nameof(Blank0x310));
                }
            }
        }

        public int FileUsedSize
        {
            get { return data.Fileusedsize; }
            set
            {
                if (data.Fileusedsize != value)
                {
                    data.Fileusedsize = value;
                    OnPropertyChanged(nameof(FileUsedSize));
                }
            }
        }

        public int Unknown0x318
        {
            get { return data.Unknown0x318; }
            set
            {
                if (data.Unknown0x318 != value)
                {
                    data.Unknown0x318 = value;
                    OnPropertyChanged(nameof(Unknown0x318));
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

        public int GamertagLength
        {
            get { return gamertagLength; }
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

        public int EditGamertagLength
        {
            get { return editGamertagLength; }
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

        public int TitleLength
        {
            get { return titleLength; }
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

        public int DescriptionLength
        {
            get { return descriptionLength; }
        }

        public int GameIcon
        {
            get { return data.GameIcon; }
            set
            {
                if (data.GameIcon != value)
                {
                    data.GameIcon = value;
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

        public ModeSettingsViewModel(ModeSettings data)
        {
            this.data = data;
            Reach = new ReachSettingsViewModel(data.Reach);
    H2AH4 = new H2AH4SettingsViewModel(data.H2AH4);
        }

        public int UnknownFlag2
        {
            get { return data.UnknownFlag2; }
            set
            {
                if (data.UnknownFlag2 != value)
                {
                    data.UnknownFlag2 = value;
                    OnPropertyChanged(nameof(UnknownFlag2));
                }
            }
        }

        public int TeamsEnabled
        {
            get { return data.Teamsenabled; }
            set
            {
                if (data.Teamsenabled != value)
                {
                    data.Teamsenabled = value;
                    OnPropertyChanged(nameof(TeamsEnabled));
                }
            }
        }

        public int ResetMapOnNewRoundUnused
        {
            get { return data.Resetmaponnewroundunused; }
            set
            {
                if (data.Resetmaponnewroundunused != value)
                {
                    data.Resetmaponnewroundunused = value;
                    OnPropertyChanged(nameof(ResetMapOnNewRoundUnused));
                }
            }
        }

        public int ResetPlayersOnNewRoundUnused
        {
            get { return data.Resetplayersonnewroundunused; }
            set
            {
                if (data.Resetplayersonnewroundunused != value)
                {
                    data.Resetplayersonnewroundunused = value;
                    OnPropertyChanged(nameof(ResetPlayersOnNewRoundUnused));
                }
            }
        }

        public int PerfectionMedalEnabled
        {
            get { return data.Perfectionmedalenabled; }
            set
            {
                if (data.Perfectionmedalenabled != value)
                {
                    data.Perfectionmedalenabled = value;
                    OnPropertyChanged(nameof(PerfectionMedalEnabled));
                }
            }
        }

        public int RoundTimeLimit
        {
            get { return data.RoundTimeLimit; }
            set
            {
                if (data.RoundTimeLimit != value)
                {
                    data.RoundTimeLimit = value;
                    OnPropertyChanged(nameof(RoundTimeLimit));
                }
            }
        }

        public int NumberOfRounds
        {
            get { return data.NumberOfRounds; }
            set
            {
                if (data.NumberOfRounds != value)
                {
                    data.NumberOfRounds = value;
                    OnPropertyChanged(nameof(NumberOfRounds));
                }
            }
        }

        public int RoundsToWin
        {
            get { return data.RoundsToWin; }
            set
            {
                if (data.RoundsToWin != value)
                {
                    data.RoundsToWin = value;
                    OnPropertyChanged(nameof(RoundsToWin));

                }
            }
        }

        public int? SuddenDeathTime
        {
            get { return data.SuddenDeathTime; }
            set
            {
                if (data.SuddenDeathTime != value)
                {
                    data.SuddenDeathTime = value;
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

    public class ReachSettingsViewModel : INotifyPropertyChanged
    {
        private ModeSettings.ReachSettings reachSettings;

        public ReachSettingsViewModel(ModeSettings.ReachSettings reachSettings)
        {
            this.reachSettings = reachSettings;
        }

        //public int? GracePeriod
        //{
        //    get { return reachSettings.GracePeriod; }
        //    set
        //    {
        //        if (reachSettings.GracePeriod != value)
        //        {
        //            reachSettings.GracePeriod = value;
        //            OnPropertyChanged(nameof(GracePeriod));
        //        }
        //    }
        //}

        // Repeat the pattern for other properties in ReachSettings
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
            get { return h2AH4Settings.Bit4; }
            set
            {
                if (h2AH4Settings.Bit4 != value)
                {
                    h2AH4Settings.Bit4 = value;
                    OnPropertyChanged(nameof(Bit4));
                }
            }
        }

        // Repeat the pattern for other properties in H2AH4Settings
        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
