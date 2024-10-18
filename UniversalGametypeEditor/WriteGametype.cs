using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
        
        public void WriteBinaryFile(string filename, GametypeHeader gt, FileHeaderViewModel fh, GametypeHeaderViewModel gh, ModeSettingsViewModel ms, SpawnSettingsViewModel ss, GameSettingsViewModel gs, PowerupTraitsViewModel ps, TeamSettingsViewModel ts, LoadoutClusterViewModel lc)
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
            int slice = 0;
            WriteFileHeaders(fh);

            int len = WriteGametypeHeaders(gh, gt);
            modifiedBinary += WriteModeSettings(ms);
            if (Settings.Default.DecompiledVersion == 0)
            {
                modifiedBinary += WriteSpawnSettings(ss);
                modifiedBinary += WriteGameSettings(gs);
                modifiedBinary += WritePowerupSettings(ps);
                modifiedBinary += WriteTeamSettings(ts);
                modifiedBinary += WriteLoadoutPalettes(lc);
            }
            
            slice = modifiedBinary.Length;

            


            
            rawBinary = rawBinary[slice..];
            rawBinary = modifiedBinary + rawBinary;
            rawBinary = prebits + rawBinary;
            //Convert raw binary string to bytes
            byte[] newBytes = Enumerable.Range(0, rawBinary.Length / 8).Select(i => Convert.ToByte(rawBinary.Substring(i * 8, 8), 2)).ToArray();
            //Write the bytes to the file
            File.WriteAllBytes(filename, newBytes);
            System.Threading.Thread.Sleep(1);
            
        }

        public void WriteFileHeaders(FileHeaderViewModel fh)
        {
            string GetBinaryString(dynamic value, int bitSize)
            {
                if (value is string strValue)
                {
                    // Check if the string is a valid binary string
                    if (strValue.All(c => c == '0' || c == '1'))
                    {
                        return strValue.PadLeft(bitSize, '0'); // Return the binary string directly
                    }
                    else
                    {
                        // Convert the non-binary string to an integer
                        int intValue = int.Parse(strValue);
                        return Convert.ToString(intValue, 2).PadLeft(bitSize, '0');
                    }
                }
                else
                {
                    return Convert.ToString((int)value, 2).PadLeft(bitSize, '0');
                }
            }




            string GetPropertyBinaryString(FileHeaderViewModel fh, PropertyInfo property)
            {
                var bitSizeAttribute = (BitSizeAttribute)Attribute.GetCustomAttribute(property, typeof(BitSizeAttribute));
                int bitSize = bitSizeAttribute?.Bits ?? 0;
                dynamic value = property.GetValue(fh);
                return GetBinaryString(value, bitSize);
            }

            var properties = typeof(FileHeaderViewModel).GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(BitSizeAttribute)))
                .ToList();

            var binaryStrings = new List<string>();

            for (int i = 0; i < properties.Count; i++)
            {
                if (Settings.Default.IsGvar && i < 2)
                {
                    // Skip the first two properties if IsGvar is true
                    continue;
                }

                binaryStrings.Add(GetPropertyBinaryString(fh, properties[i]));
            }

            modifiedBinary = string.Join("", binaryStrings);

            // Use the modifiedBinary as needed
        }



        private int WriteGametypeHeaders(GametypeHeaderViewModel gh, GametypeHeader gt)
        {
            int modlen = 0;
            gh.Gamertag = gh.Gamertag ?? "?";
            gh.EditGamertag = gh.EditGamertag ?? "?";
            gh.Title = gh.Title ?? "?";
            gh.Description = gh.Description ?? "?";

            var properties = typeof(GametypeHeaderViewModel).GetProperties();
            foreach (var prop in properties)
            {
                var bitSizeAttribute = (BitSizeAttribute)Attribute.GetCustomAttribute(prop, typeof(BitSizeAttribute));
                int bitSize = bitSizeAttribute?.Bits ?? 0;

                var value = prop.GetValue(gh);
                string binaryValue = value switch
                {
                    string str when str.All(c => c == '0' || c == '1') => str.PadLeft(bitSize, '0'), // Already binary
                    string str => prop.Name switch
                    {
                        nameof(gh.Title) => ConvertASCIItoBinary2(str).PadLeft(bitSize, '0') + "0000000000000000",
                        nameof(gh.Description) => ConvertASCIItoBinary2(str).PadLeft(bitSize, '0') + "0000000000000000",
                        _ => ConvertASCIItoBinary(str).PadLeft(bitSize, '0')
                    },
                    int intValue => Convert.ToString(intValue, 2).PadLeft(bitSize, '0'),
                    _ => string.Empty
                };

                modifiedBinary += binaryValue;
            }

            int length = gh.Gamertag.Length * 8 + gh.EditGamertag.Length * 8 + gh.Description.Length * 16 + 16 + gh.Title.Length * 16 + 16;
            int oldLen = (Settings.Default.Description.Length * 16 + 16) + (Settings.Default.Title.Length * 16 + 16) + (Settings.Default.Gamertag.Length * 8 + 8) + (Settings.Default.EditGamertag.Length * 8 + 8);
            int diff = length - oldLen;

            Settings.Default.Description = gh.Description;
            Settings.Default.Title = gh.Title;
            modifiedBinary += Convert.ToString((int)gh.GameIcon, 2).PadLeft(8, '0');

            return diff;
        }




        private string WriteModeSettings(ModeSettingsViewModel ms)
        {
            StringBuilder modifiedBinaryBuilder = new StringBuilder();
            ProcessViewModel(ms, modifiedBinaryBuilder);
            return modifiedBinaryBuilder.ToString();
        }



        public string WriteSpawnSettings(object viewModel)
        {
            StringBuilder modifiedBinary = new StringBuilder();
            ProcessViewModel(viewModel, modifiedBinary);
            return modifiedBinary.ToString();
        }

        public string WriteGameSettings(object viewModel)
        {
            StringBuilder modifiedBinary = new StringBuilder();
            ProcessViewModel(viewModel, modifiedBinary);
            return modifiedBinary.ToString();
        }

        public string WritePowerupSettings(object viewModel)
        {
            StringBuilder modifiedBinary = new StringBuilder();
            ProcessViewModel(viewModel, modifiedBinary);
            return modifiedBinary.ToString();
        }

        private string WriteTeamSettings(object viewModel)
        {
            StringBuilder modifiedBinary = new StringBuilder();
            ProcessViewModel(viewModel, modifiedBinary);
            return modifiedBinary.ToString();
        }

        public string WriteLoadoutPalettes(LoadoutClusterViewModel lc)
        {
            StringBuilder modifiedBinary = new StringBuilder();
            ProcessViewModel(lc, modifiedBinary);
            return modifiedBinary.ToString();
        }



        private void ProcessViewModel(object viewModel, StringBuilder modifiedBinary)
        {
            if (viewModel == null)
            {
                return; // Skip processing if viewModel is null
            }

            var properties = viewModel.GetType().GetProperties();
            foreach (var property in properties)
            {
                try
                {
                    var bitSizeAttribute = property.GetCustomAttributes(typeof(BitSizeAttribute), false).FirstOrDefault() as BitSizeAttribute;
                    if (bitSizeAttribute != null)
                    {
                        int bitSize = bitSizeAttribute.Bits;
                        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                        var propertyValue = property.GetValue(viewModel);

                        if (propertyValue == null)
                        {
                            continue; // Skip processing if property value is null
                        }

                        if (propertyType == typeof(int))
                        {
                            int? value = (int?)propertyValue;
                            string binaryString = value.HasValue ? Convert.ToString(value.Value, 2).PadLeft(bitSize, '0') : new string('0', bitSize);
                            modifiedBinary.Append(binaryString);
                        }
                        else if (propertyType == typeof(bool))
                        {
                            bool? value = (bool?)propertyValue;
                            string binaryString = value.HasValue ? (value.Value ? "1" : "0") : "0";
                            modifiedBinary.Append(binaryString.PadLeft(bitSize, '0'));
                        }
                        else if (propertyType == typeof(string))
                        {
                            string value = (string)propertyValue;
                            string binaryString;

                            if (IsHexString(value))
                            {
                                binaryString = ConvertHexToBinary(value).PadLeft(bitSize, '0');
                            }
                            else
                            {
                                binaryString = value != null && value.All(c => c == '0' || c == '1')
                                    ? value.PadLeft(bitSize, '0')
                                    : Convert.ToString(int.Parse(value ?? "0"), 2).PadLeft(bitSize, '0');
                            }

                            modifiedBinary.Append(binaryString);
                        }
                        else if (propertyType.IsEnum)
                        {
                            var value = propertyValue;
                            int intValue = (int)value;
                            string binaryString = Convert.ToString(intValue, 2).PadLeft(bitSize, '0');
                            modifiedBinary.Append(binaryString);
                        }
                        else if (propertyType == typeof(LanguageStrings))
                        {
                            var langStrings = (LanguageStrings)propertyValue;
                            //AppendLanguageStrings(langStrings, modifiedBinary, 1);
                            modifiedBinary.Append(langStrings.oldbits);
                        }
                        else
                        {
                            throw new InvalidCastException($"Unsupported property type: {property.PropertyType}");
                        }
                    }
                    else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    {
                        var nestedViewModel = property.GetValue(viewModel);
                        if (nestedViewModel != null)
                        {
                            // Check if the nested view model has at least one non-null property
                            var nestedProperties = nestedViewModel.GetType().GetProperties();

                            ProcessViewModel(nestedViewModel, modifiedBinary);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Debug.WriteLine($"Skipping property {property.Name} due to exception: {ex.Message}");
                }
            }
        }

        private bool IsHexString(string value)
        {
            return !string.IsNullOrEmpty(value) && value.All(c => "0123456789ABCDEFabcdef".Contains(c));
        }

        private string ConvertHexToBinary(string hex)
        {
            StringBuilder binary = new StringBuilder(hex.Length * 4);
            foreach (char c in hex)
            {
                binary.Append(Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0'));
            }
            return binary.ToString();
        }

        private void AppendLanguageStrings(LanguageStrings langStrings, StringBuilder modifiedBinary, int chars)
        {
            
            AppendString(langStrings.English, modifiedBinary, chars, langStrings);
            AppendString(langStrings.Japanese, modifiedBinary, chars, langStrings);
            AppendString(langStrings.German, modifiedBinary, chars, langStrings);
            AppendString(langStrings.French, modifiedBinary, chars, langStrings);
            AppendString(langStrings.Spanish, modifiedBinary, chars, langStrings);
            AppendString(langStrings.LatinAmericanSpanish, modifiedBinary, chars, langStrings);
            AppendString(langStrings.Italian, modifiedBinary, chars, langStrings);
            AppendString(langStrings.Korean, modifiedBinary, chars, langStrings);
            AppendString(langStrings.ChineseTraditional, modifiedBinary, chars, langStrings);
            AppendString(langStrings.ChineseSimplified, modifiedBinary, chars, langStrings);
            AppendString(langStrings.Portuguese, modifiedBinary, chars, langStrings);
            AppendString(langStrings.Polish, modifiedBinary, chars, langStrings);

            if (langStrings.H2AH4 != null)
            {
                AppendString(langStrings.H2AH4.Russian, modifiedBinary, chars, langStrings);
                AppendString(langStrings.H2AH4.Danish, modifiedBinary, chars, langStrings);
                AppendString(langStrings.H2AH4.Finnish, modifiedBinary, chars, langStrings);
                AppendString(langStrings.H2AH4.Dutch, modifiedBinary, chars, langStrings);
                AppendString(langStrings.H2AH4.Norwegian, modifiedBinary, chars, langStrings);
            }
        }

        //        if (stringPresent > 0)
        //            {

        //                if (teamString && Settings.Default.DecompiledVersion == 0)
        //                {
        //                    ls.m3 = ConvertToInt(GetValue(bits+1));
        //                }
        //                else
        //                {
        //                    //GetValue(1);
        //                    ls.m3 = ConvertToInt(GetValue(bits));
        //}

        //ls.d = ConvertToInt(GetValue(1));
        //if (ls.d == 0)
        //{
        //    compressedChunk = GetValue(ls.m3 * 8);
        //    compression = false;
        //}
        //else
        //{
        //    int m1 = ConvertToInt(GetValue(bits));
        //    string b = ConvertToHex(GetValue(m1 * 8));


        //    byte[] b2 = Convert.FromHexString(b);
        //    var bytes = LowLevelDecompress(b2, ls.m3);
        //    //convert to hex string
        //    compressedChunk = BitConverter.ToString(bytes).Replace("-", "");
        //    //Convert compressedChunk to binary
        //    compressedChunk = ConvertToBinary(compressedChunk);
        //}   
        //            }

        private string ConvertBinaryToHex(string binary)
        {
            // Ensure the binary string length is a multiple of 4
            int remainder = binary.Length % 4;
            if (remainder != 0)
            {
                binary = binary.PadLeft(binary.Length + (4 - remainder), '0');
            }

            // Convert binary to hex
            StringBuilder hex = new StringBuilder(binary.Length / 4);
            for (int i = 0; i < binary.Length; i += 4)
            {
                string fourBits = binary.Substring(i, 4);
                hex.Append(Convert.ToInt32(fourBits, 2).ToString("X"));
            }

            return hex.ToString();
        }


        private string RestoreCompression(LanguageStrings ls, string value)
        {

            //Convert the ascii string to a binary string
            string binary = ConvertToBinary(value);
            //Convert to hex
            string hex = ConvertBinaryToHex(binary);

            return value;
        }

 
        private void AppendString(string value, StringBuilder modifiedBinary, int chars, LanguageStrings ls)
        {
            // Append the number of characters in binary form
            modifiedBinary.Append(Convert.ToString(chars, 2).PadLeft(chars, '0'));
            if (value == "-1")
            {
                // Append a single '0' if the value is "-1"
                modifiedBinary.Append("0");
            }
            else
            {
                // Append a single '1' to indicate the presence of a valid string
                modifiedBinary.Append("1");

                // Convert the string to binary and append it
                modifiedBinary.Append(ConvertToBinary(value));

                //append ls.m3
                modifiedBinary.Append(Convert.ToString(ls.m3, 2));
                modifiedBinary.Append("0");

            }

            
        }

        private string ConvertToBinary(string value)
        {
            // Convert each character in the string to its binary representation
            return string.Join("", value.Select(c => Convert.ToString(c, 2).PadLeft(8, '0')));
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
