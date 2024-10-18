using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using UniversalGametypeEditor.Properties;
using static UniversalGametypeEditor.ReadGametype;

namespace UniversalGametypeEditor
{

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class BitSizeAttribute : Attribute
    {
        public int Bits { get; }
        public Type DictionaryType { get; }

        public BitSizeAttribute(int bits)
        {
            Bits = bits;
        }

        public BitSizeAttribute(int bits, Type dictionaryType)
        {
            Bits = bits;
            DictionaryType = dictionaryType;
        }
    }

    




    public class FileHeaderViewModel : INotifyPropertyChanged
    {
        private FileHeader data;
        


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


        [BitSize(32)]
        public dynamic Mpvr
        {
            get
            {
                return data.mpvr;
            }
            set
            {
                if (data.mpvr != value)
                {
                    data.mpvr = value;
                    OnPropertyChanged(nameof(Mpvr));
                }
            }
        }

        [BitSize(32)]
        public dynamic MegaloVersion
        {
            get
            {
                return data.megaloversion;
            }
            set
            {
                if (data.megaloversion != value)
                {
                    data.megaloversion = value;
                    OnPropertyChanged(nameof(MegaloVersion));
                }
            }
        }


        [BitSize(16)]
        public dynamic Unknown0x2F8
        {
            get
            {
                return data.Unknown0x2F8;
            }
            set
            {
                if (data.Unknown0x2F8 != value)
                {
                    data.Unknown0x2F8 = value;
                    OnPropertyChanged(nameof(Unknown0x2F8));
                }
            }
        }

        [BitSize(16)]
        public dynamic Unknown0x2FA
        {
            get
            {
                return data.Unknown0x2FA;
            }
            set
            {
                if (data.Unknown0x2FA != value)
                {
                    data.Unknown0x2FA = value;
                    OnPropertyChanged(nameof(Unknown0x2FA));
                }
            }
        }

        [BitSize(160)]
        public dynamic UnknownHash0x2FC
        {
            get
            {
                return data.UnknownHash0x2FC;
            }
            set
            {
                if (data.UnknownHash0x2FC != value)
                {
                    data.UnknownHash0x2FC = value;
                    OnPropertyChanged(nameof(UnknownHash0x2FC));
                }
            }
        }

        [BitSize(32)]
        public dynamic Blank0x310
        {
            get
            {
                return data.Blank0x310;
            }
            set
            {
                if (data.Blank0x310 != value)
                {
                    data.Blank0x310 = value;
                    OnPropertyChanged(nameof(Blank0x310));
                }
            }
        }

        [BitSize(32)]
        public int FileUsedSize
        {
            get
            {
                return data.Fileusedsize;
            }
            set
            {
                if (data.Fileusedsize != value)
                {
                    data.Fileusedsize = value;
                    OnPropertyChanged(nameof(FileUsedSize));
                }
            }
        }

        [BitSize(2)]
        public int Unknown0x318
        {
            get
            {
                return data.Unknown0x318;
            }
            set
            {
                if (data.Unknown0x318 != value)
                {
                    data.Unknown0x318 = value;
                    OnPropertyChanged(nameof(Unknown0x318));
                }
            }
        }

        [BitSize(2)]
        public VariantTypeEnum VariantType
        {
            get
            {
                return (VariantTypeEnum)data.VariantType;
            }
            set
            {
                if (data.VariantType != (int)value)
                {
                    data.VariantType = (int)value;
                    OnPropertyChanged(nameof(VariantType));
                }
            }
        }

        [BitSize(4)]
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
        [BitSize(32)]
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
        [BitSize(32)]
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
        [BitSize(32)]
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
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


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
        [BitSize(64)]
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
        [BitSize(64)]
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
        [BitSize(64)]
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
        [BitSize(64)]
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
        [BitSize(8)]
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
        [BitSize(32)]
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
        [BitSize(8)]
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
        [BitSize(32)]
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
        [BitSize(32)]
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
        [BitSize(64)]
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
        [BitSize(33)]
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
        [BitSize(32)]
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
        [BitSize(64)]
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
        [BitSize(1)]
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
        [BitSize(2)]
        public IconEnum GameIcon
        {
            get
            {
                return (IconEnum)data.GameIcon;
            }
            set
            {
                if (data.GameIcon != (int)value)
                {
                    data.GameIcon = (int)value;
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



    public class ModeSettingsViewModel : INotifyPropertyChanged
    {
        private ModeSettings data;

        public ModeSettingsViewModel(ModeSettings data)
        {
            this.data = data;
            Reach = new ReachSettingsViewModel(data.Reach);
            H2AH4 = new H2AH4SettingsViewModel(data.H2AH4);
        }
        [BitSize(1)]
        public bool UnknownFlag2
        {
            get
            {
                return Convert.ToBoolean(data.UnknownFlag2);
            }
            set
            {
                if (Convert.ToBoolean(data.UnknownFlag2) != value)
                {
                    data.UnknownFlag2 = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(UnknownFlag2));
                }
            }
        }
        [BitSize(1)]
        public bool TeamsEnabled
        {
            get
            {
                return Convert.ToBoolean(data.Teamsenabled);
            }
            set
            {
                if (Convert.ToBoolean(data.Teamsenabled) != value)
                {
                    data.Teamsenabled = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(TeamsEnabled));
                }
            }
        }

        [BitSize(1)]
        public bool ResetMapOnNewRoundUnused
        {
            get
            {
                return Convert.ToBoolean(data.Resetmaponnewroundunused);
            }
            set
            {
                if (Convert.ToBoolean(data.Resetmaponnewroundunused) != value)
                {
                    data.Resetmaponnewroundunused = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(ResetMapOnNewRoundUnused));
                }
            }
        }
        [BitSize(1)]
        public bool ResetPlayersOnNewRoundUnused
        {
            get
            {
                return Convert.ToBoolean(data.Resetplayersonnewroundunused);
            }
            set
            {
                if (Convert.ToBoolean(data.Resetplayersonnewroundunused) != value)
                {
                    data.Resetplayersonnewroundunused = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(ResetPlayersOnNewRoundUnused));
                }
            }
        }
        [BitSize(1)]
        public bool PerfectionMedalEnabled
        {
            get
            {
                return Convert.ToBoolean(data.Perfectionmedalenabled);
            }
            set
            {
                if (Convert.ToBoolean(data.Perfectionmedalenabled) != value)
                {
                    data.Perfectionmedalenabled = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(PerfectionMedalEnabled));
                }
            }
        }

        [BitSize(8)]
        public int RoundTimeLimit
        {
            get
            {
                return data.RoundTimeLimit;
            }
            set
            {
                if (data.RoundTimeLimit != value)
                {
                    data.RoundTimeLimit = value;
                    OnPropertyChanged(nameof(RoundTimeLimit));
                }
            }
        }
        [BitSize(5)]
        public int NumberOfRounds
        {
            get
            {
                return data.NumberOfRounds;
            }
            set
            {
                if (data.NumberOfRounds != value)
                {
                    data.NumberOfRounds = value;
                    OnPropertyChanged(nameof(NumberOfRounds));
                }
            }
        }
        [BitSize(4)]
        public int RoundsToWin
        {
            get
            {
                return data.RoundsToWin;
            }
            set
            {
                if (data.RoundsToWin != value)
                {
                    data.RoundsToWin = value;
                    OnPropertyChanged(nameof(RoundsToWin));
                }
            }
        }

        public H2AH4SettingsViewModel H2AH4 { get; set; }


        [BitSize(7)]
        public int? SuddenDeathTime
        {
            get
            {
                return data.SuddenDeathTime;
            }
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


        public SpawnSettingsViewModel(SpawnSettings data)
        {
            this.data = data;
            RespawnPlayerTraits = new PlayerTraitsViewModel(data.RespawnPlayerTraits);
            H2AH4 = new SpawnH2AH4SettingsViewModel(data.H2AH4);
            Reach = new SpawnReachSettingsViewModel(data.Reach);
        }

        public SpawnReachSettingsViewModel Reach { get; set; }
        [BitSize(6)]
        public int LivesPerround
        {
            get
            {
                return data.LivesPerround;
            }
            set
            {
                if (data.LivesPerround != value)
                {
                    data.LivesPerround = value;
                    OnPropertyChanged(nameof(LivesPerround));
                }
            }
        }
        [BitSize(7)]
        public int TeamLivesPerround
        {
            get
            {
                return data.TeamLivesPerround;
            }
            set
            {
                if (data.TeamLivesPerround != value)
                {
                    data.TeamLivesPerround = value;
                    OnPropertyChanged(nameof(TeamLivesPerround));
                }
            }
        }


        public SpawnH2AH4SettingsViewModel H2AH4 { get; set; }
        [BitSize(8)]
        public int RespawnTime
        {
            get
            {
                return data.RespawnTime;
            }
            set
            {
                if (data.RespawnTime != value)
                {
                    data.RespawnTime = value;
                    OnPropertyChanged(nameof(RespawnTime));
                }
            }
        }
        [BitSize(8)]
        public int Suicidepenalty
        {
            get
            {
                return data.Suicidepenalty;
            }
            set
            {
                if (data.Suicidepenalty != value)
                {
                    data.Suicidepenalty = value;
                    OnPropertyChanged(nameof(Suicidepenalty));
                }
            }
        }
        [BitSize(8)]
        public int Betrayalpenalty
        {
            get
            {
                return data.Betrayalpenalty;
            }
            set
            {
                if (data.Betrayalpenalty != value)
                {
                    data.Betrayalpenalty = value;
                    OnPropertyChanged(nameof(Betrayalpenalty));
                }
            }
        }
        [BitSize(4)]
        public int RespawnTimegrowth
        {
            get
            {
                return data.RespawnTimegrowth;
            }
            set
            {
                if (data.RespawnTimegrowth != value)
                {
                    data.RespawnTimegrowth = value;
                    OnPropertyChanged(nameof(RespawnTimegrowth));
                }
            }
        }
        [BitSize(4)]
        public int LoadoutCamTime
        {
            get
            {
                return data.LoadoutCamTime;
            }
            set
            {
                if (data.LoadoutCamTime != value)
                {
                    data.LoadoutCamTime = value;
                    OnPropertyChanged(nameof(LoadoutCamTime));
                }
            }
        }
        [BitSize(6)]
        public int Respawntraitsduration
        {
            get
            {
                return data.Respawntraitsduration;
            }
            set
            {
                if (data.Respawntraitsduration != value)
                {
                    data.Respawntraitsduration = value;
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

    public class PlayerTraitsViewModel
    {
        private PlayerTraits data;


        public PlayerTraitsViewModel(PlayerTraits data)
        {
            this.data = data;
            H2AH4 = new TraitsH2AH4SettingsViewModel(data.H2AH4);

        }



        [BitSize(4, typeof(DamageResistanceStrings))]
        public DamageResistanceEnum DamageResistance
        {
            get
            {
                return (DamageResistanceEnum)data.DamageResistance;
            }
            set
            {
                if (data.DamageResistance != (int)value)
                {
                    data.DamageResistance = (int)value;
                    OnPropertyChanged(nameof(DamageResistance));
                }
            }
        }

        [BitSize(3, typeof(HealthMultiplierStrings))]
        public HealthMultiplierEnum HealthMultiplyer
        {
            get
            {
                return (HealthMultiplierEnum)data.Healthmultiplyer;
            }
            set
            {
                if (data.Healthmultiplyer != (int)value)
                {
                    data.Healthmultiplyer = (int)value;
                    OnPropertyChanged(nameof(HealthMultiplyer));
                }
            }
        }

        [BitSize(4, typeof(RegenStrings))]
        public RegenEnum HealthRegenRate
        {
            get
            {
                return (RegenEnum)data.Healthregenrate;
            }
            set
            {
                if (data.Healthregenrate != (int)value)
                {
                    data.Healthregenrate = (int)value;
                    OnPropertyChanged(nameof(HealthRegenRate));
                }
            }
        }

        [BitSize(3, typeof(ShieldMultiplyerStrings))]
        public ShieldMultiplyerEnum ShieldMultiplyer
        {
            get
            {
                return (ShieldMultiplyerEnum)data.ShieldMultiplyer;
            }
            set
            {
                if (data.ShieldMultiplyer != (int)value)
                {
                    data.ShieldMultiplyer = (int)value;
                    OnPropertyChanged(nameof(ShieldMultiplyer));
                }
            }
        }


        [BitSize(4, typeof(RegenStrings))]
        public RegenEnum ShieldRegenRate
        {
            get
            {
                return (RegenEnum)data.ShieldRegenrate;
            }
            set
            {
                if (data.ShieldRegenrate != (int)value)
                {
                    data.ShieldRegenrate = (int)value;
                    OnPropertyChanged(nameof(ShieldRegenRate));
                }
            }
        }


        [BitSize(4, typeof(RegenStrings))]
        public RegenEnum OvershieldRegenRate
        {
            get
            {
                return (RegenEnum)data.Overshieldregenrate;
            }
            set
            {
                if (data.Overshieldregenrate != (int)value)
                {
                    data.Overshieldregenrate = (int)value;
                    OnPropertyChanged(nameof(OvershieldRegenRate));
                }
            }
        }


        [BitSize(2, typeof(ToggleEnumStrings))]
        public ToggleEnum HeadshotImmunity
        {
            get
            {
                return (ToggleEnum)data.HeadshotImmunity;
            }
            set
            {
                if (data.HeadshotImmunity != (int)value)
                {
                    data.HeadshotImmunity = (int)value;
                    OnPropertyChanged(nameof(HeadshotImmunity));
                }
            }
        }


        [BitSize(3, typeof(VampirismStrings))]
        public VampirismEnum ShieldVampirism
        {
            get
            {
                return (VampirismEnum)data.shieldvampirism;
            }
            set
            {
                if (data.shieldvampirism != (int)value)
                {
                    data.shieldvampirism = (int)value;
                    OnPropertyChanged(nameof(ShieldVampirism));
                }
            }
        }


        [BitSize(2, typeof(ToggleEnumStrings))]
        public ToggleEnum AssassinationImmunity
        {
            get
            {
                return (ToggleEnum)data.Assasinationimmunity;
            }
            set
            {
                if (data.Assasinationimmunity != (int)value)
                {
                    data.Assasinationimmunity = (int)value;
                    OnPropertyChanged(nameof(AssassinationImmunity));
                }
            }
        }


        [BitSize(2, typeof(ToggleEnumStrings))]
        public ToggleEnum Invincible
        {
            get
            {
                return (ToggleEnum)data.invincible;
            }
            set
            {
                if (data.invincible != (int)value)
                {
                    data.invincible = (int)value;
                    OnPropertyChanged(nameof(Invincible));
                }
            }
        }

        [BitSize(4, typeof(DamageStrings))]
        public DamageEnum WeaponDamageMultiplier
        {
            get
            {
                return (DamageEnum)data.WeaponDamagemultiplier;
            }
            set
            {
                if (data.WeaponDamagemultiplier != (int)value)
                {
                    data.WeaponDamagemultiplier = (int)value;
                    OnPropertyChanged(nameof(WeaponDamageMultiplier));
                }
            }
        }

        [BitSize(4, typeof(DamageStrings))]
        public DamageEnum MeleeDamageMultiplier
        {
            get
            {
                return (DamageEnum)data.MeleeDamagemultiplier;
            }
            set
            {
                if (data.MeleeDamagemultiplier != (int)value)
                {
                    data.MeleeDamagemultiplier = (int)value;
                    OnPropertyChanged(nameof(MeleeDamageMultiplier));
                }
            }
        }

        [BitSize(8, typeof(WeaponStrings))]
        public WeaponEnum PrimaryWeapon
        {
            get
            {
                return (WeaponEnum)data.Primaryweapon;
            }
            set
            {
                if (data.Primaryweapon != (int)value)
                {
                    data.Primaryweapon = (int)value;
                    OnPropertyChanged(nameof(PrimaryWeapon));
                }
            }
        }


        [BitSize(8, typeof(WeaponStrings))]
        public WeaponEnum SecondaryWeapon
        {
            get
            {
                return (WeaponEnum)data.Secondaryweapon;
            }
            set
            {
                if (data.Secondaryweapon != (int)value)
                {
                    data.Secondaryweapon = (int)value;
                    OnPropertyChanged(nameof(SecondaryWeapon));
                }
            }
        }
        [BitSize(4)]
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
        [BitSize(2)]
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
        [BitSize(2)]
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
        [BitSize(2)]
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
        [BitSize(2)]
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
        [BitSize(2)]
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
        [BitSize(2)]
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
        [BitSize(8)]
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
        [BitSize(5)]
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
        [BitSize(4)]
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
        [BitSize(4)]
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
        [BitSize(2)]
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
        [BitSize(1)]
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
        [BitSize(9)]
        public int? JumpOverride
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
        [BitSize(3)]
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
        [BitSize(2)]
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
        [BitSize(2)]
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
        [BitSize(3)]
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
        [BitSize(4)]
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
        [BitSize(3)]
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
        [BitSize(3)]
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
        [BitSize(2)]
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




    public class ReachSettingsViewModel : INotifyPropertyChanged
    {
        private ModeSettings.ReachSettings reachSettings;


        public ReachSettingsViewModel(ModeSettings.ReachSettings reachSettings)
        {
            this.reachSettings = reachSettings;
        }
        [BitSize(5)]

        public int GracePeriod
        {
            get
            {
                return reachSettings != null ? Convert.ToInt32(reachSettings.GracePeriod) : 0;
            }
            set
            {
                if (reachSettings != null && Convert.ToInt32(reachSettings.GracePeriod) != value)
                {
                    reachSettings.GracePeriod = Convert.ToInt32(value);
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

        [BitSize(1)]
        public bool RespawnOnKills
        {
            get
            {
                return Convert.ToBoolean(reachSettings?.RespawnOnKills);
            }
            set
            {
                if (reachSettings != null && Convert.ToBoolean(reachSettings.RespawnOnKills) != value)
                {
                    reachSettings.RespawnOnKills = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(RespawnOnKills));
                }
            }
        }
        [BitSize(1)]
        public bool RespawnAtLocationUnused
        {
            get
            {
                return Convert.ToBoolean(reachSettings?.respawnatlocationunused);
            }
            set
            {
                if (reachSettings != null && Convert.ToBoolean(reachSettings.respawnatlocationunused) != value)
                {
                    reachSettings.respawnatlocationunused = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(RespawnAtLocationUnused));
                }
            }
        }
        [BitSize(1)]
        public bool RespawnWithTeammateUnused
        {
            get
            {
                return Convert.ToBoolean(reachSettings?.respawnwithteammateunused);
            }
            set
            {
                if (reachSettings != null && Convert.ToBoolean(reachSettings.respawnwithteammateunused) != value)
                {
                    reachSettings.respawnwithteammateunused = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(RespawnWithTeammateUnused));
                }
            }
        }
        [BitSize(1)]
        public bool RespawnSyncWithTeam
        {
            get
            {
                return Convert.ToBoolean(reachSettings?.RespawnSyncwithteam);
            }
            set
            {
                if (reachSettings != null && Convert.ToBoolean(reachSettings.RespawnSyncwithteam) != value)
                {
                    reachSettings.RespawnSyncwithteam = Convert.ToInt32(value);
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
        [BitSize(1)]
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
        [BitSize(2)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(1)]
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
        [BitSize(8)]
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
        [BitSize(8)]
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

    public class PowerupTraitsViewModel : INotifyPropertyChanged
    {
        private PowerupTraits powerupTraits;

        public PowerupTraitsViewModel(PowerupTraits powerupTraits)
        {
            this.powerupTraits = powerupTraits;
            Reach = new ReachPowerupSettings(powerupTraits);
        }

        public ReachPowerupSettings Reach { get; set; }



        // Implement INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class ReachPowerupSettings : INotifyPropertyChanged
    {
        private PowerupTraits.ReachSettings powerupTraits;

        public ReachPowerupSettings(PowerupTraits reachPowerupSettings)
        {
            this.powerupTraits = reachPowerupSettings.Reach;
            RedPlayerTraits = new PlayerTraitsViewModel(powerupTraits.RedPlayerTraits);
            BluePlayerTraits = new PlayerTraitsViewModel(powerupTraits.BluePlayerTraits);
            YellowPlayerTraits = new PlayerTraitsViewModel(powerupTraits.YellowPlayerTraits);
        }

        public PlayerTraitsViewModel RedPlayerTraits { get; set; }
        public PlayerTraitsViewModel BluePlayerTraits { get; set; }
        public PlayerTraitsViewModel YellowPlayerTraits { get; set; }
        [BitSize(7)]
        public int? RedPowerupDuration
        {
            get { return powerupTraits.RedPowerupDuration; }
            set
            {
                if (powerupTraits.RedPowerupDuration != value)
                {
                    powerupTraits.RedPowerupDuration = value;
                    OnPropertyChanged(nameof(RedPowerupDuration));
                }
            }
        }
        [BitSize(7)]
        public int? BluePowerupDuration
        {
            get { return powerupTraits.BluePowerupDuration; }
            set
            {
                if (powerupTraits.BluePowerupDuration != value)
                {
                    powerupTraits.BluePowerupDuration = value;
                    OnPropertyChanged(nameof(BluePowerupDuration));
                }
            }
        }
        [BitSize(7)]
        public int? YellowPowerupDuration
        {
            get { return powerupTraits.YellowPowerupDuration; }
            set
            {
                if (powerupTraits.YellowPowerupDuration != value)
                {
                    powerupTraits.YellowPowerupDuration = value;
                    OnPropertyChanged(nameof(YellowPowerupDuration));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class TeamSettingsViewModel : INotifyPropertyChanged
    {
        private TeamSettings teamSettings;

        public TeamSettingsViewModel(TeamSettings teamSettings)
        {
            this.teamSettings = teamSettings;
            Team1Options = new TeamOptionsViewModel(teamSettings.Team1Options);
            Team2Options = new TeamOptionsViewModel(teamSettings.Team2Options);
            Team3Options = new TeamOptionsViewModel(teamSettings.Team3Options);
            Team4Options = new TeamOptionsViewModel(teamSettings.Team4Options);
            Team5Options = new TeamOptionsViewModel(teamSettings.Team5Options);
            Team6Options = new TeamOptionsViewModel(teamSettings.Team6Options);
            Team7Options = new TeamOptionsViewModel(teamSettings.Team7Options);
            Team8Options = new TeamOptionsViewModel(teamSettings.Team8Options);
        }

        [BitSize(3)]

        public int TeamScoringMethod
        {
            get { return teamSettings.TeamScoringMethod; }
            set
            {
                if (teamSettings.TeamScoringMethod != value)
                {
                    teamSettings.TeamScoringMethod = value;
                    OnPropertyChanged(nameof(TeamScoringMethod));
                }
            }
        }
        [BitSize(3)]
        public int PlayerSpecies
        {
            get { return teamSettings.PlayerSpecies; }
            set
            {
                if (teamSettings.PlayerSpecies != value)
                {
                    teamSettings.PlayerSpecies = value;
                    OnPropertyChanged(nameof(PlayerSpecies));
                }
            }
        }

        [BitSize(2)]

        public int DesignatorSwitchType
        {
            get { return teamSettings.DesignatorSwitchtype; }
            set
            {
                if (teamSettings.DesignatorSwitchtype != value)
                {
                    teamSettings.DesignatorSwitchtype = value;
                    OnPropertyChanged(nameof(DesignatorSwitchType));
                }
            }
        }

        public TeamOptionsViewModel Team1Options { get; set; }
        public TeamOptionsViewModel Team2Options { get; set; }
        public TeamOptionsViewModel Team3Options { get; set; }
        public TeamOptionsViewModel Team4Options { get; set; }
        public TeamOptionsViewModel Team5Options { get; set; }
        public TeamOptionsViewModel Team6Options { get; set; }
        public TeamOptionsViewModel Team7Options { get; set; }
        public TeamOptionsViewModel Team8Options { get; set; }


        //Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class TeamOptionsViewModel : INotifyPropertyChanged
    {
        public TeamOptions teamOptions;

        public TeamOptionsViewModel(TeamOptions teamOptions)
        {
            this.teamOptions = teamOptions;
        }

        [BitSize(1)]
        public bool TertiarycolorOverride
        {
            get { return Convert.ToBoolean(teamOptions.TertiarycolorOverride); }
            set
            {
                if (Convert.ToBoolean(teamOptions.TertiarycolorOverride) != value)
                {
                    teamOptions.TertiarycolorOverride = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(TertiarycolorOverride));
                }
            }
        }

        [BitSize(1)]
        public bool SecondarycolorOverride
        {
            get { return Convert.ToBoolean(teamOptions.SecondarycolorOverride); }
            set
            {
                if (Convert.ToBoolean(teamOptions.SecondarycolorOverride) != value)
                {
                    teamOptions.SecondarycolorOverride = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(SecondarycolorOverride));
                }
            }
        }

        [BitSize(1)]
        public bool PrimarycolorOverride
        {
            get { return Convert.ToBoolean(teamOptions.PrimarycolorOverride); }
            set
            {
                if (Convert.ToBoolean(teamOptions.PrimarycolorOverride) != value)
                {
                    teamOptions.PrimarycolorOverride = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(PrimarycolorOverride));
                }
            }
        }

        [BitSize(1)]
        public bool TeamEnabled
        {
            get { return Convert.ToBoolean(teamOptions.TeamEnabled); }
            set
            {
                if (Convert.ToBoolean(teamOptions.TeamEnabled) != value)
                {
                    teamOptions.TeamEnabled = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(TeamEnabled));
                }
            }
        }
        [BitSize(1)]
        public string Teamstring
        {
            get { return teamOptions.Teamstring; }
            set
            {
                if (teamOptions.Teamstring != value)
                {
                    teamOptions.Teamstring = value;
                    OnPropertyChanged(nameof(Teamstring));
                }
            }
        }

        [BitSize(4)]
        public int InitialDesignator
        {
            get { return teamOptions.InitialDesignator; }
            set
            {
                if (teamOptions.InitialDesignator != value)
                {
                    teamOptions.InitialDesignator = value;
                    OnPropertyChanged(nameof(InitialDesignator));
                }
            }
        }

        [BitSize(1)]
        public int Elitespecies
        {
            get { return teamOptions.Elitespecies; }
            set
            {
                if (teamOptions.Elitespecies != value)
                {
                    teamOptions.Elitespecies = value;
                    OnPropertyChanged(nameof(Elitespecies));
                }
            }
        }

        [BitSize(32)]
        public string PrimaryColor
        {
            get { return teamOptions.PrimaryColor; }
            set
            {
                if (teamOptions.PrimaryColor != value)
                {
                    teamOptions.PrimaryColor = value;
                    OnPropertyChanged(nameof(PrimaryColor));
                }
            }
        }

        [BitSize(32)]
        public string SecondaryColor
        {
            get { return teamOptions.SecondaryColor; }
            set
            {
                if (teamOptions.SecondaryColor != value)
                {
                    teamOptions.SecondaryColor = value;
                    OnPropertyChanged(nameof(SecondaryColor));
                }
            }
        }

        [BitSize(32)]
        public string TertiaryColor
        {
            get { return teamOptions.TertiaryColor; }
            set
            {
                if (teamOptions.TertiaryColor != value)
                {
                    teamOptions.TertiaryColor = value;
                    OnPropertyChanged(nameof(TertiaryColor));
                }
            }
        }

        [BitSize(5)]
        public int FireteamCount
        {
            get { return teamOptions.FireteamCount; }
            set
            {
                if (teamOptions.FireteamCount != value)
                {
                    teamOptions.FireteamCount = value;
                    OnPropertyChanged(nameof(FireteamCount));
                }
            }
        }

        //Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LoadoutClusterViewModel : INotifyPropertyChanged
    {
        private LoadoutCluster loadoutCluster;

        public LoadoutClusterViewModel(LoadoutCluster loadoutCluster)
        {
            this.loadoutCluster = loadoutCluster;
            SpartanLoadout1 = new LoadoutViewModel(loadoutCluster.Loadout1);
            SpartanLoadout2 = new LoadoutViewModel(loadoutCluster.Loadout2);
            SpartanLoadout3 = new LoadoutViewModel(loadoutCluster.Loadout3);
            SpartanLoadout4 = new LoadoutViewModel(loadoutCluster.Loadout4);
            SpartanLoadout5 = new LoadoutViewModel(loadoutCluster.Loadout5);
            SpartanLoadout6 = new LoadoutViewModel(loadoutCluster.Loadout6);
            SpartanLoadout7 = new LoadoutViewModel(loadoutCluster.Loadout7);
            SpartanLoadout8 = new LoadoutViewModel(loadoutCluster.Loadout8);
            SpartanLoadout9 = new LoadoutViewModel(loadoutCluster.Loadout9);
            SpartanLoadout10 = new LoadoutViewModel(loadoutCluster.Loadout10);
            SpartanLoadout11 = new LoadoutViewModel(loadoutCluster.Loadout11);
            SpartanLoadout12 = new LoadoutViewModel(loadoutCluster.Loadout12);
            SpartanLoadout13 = new LoadoutViewModel(loadoutCluster.Loadout13);
            SpartanLoadout14 = new LoadoutViewModel(loadoutCluster.Loadout14);
            SpartanLoadout15 = new LoadoutViewModel(loadoutCluster.Loadout15);
            EliteLoadout1 = new LoadoutViewModel(loadoutCluster.Loadout16);
            EliteLoadout2 = new LoadoutViewModel(loadoutCluster.Loadout17);
            EliteLoadout3 = new LoadoutViewModel(loadoutCluster.Loadout18);
            EliteLoadout4 = new LoadoutViewModel(loadoutCluster.Loadout19);
            EliteLoadout5 = new LoadoutViewModel(loadoutCluster.Loadout20);
            EliteLoadout6 = new LoadoutViewModel(loadoutCluster.Loadout21);
            EliteLoadout7 = new LoadoutViewModel(loadoutCluster.Loadout22);
            EliteLoadout8 = new LoadoutViewModel(loadoutCluster.Loadout23);
            EliteLoadout9 = new LoadoutViewModel(loadoutCluster.Loadout24);
            EliteLoadout10 = new LoadoutViewModel(loadoutCluster.Loadout25);
            EliteLoadout11 = new LoadoutViewModel(loadoutCluster.Loadout26);
            EliteLoadout12 = new LoadoutViewModel(loadoutCluster.Loadout27);
            EliteLoadout13 = new LoadoutViewModel(loadoutCluster.Loadout28);
            EliteLoadout14 = new LoadoutViewModel(loadoutCluster.Loadout29);
            EliteLoadout15 = new LoadoutViewModel(loadoutCluster.Loadout30);

        }
        [BitSize(1)]
        public bool EliteLoadoutsEnabled
        {
            get { return Convert.ToBoolean(loadoutCluster.EliteLoadoutsEnabled); }
            set
            {
                if (Convert.ToBoolean(loadoutCluster.EliteLoadoutsEnabled) != value)
                {
                    loadoutCluster.EliteLoadoutsEnabled = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(EliteLoadoutsEnabled));
                }
            }
        }
        [BitSize(1)]
        public bool SpartanLoadoutsEnabled
        {
            get { return Convert.ToBoolean(loadoutCluster.SpartanLoadoutsEnabled); }
            set
            {
                if (Convert.ToBoolean(loadoutCluster.SpartanLoadoutsEnabled) != value)
                {
                    loadoutCluster.SpartanLoadoutsEnabled = Convert.ToInt32(value);
                    OnPropertyChanged(nameof(SpartanLoadoutsEnabled));
                }
            }
        }

        public LoadoutViewModel SpartanLoadout1 { get; set; }
        public LoadoutViewModel SpartanLoadout2 { get; set; }
        public LoadoutViewModel SpartanLoadout3 { get; set; }
        public LoadoutViewModel SpartanLoadout4 { get; set; }
        public LoadoutViewModel SpartanLoadout5 { get; set; }
        public LoadoutViewModel SpartanLoadout6 { get; set; }
        public LoadoutViewModel SpartanLoadout7 { get; set; }
        public LoadoutViewModel SpartanLoadout8 { get; set; }

        public LoadoutViewModel SpartanLoadout9 { get; set; }
        public LoadoutViewModel SpartanLoadout10 { get; set; }
        public LoadoutViewModel SpartanLoadout11 { get; set; }
        public LoadoutViewModel SpartanLoadout12 { get; set; }
        public LoadoutViewModel SpartanLoadout13 { get; set; }
        public LoadoutViewModel SpartanLoadout14 { get; set; }
        public LoadoutViewModel SpartanLoadout15 { get; set; }
        public LoadoutViewModel EliteLoadout1 { get; set; }
        public LoadoutViewModel EliteLoadout2 { get; set; }
        public LoadoutViewModel EliteLoadout3 { get; set; }
        public LoadoutViewModel EliteLoadout4 { get; set; }
        public LoadoutViewModel EliteLoadout5 { get; set; }
        public LoadoutViewModel EliteLoadout6 { get; set; }
        public LoadoutViewModel EliteLoadout7 { get; set; }
        public LoadoutViewModel EliteLoadout8 { get; set; }
        public LoadoutViewModel EliteLoadout9 { get; set; }
        public LoadoutViewModel EliteLoadout10 { get; set; }
        public LoadoutViewModel EliteLoadout11 { get; set; }
        public LoadoutViewModel EliteLoadout12 { get; set; }
        public LoadoutViewModel EliteLoadout13 { get; set; }
        public LoadoutViewModel EliteLoadout14 { get; set; }
        public LoadoutViewModel EliteLoadout15 { get; set; }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LoadoutViewModel : INotifyPropertyChanged
    {
        private LoadoutOptions loadout;

        public LoadoutViewModel(LoadoutOptions loadout)
        {
            this.loadout = loadout;
        }
        [BitSize(1)]
        public int LoadoutVisibleInGame
        {
            get { return loadout.LoadoutVisibleingame; }
            set
            {
                if (loadout.LoadoutVisibleingame != value)
                {
                    loadout.LoadoutVisibleingame = value;
                    OnPropertyChanged(nameof(LoadoutVisibleInGame));
                }
            }
        }
        [BitSize(1)]
        public int LoadoutName
        {
            get { return loadout.LoadoutName; }
            set
            {
                if (loadout.LoadoutName != value)
                {
                    loadout.LoadoutName = value;
                    OnPropertyChanged(nameof(LoadoutName));
                }
            }
        }
        [BitSize(8)]
        public WeaponEnum PrimaryWeapon
        {
            get { return (WeaponEnum)loadout.PrimaryWeapon; }
            set
            {
                if ((WeaponEnum)loadout.PrimaryWeapon != value)
                {
                    loadout.PrimaryWeapon = (int)value;
                    OnPropertyChanged(nameof(PrimaryWeapon));
                }
            }
        }
        [BitSize(8)]
        public WeaponEnum SecondaryWeapon
        {
            get { return (WeaponEnum)loadout.SecondaryWeapon; }
            set
            {
                if ((WeaponEnum)loadout.SecondaryWeapon != value)
                {
                    loadout.SecondaryWeapon = (int)value;
                    OnPropertyChanged(nameof(SecondaryWeapon));
                }
            }
        }
        [BitSize(8)]
        public int ArmorAbility
        {
            get { return loadout.Armorability; }
            set
            {
                if (loadout.Armorability != value)
                {
                    loadout.Armorability = value;
                    OnPropertyChanged(nameof(ArmorAbility));
                }
            }
        }
        [BitSize(4)]
        public int Grenades
        {
            get { return loadout.Grenades; }
            set
            {
                if (loadout.Grenades != value)
                {
                    loadout.Grenades = value;
                    OnPropertyChanged(nameof(Grenades));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ScriptedPlayerTraitsViewModel : INotifyPropertyChanged
    {
        private int count;

        [BitSize(5)]
        public int Count
        {
            get => count;
            set
            {
                count = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        public ObservableCollection<ScriptedPlayerTraitItemViewModel> PlayerTraitsItems { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ScriptedPlayerTraitItemViewModel : INotifyPropertyChanged
    {
        private ScriptedPlayerTraits data;

        public ScriptedPlayerTraitItemViewModel(ScriptedPlayerTraits data)
        {
            this.data = data;
            PlayerTraits = new PlayerTraitsViewModel(data.PlayerTraits);
        }

        [BitSize(7)]
        public int String1
        {
            get => data.String1;
            set
            {
                data.String1 = value;
                OnPropertyChanged(nameof(String1));
            }
        }

        [BitSize(7)]
        public int String2
        {
            get => data.String2;
            set
            {
                data.String2 = value;
                OnPropertyChanged(nameof(String2));
            }
        }

        public PlayerTraitsViewModel PlayerTraits { get; set; }

        [BitSize(1)]
        public int? Hidden
        {
            get => data?.H2AH4?.hidden;
            set
            {
                if (value.HasValue)
                {
                    data.H2AH4.hidden = value.Value;
                    OnPropertyChanged(nameof(Hidden));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


public class ScriptOptionsViewModel : INotifyPropertyChanged
    {
        private int count;

        [BitSize(5)]
        public int Count
        {
            get => count;
            set
            {
                count = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        public ObservableCollection<ScriptOptionItemViewModel> ScriptOptionItems { get; set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


public class ScriptOptionItemViewModel : INotifyPropertyChanged
    {
        private ScriptOptions data;

        public ScriptOptionItemViewModel(ScriptOptions data)
        {
            this.data = data;
        }

        [BitSize(7)]
        public int String1
        {
            get => data.String1;
            set
            {
                data.String1 = value;
                OnPropertyChanged(nameof(String1));
            }
        }

        [BitSize(7)]
        public int String2
        {
            get => data.String2;
            set
            {
                data.String2 = value;
                OnPropertyChanged(nameof(String2));
            }
        }

        [BitSize(1)]
        public int ScriptOption
        {
            get => data.ScriptOption;
            set
            {
                data.ScriptOption = value;
                OnPropertyChanged(nameof(ScriptOption));
            }
        }

        [BitSize(3)]
        public int ChildIndex
        {
            get => data.ChildIndex;
            set
            {
                data.ChildIndex = value;
                OnPropertyChanged(nameof(ChildIndex));
            }
        }

        [BitSize(4)]
        public int ScriptOptionChild
        {
            get => data.ScriptOptionChild;
            set
            {
                data.ScriptOptionChild = value;
                OnPropertyChanged(nameof(ScriptOptionChild));
            }
        }

        [BitSize(10)]
        public int Value
        {
            get => data.Value;
            set
            {
                data.Value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        [BitSize(3)]
        public int Unknown
        {
            get => data.Unknown;
            set
            {
                data.Unknown = value;
                OnPropertyChanged(nameof(Unknown));
            }
        }

        [BitSize(4)]
        public int ActualChildIndex
        {
            get => data.ActualChildIndex;
            set
            {
                data.ActualChildIndex = value;
                OnPropertyChanged(nameof(ActualChildIndex));
            }
        }

        [BitSize(10)]
        public int Range1
        {
            get => data.range1;
            set
            {
                data.range1 = value;
                OnPropertyChanged(nameof(Range1));
            }
        }

        [BitSize(10)]
        public int Range2
        {
            get => data.range2;
            set
            {
                data.range2 = value;
                OnPropertyChanged(nameof(Range2));
            }
        }

        [BitSize(10)]
        public int Range3
        {
            get => data.range3;
            set
            {
                data.range3 = value;
                OnPropertyChanged(nameof(Range3));
            }
        }

        [BitSize(10)]
        public int Range4
        {
            get => data.range4;
            set
            {
                data.range4 = value;
                OnPropertyChanged(nameof(Range4));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StringsViewModel : INotifyPropertyChanged
    {
        private string _stringTable;
        private int _stringNameIndex;
        private string _metaNameStrings;
        private string _metaDescStrings;
        private string _metaIntroStrings;
        private string _metaGroupStrings;

        [BitSize(16)]
        public string StringTable
        {
            get => _stringTable;
            set { _stringTable = value; OnPropertyChanged(nameof(StringTable)); }
        }

        [BitSize(8)]
        public int StringNameIndex
        {
            get => _stringNameIndex;
            set { _stringNameIndex = value; OnPropertyChanged(nameof(StringNameIndex)); }
        }

        [BitSize(11)]
        public string MetaNameStrings
        {
            get => _metaNameStrings;
            set { _metaNameStrings = value; OnPropertyChanged(nameof(MetaNameStrings)); }
        }

        [BitSize(13)]
        public string MetaDescStrings
        {
            get => _metaDescStrings;
            set { _metaDescStrings = value; OnPropertyChanged(nameof(MetaDescStrings)); }
        }

        [BitSize(13)]
        public string MetaIntroStrings
        {
            get => _metaIntroStrings;
            set { _metaIntroStrings = value; OnPropertyChanged(nameof(MetaIntroStrings)); }
        }

        [BitSize(10)]
        public string MetaGroupStrings
        {
            get => _metaGroupStrings;
            set { _metaGroupStrings = value; OnPropertyChanged(nameof(MetaGroupStrings)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GameViewModel : INotifyPropertyChanged
    {
        private int _actualGameIcon;
        private int _actualGameCategory;

        [BitSize(5)]
        public int ActualGameIcon
        {
            get => _actualGameIcon;
            set { _actualGameIcon = value; OnPropertyChanged(nameof(ActualGameIcon)); }
        }

        [BitSize(5)]
        public int ActualGameCategory
        {
            get => _actualGameCategory;
            set { _actualGameCategory = value; OnPropertyChanged(nameof(ActualGameCategory)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MapViewModel : INotifyPropertyChanged
    {
        private string _mapVariantID;
        private int _mapCount;
        private int _mapperMsFlip;

        [BitSize(16)]
        public string MapVariantID
        {
            get => _mapVariantID;
            set { _mapVariantID = value; OnPropertyChanged(nameof(MapVariantID)); }
        }

        [BitSize(6)]
        public int MapCount
        {
            get => _mapCount;
            set { _mapCount = value; OnPropertyChanged(nameof(MapCount)); }
        }

        [BitSize(1)]
        public int MapperMsFlip
        {
            get => _mapperMsFlip;
            set { _mapperMsFlip = value; OnPropertyChanged(nameof(MapperMsFlip)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PlayerRatingsViewModel : INotifyPropertyChanged
    {
        private int _ratingScale;
        private int _killWeight;
        private int _assistWeight;
        private int _betrayalWeight;
        private int _deathWeight;
        private int _normalizeByMaxKills;
        private int _baseRating;
        private int _range;
        private int _lossScalar;
        private int _customStat0;
        private int _customStat1;
        private int _customStat2;
        private int _customStat3;
        private int _expansion0;
        private int _expansion1;
        private int _showPlayerRatings;

        [BitSize(32)]
        public int RatingScale
        {
            get => _ratingScale;
            set { _ratingScale = value; OnPropertyChanged(nameof(RatingScale)); }
        }

        [BitSize(32)]
        public int KillWeight
        {
            get => _killWeight;
            set { _killWeight = value; OnPropertyChanged(nameof(KillWeight)); }
        }

        [BitSize(32)]
        public int AssistWeight
        {
            get => _assistWeight;
            set { _assistWeight = value; OnPropertyChanged(nameof(AssistWeight)); }
        }

        [BitSize(32)]
        public int BetrayalWeight
        {
            get => _betrayalWeight;
            set { _betrayalWeight = value; OnPropertyChanged(nameof(BetrayalWeight)); }
        }

        [BitSize(32)]
        public int DeathWeight
        {
            get => _deathWeight;
            set { _deathWeight = value; OnPropertyChanged(nameof(DeathWeight)); }
        }

        [BitSize(32)]
        public int NormalizeByMaxKills
        {
            get => _normalizeByMaxKills;
            set { _normalizeByMaxKills = value; OnPropertyChanged(nameof(NormalizeByMaxKills)); }
        }

        [BitSize(32)]
        public int BaseRating
        {
            get => _baseRating;
            set { _baseRating = value; OnPropertyChanged(nameof(BaseRating)); }
        }

        [BitSize(32)]
        public int Range
        {
            get => _range;
            set { _range = value; OnPropertyChanged(nameof(Range)); }
        }

        [BitSize(32)]
        public int LossScalar
        {
            get => _lossScalar;
            set { _lossScalar = value; OnPropertyChanged(nameof(LossScalar)); }
        }

        [BitSize(32)]
        public int CustomStat0
        {
            get => _customStat0;
            set { _customStat0 = value; OnPropertyChanged(nameof(CustomStat0)); }
        }

        [BitSize(32)]
        public int CustomStat1
        {
            get => _customStat1;
            set { _customStat1 = value; OnPropertyChanged(nameof(CustomStat1)); }
        }

        [BitSize(32)]
        public int CustomStat2
        {
            get => _customStat2;
            set { _customStat2 = value; OnPropertyChanged(nameof(CustomStat2)); }
        }

        [BitSize(32)]
        public int CustomStat3
        {
            get => _customStat3;
            set { _customStat3 = value; OnPropertyChanged(nameof(CustomStat3)); }
        }

        [BitSize(32)]
        public int Expansion0
        {
            get => _expansion0;
            set { _expansion0 = value; OnPropertyChanged(nameof(Expansion0)); }
        }

        [BitSize(32)]
        public int Expansion1
        {
            get => _expansion1;
            set { _expansion1 = value; OnPropertyChanged(nameof(Expansion1)); }
        }

        [BitSize(1)]
        public int ShowPlayerRatings
        {
            get => _showPlayerRatings;
            set { _showPlayerRatings = value; OnPropertyChanged(nameof(ShowPlayerRatings)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ConditionViewModel : INotifyPropertyChanged
    {
        private int _conditionType;
        private int _varType1;
        private string _specificType;
        private int _varType2;
        private string _specificType2;
        private int _oper;
        private bool _not;
        private int _orSequence;

        // References
        private string _playerRef;
        private string _teamRef;
        private string _objectRef;
        private string _timerRef;
        private string _boundary;
        private string _label;
        private string _allegiance;
        private string _deathFlags;
        private string _type1;

        // Indices and Values
        private int? _numericValue;          // Represents Int16 or other numeric values
        private int? _playerRefIndex;        // Represents 5-bit Player reference index
        private int? _playerNumberIndex;     // Represents 3-bit Player number index
        private int? _objectRefIndex;        // Represents 5-bit Object reference index
        private int? _objectNumberIndex;     // Represents 3-bit Object number index
        private int? _teamRefIndex;          // Represents 5-bit Team reference index
        private int? _teamNumberIndex;       // Represents 3-bit Team number index
        private int? _globalNumberIndex;     // Represents 4-bit Global number index

        // Properties with OnPropertyChanged
        public int ConditionType
        {
            get => _conditionType;
            set
            {
                _conditionType = value;
                OnPropertyChanged(nameof(ConditionType));
            }
        }

        public int VarType1
        {
            get => _varType1;
            set
            {
                _varType1 = value;
                OnPropertyChanged(nameof(VarType1));
            }
        }

        public string SpecificType
        {
            get => _specificType;
            set
            {
                _specificType = value;
                OnPropertyChanged(nameof(SpecificType));
            }
        }

        public int VarType2
        {
            get => _varType2;
            set
            {
                _varType2 = value;
                OnPropertyChanged(nameof(VarType2));
            }
        }

        public string SpecificType2
        {
            get => _specificType2;
            set
            {
                _specificType2 = value;
                OnPropertyChanged(nameof(SpecificType2));
            }
        }

        public int Oper
        {
            get => _oper;
            set
            {
                _oper = value;
                OnPropertyChanged(nameof(Oper));
            }
        }

        public bool Not
        {
            get => _not;
            set
            {
                _not = value;
                OnPropertyChanged(nameof(Not));
            }
        }

        public int OrSequence
        {
            get => _orSequence;
            set
            {
                _orSequence = value;
                OnPropertyChanged(nameof(OrSequence));
            }
        }

        // Player Reference (5-bit) and Number Index (3-bit)
        public int? PlayerRefIndex
        {
            get => _playerRefIndex;
            set
            {
                _playerRefIndex = value;
                OnPropertyChanged(nameof(PlayerRefIndex));
            }
        }

        public int? PlayerNumberIndex
        {
            get => _playerNumberIndex;
            set
            {
                _playerNumberIndex = value;
                OnPropertyChanged(nameof(PlayerNumberIndex));
            }
        }

        // Team Reference (5-bit) and Number Index (3-bit)
        public int? TeamRefIndex
        {
            get => _teamRefIndex;
            set
            {
                _teamRefIndex = value;
                OnPropertyChanged(nameof(TeamRefIndex));
            }
        }

        public int? TeamNumberIndex
        {
            get => _teamNumberIndex;
            set
            {
                _teamNumberIndex = value;
                OnPropertyChanged(nameof(TeamNumberIndex));
            }
        }

        // Object Reference (5-bit) and Number Index (3-bit)
        public int? ObjectRefIndex
        {
            get => _objectRefIndex;
            set
            {
                _objectRefIndex = value;
                OnPropertyChanged(nameof(ObjectRefIndex));
            }
        }

        public int? ObjectNumberIndex
        {
            get => _objectNumberIndex;
            set
            {
                _objectNumberIndex = value;
                OnPropertyChanged(nameof(ObjectNumberIndex));
            }
        }

        // Global Number Index (4-bit)
        public int? GlobalNumberIndex
        {
            get => _globalNumberIndex;
            set
            {
                _globalNumberIndex = value;
                OnPropertyChanged(nameof(GlobalNumberIndex));
            }
        }

        // Numeric Value (Int16 or other numeric types)
        public int? NumericValue
        {
            get => _numericValue;
            set
            {
                _numericValue = value;
                OnPropertyChanged(nameof(NumericValue));
            }
        }

        // Other References
        public string PlayerRef
        {
            get => _playerRef;
            set
            {
                _playerRef = value;
                OnPropertyChanged(nameof(PlayerRef));
            }
        }

        public string TeamRef
        {
            get => _teamRef;
            set
            {
                _teamRef = value;
                OnPropertyChanged(nameof(TeamRef));
            }
        }

        public string ObjectRef
        {
            get => _objectRef;
            set
            {
                _objectRef = value;
                OnPropertyChanged(nameof(ObjectRef));
            }
        }

        public string TimerRef
        {
            get => _timerRef;
            set
            {
                _timerRef = value;
                OnPropertyChanged(nameof(TimerRef));
            }
        }

        public string Boundary
        {
            get => _boundary;
            set
            {
                _boundary = value;
                OnPropertyChanged(nameof(Boundary));
            }
        }

        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                OnPropertyChanged(nameof(Label));
            }
        }

        public string Allegiance
        {
            get => _allegiance;
            set
            {
                _allegiance = value;
                OnPropertyChanged(nameof(Allegiance));
            }
        }

        public string DeathFlags
        {
            get => _deathFlags;
            set
            {
                _deathFlags = value;
                OnPropertyChanged(nameof(DeathFlags));
            }
        }

        public string Type1
        {
            get => _type1;
            set
            {
                _type1 = value;
                OnPropertyChanged(nameof(Type1));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


















}
