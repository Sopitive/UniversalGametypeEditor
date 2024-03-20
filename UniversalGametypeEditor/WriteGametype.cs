using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UniversalGametypeEditor.Properties;
using static UniversalGametypeEditor.ReadGametype;

namespace UniversalGametypeEditor
{
    public class WriteGametype
    {
        private string rawBinary = "";
        private string modifiedBinary = "";
        
        public void WriteBinaryFile(string filename, GametypeHeader gt, FileHeaderViewModel fh, GametypeHeaderViewModel gh, ModeSettingsViewModel ms, SpawnSettingsViewModel ss)
        {
            //Write the bytes for the file, starting at file offset 2F0
            byte[] bytes = File.ReadAllBytes(filename);

            //Get all bytes up to file offset 2F0
            string prebits;
            prebits = string.Join("", bytes.Take(752).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            if (Settings.Default.IsGvar)
            {
                prebits = string.Join("", bytes.Take(136).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            }
            //Convert to a string of 0s and 1s starting at file offset 2F0
            rawBinary = string.Join("", bytes.Skip(752).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            if (Settings.Default.IsGvar)
            {
                rawBinary = string.Join("", bytes.Skip(136).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            }
            WriteFileHeaders(fh);
            int len = WriteGametypeHeaders(gh, gt);
            if (Settings.Default.DecompiledVersion == 0)
            {
                WriteModeSettings(ms);
                WriteSpawnSettings(ss);
            }
            
            int slice = modifiedBinary.Length - len;


            
            rawBinary = rawBinary[slice..];
            rawBinary = modifiedBinary + rawBinary;
            rawBinary = prebits + rawBinary;
            //Convert raw binary string to bytes
            byte[] newBytes = Enumerable.Range(0, rawBinary.Length / 8).Select(i => Convert.ToByte(rawBinary.Substring(i * 8, 8), 2)).ToArray();
            //Write the bytes to the file
            File.WriteAllBytes(filename, newBytes);
        }
        
        public void WriteFileHeaders(FileHeaderViewModel fh)
        {
            string mpvr = fh.Mpvr.Value;
            string megaloversion = Convert.ToString(fh.MegaloVersion.Value, 2).PadLeft(32, '0');
            string Unknown0x2F8 = Convert.ToString(fh.Unknown0x2F8, 2).PadLeft(16, '0');
            string Unknown0x2FA = Convert.ToString(fh.Unknown0x2FA, 2).PadLeft(16, '0');
            string UnknownHash0x2FC = fh.UnknownHash0x2FC.Value;
            string Blank0x310 = fh.Blank0x310.Value;
            string Fileusedsize = Convert.ToString(fh.FileUsedSize, 2).PadLeft(32, '0');
            string Unknown0x318 = Convert.ToString(fh.Unknown0x318, 2).PadLeft(2, '0');


            // Convert variant type to string and then to binary
            string VariantType = Convert.ToString((int)fh.VariantType.Value, 2).PadLeft(2, '0');

            string Unknown0x319 = Convert.ToString(fh.Unknown0x319, 2).PadLeft(4, '0');
            string Unknown0x31D = Convert.ToString(fh.Unknown0x31D, 2).PadLeft(32, '0');
            string Unknown0x31C = Convert.ToString(fh.Unknown0x31C, 2).PadLeft(32, '0');
            string FileLength = Convert.ToString(fh.FileLength, 2).PadLeft(32, '0');

            if (Settings.Default.IsGvar == false)
            {
                modifiedBinary = mpvr + megaloversion + Unknown0x2F8 + Unknown0x2FA + UnknownHash0x2FC + Blank0x310 + Fileusedsize + Unknown0x318 + VariantType + Unknown0x319 + Unknown0x31D + Unknown0x31C + FileLength;
            }
            if (Settings.Default.IsGvar == true)
            {
                modifiedBinary = Unknown0x2F8 + Unknown0x2FA + Unknown0x318 + VariantType + Unknown0x319 + Unknown0x31D + Unknown0x31C + FileLength;
            }
        }

        private int WriteGametypeHeaders(GametypeHeaderViewModel gh, GametypeHeader gt)
        {
            //if (Settings.Default.IsGvar)
            //{
            //    return 0;
            //}
            int gamertaglen = (gh.Gamertag.Length * 8);
            int gamertaglen2 = (gh.EditGamertag.Length * 8);
            int titlelen = (gh.Title.Length * 2);
            int desclen = (gh.Description.Length * 2) ;
            int modlen = 0;
            string sliced = "";
            string prebits = "";
            gh.Gamertag = gh.Gamertag == null ? "?" : gh.Gamertag;
            gh.EditGamertag = gh.EditGamertag == null ? "?" : gh.EditGamertag;
            gh.Title = gh.Title == null ? "?" : gh.Title;
            gh.Description = gh.Description == null ? "?" : gh.Description;

            string ID0x48 = gh.ID0x48;
            string ID0x50 = gh.ID0x50;
            string ID0x58 = gh.ID0x58;
            string Blank0x60 = gh.Blank0x60;
            string UnknownFlags = gh.UnknownFlags;
            string Unknown_1 = Convert.ToString(gh.Unknown_1, 2).PadLeft(32, '0');
            string Unknown0x1 = Convert.ToString(gh.Unknown0x1, 2).PadLeft(8, '0');
            string Blank04 = Convert.ToString(gh.Blank04, 2).PadLeft(32, '0');
            string TimeStampUint = Convert.ToString(gh.TimeStampUint, 2).PadLeft(32, '0');
            string XUID = gh.XUID;
            modifiedBinary += ID0x48 + ID0x50 + ID0x58 + Blank0x60 + UnknownFlags + Unknown_1 + Unknown0x1 + Blank04 + TimeStampUint + XUID;
            modlen = modifiedBinary.Length;
            string Gamertag = ConvertASCIItoBinary(gh.Gamertag);
            modifiedBinary = modifiedBinary.Insert(modlen, Gamertag);




            string Blank041bit = gh.Blank041bit;
            string EditTimeStampUint = Convert.ToString(gh.EditTimeStampUint, 2).PadLeft(32, '0');
            string EditXUID = gh.EditXUID;
            modifiedBinary += Blank041bit + EditTimeStampUint + EditXUID;
            modlen = modifiedBinary.Length;
            string EditGamertag = ConvertASCIItoBinary(gh.EditGamertag);
            modifiedBinary = modifiedBinary.Insert(modlen, EditGamertag);


            string UnknownFlag1 = Convert.ToString(gh.UnknownFlag1, 2).PadLeft(1, '0');
            modifiedBinary += UnknownFlag1;
            modlen = modifiedBinary.Length;
            string Title = ConvertASCIItoBinary2(gh.Title);
            Title += "0000000000000000";
            modifiedBinary = modifiedBinary.Insert(modlen, Title);
            modlen = modifiedBinary.Length;
            string Description = ConvertASCIItoBinary2(gh.Description);
            Description += "0000000000000000";
            //Description = Description[..8];
            modifiedBinary = modifiedBinary.Insert(modlen, Description);
            int length = Gamertag.Length + EditGamertag.Length + Description.Length + Title.Length;
            int oldLen = (Settings.Default.Description.Length * 16 + 16) + (Settings.Default.Title.Length * 16 + 16) + (Settings.Default.Gamertag.Length * 8 + 8) + (Settings.Default.EditGamertag.Length * 8 + 8);
            int diff = length - oldLen;
            //diff = modifiedBinary.Length - diff;
            Settings.Default.Description = gh.Description;
            Settings.Default.Title = gh.Title;
            modifiedBinary += Convert.ToString((int)gh.GameIcon.Value, 2).PadLeft(8, '0');
            //modifiedBinary2 += UnknownFlag1;
            ////modlen = modifiedBinary.Length;
            //int total3 = modlen + titlelen + total + total2 + modifiedBinary2.Length;
            //sliced = modifiedBinary[total3..];
            //prebits = modifiedBinary2[..total3];
            //string Title = ConvertASCIItoBinary2(gh.Title);
            //modifiedBinary2 += Title;
            //modifiedBinary2 += sliced;
            //modifiedBinary = modifiedBinary2;
            //modifiedBinary2 = "";
            ////modlen = modifiedBinary.Length;
            //int total4 = modlen + desclen + total + total2 + total3 + modifiedBinary2.Length;
            //sliced = modifiedBinary[total4..];
            //string Description = ConvertASCIItoBinary2(gh.Description);
            //modifiedBinary2 += Description;
            //modifiedBinary2 += sliced;
            //modifiedBinary = modifiedBinary2;
            //modifiedBinary2 = "";
            //int total5 = modlen + total + total2 + total3 + total4;


            //string GameIcon = Convert.ToString(gh.GameIcon, 2).PadLeft(8, '0');
            //modifiedBinary += GameIcon;

            //var header = gt.GametypeHeader;
            //var deserializedJSON = JsonConvert.DeserializeObject<ReadGametype.FileHeader>(header);
            //int length = 0;
            //deserializedJSON.GetType().GetProperties().ToList().ForEach(prop =>
            //{
            //    if ( prop.Name == "Gamertag")
            //    {
            //        length += prop.GetValue(deserializedJSON).ToString().Length;
            //    }
            //});
            //length *= 8;
            //modifiedBinary += ID0x48 + ID0x50 + ID0x58 + Blank0x60 + UnknownFlags + Unknown_1 + Unknown0x1 + Blank04 + TimeStampUint + XUID + Gamertag;
            
            return diff;
        }

        private void WriteModeSettings(ModeSettingsViewModel ms)
        {
            // Get all properties of ModeSettingsViewModel
            var properties = typeof(ModeSettingsViewModel).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(SharedProperties))
                {
                    // Handle SharedProperties
                    var value = (SharedProperties)prop.GetValue(ms);
                    var val = Convert.ToInt32(value.Value);
                    var bits = value.Bits;
                    var binary = Convert.ToString(val, 2).PadLeft(bits, '0');
                    modifiedBinary += binary;
                }
                else if (prop.PropertyType == typeof(ReachSettingsViewModel) && Settings.Default.DecompiledVersion == 0)
                {
                    // Handle ReachSettingsViewModel
                    var reachViewModel = (ReachSettingsViewModel)prop.GetValue(ms);
                    if (reachViewModel != null)
                    {
                        // Get the GracePeriod property of ReachSettingsViewModel
                        var gracePeriodProp = typeof(ReachSettingsViewModel).GetProperty("GracePeriod");
                        if (gracePeriodProp != null)
                        {
                            // Handle SharedProperties.GracePeriod
                            var gracePeriod = (SharedProperties)gracePeriodProp.GetValue(reachViewModel);
                            var val = Convert.ToInt32(gracePeriod.Value);
                            var bits = gracePeriod.Bits;
                            var binary = Convert.ToString(val, 2).PadLeft(bits, '0');
                            modifiedBinary += binary;
                        }
                    }
                }
            }
        }

        private void WriteSpawnSettings(object viewModel)
        {
            // Get properties of the ViewModel
            var properties = viewModel.GetType().GetProperties();

            foreach (var prop in properties)
            {
                // Check if the property is of type SharedProperties
                if (prop.PropertyType == typeof(SharedProperties))
                {
                    var value = (SharedProperties)prop.GetValue(viewModel);
                    if (value != null)
                    {
                        var val = Convert.ToInt32(value.Value);
                        var bits = value.Bits;
                        var binary = Convert.ToString(val, 2).PadLeft(bits, '0');
                        modifiedBinary += binary;
                    }
                    
                }
                // Check if the property is a ViewModel that contains SharedProperties
                else if (prop.PropertyType == typeof(SpawnReachSettingsViewModel) || prop.PropertyType == typeof(PlayerTraitsViewModel))
                {
                    var nestedViewModel = prop.GetValue(viewModel);
                    if (nestedViewModel != null)
                    {
                        // Recursively call WriteSpawnSettings on the nested ViewModel
                        WriteSpawnSettings(nestedViewModel);
                    }
                }
            }
        }

        private string ConvertASCIItoBinary(string input)
        {
            string output = "";
            foreach (char c in input)
            {
                output += Convert.ToString(c, 2).PadLeft(8, '0');
                
            }
            //Check output length, if not empty, add 0s to the end
            output += "00000000";
            
            
            return output;
        }

        private string ConvertASCIItoBinary2(string input)
        {
            string output = "";
            foreach (char c in input)
            {
                //Convert to unicode
                output += Convert.ToString(c, 2).PadLeft(16, '0');
            }
            return output;
        }

    }
}
