using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalGametypeEditor.Properties;
using static UniversalGametypeEditor.ReadGametype;

namespace UniversalGametypeEditor
{
    public class WriteGametype
    {
        private string rawBinary = "";
        private string modifiedBinary = "";
        
        public void WriteBinaryFile(string filename, GametypeHeader gt, YourDataViewModel fh, GametypeHeaderViewModel gh)
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
            int slice = modifiedBinary.Length + len;


            
            rawBinary = rawBinary[slice..];
            rawBinary = modifiedBinary + rawBinary;
            rawBinary = prebits + rawBinary;
            //Convert raw binary string to bytes
            byte[] newBytes = Enumerable.Range(0, rawBinary.Length / 8).Select(i => Convert.ToByte(rawBinary.Substring(i * 8, 8), 2)).ToArray();
            //Write the bytes to the file
            File.WriteAllBytes(filename, newBytes);
        }
        
        private void WriteFileHeaders(YourDataViewModel fh)
        {


            string mpvr = fh.Mpvr;
            string megaloversion = Convert.ToString(fh.MegaloVersion, 2).PadLeft(32, '0');
            string Unknown0x2F8 = Convert.ToString(fh.Unknown0x2F8, 2).PadLeft(16, '0');
            string Unknown0x2FA = Convert.ToString(fh.Unknown0x2FA, 2).PadLeft(16, '0');
            string UnknownHash0x2FC = fh.UnknownHash0x2FC;
            string Blank0x310 = fh.Blank0x310;
            string Fileusedsize = Convert.ToString(fh.FileUsedSize, 2).PadLeft(32, '0');
            string Unknown0x318 = Convert.ToString(fh.Unknown0x318, 2).PadLeft(2, '0');
            string VariantType = Convert.ToString(fh.VariantType, 2).PadLeft(2, '0');
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
            if (Settings.Default.IsGvar)
            {
                return 0;
            }
            int gamertaglen = (gh.Gamertag.Length * 8);
            int gamertaglen2 = (gh.EditGamertag.Length * 8);
            int titlelen = (gh.Title.Length * 2);
            int desclen = (gh.Description.Length * 2) ;
            int modlen = 0;
            string sliced = "";
            string prebits = "";

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
            modifiedBinary = modifiedBinary.Insert(modlen, ConvertASCIItoBinary(gh.Gamertag));




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
            modifiedBinary = modifiedBinary.Insert(modlen, Title);
            modlen = modifiedBinary.Length;
            string Description = ConvertASCIItoBinary2(gh.Description);
            modifiedBinary = modifiedBinary.Insert(modlen, Description);




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
            int oldLen = gh.GamertagLength*8 + gh.EditGamertagLength*8 + gh.TitleLength*16 + gh.DescriptionLength*16;
            int newLen = gh.Gamertag.Length*8 + gh.EditGamertag.Length*8 + gh.Title.Length*16 + gh.Description.Length*16;
            int length = oldLen - newLen;
            return length;
        }

        private string ConvertASCIItoBinary(string input)
        {
            string output = "";
            foreach (char c in input)
            {
                output += Convert.ToString(c, 2).PadLeft(8, '0');
                
            }
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
            output += "0000000000000000";
            return output;
        }

    }
}
