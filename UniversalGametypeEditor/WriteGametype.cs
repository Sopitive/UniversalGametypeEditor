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

        public void WriteBinaryFile(
    string filename,
    GametypeHeader gt,
    FileHeaderViewModel fh,
    GametypeHeaderViewModel gh,
    ModeSettingsViewModel ms,
    SpawnSettingsViewModel ss,
    GameSettingsViewModel gs,
    PowerupTraitsViewModel ps,
    TeamSettingsViewModel ts,
    LoadoutClusterViewModel lc)
        {
            byte[] bytes = File.ReadAllBytes(filename);

            // Apply convert-to-forge BEFORE writing anything
            if (Settings.Default.ConvertToForge)
            {
                fh.VariantType = VariantTypeEnum.Forge;
            }

            // Build bitstreams
            string prebits;
            if (Settings.Default.IsGvar)
            {
                // ✅ gvar data starts at 0x80 (128)
                const int GVAR_BASE = 0x80;

                if (bytes.Length < GVAR_BASE)
                    throw new InvalidOperationException($"WriteBinaryFile: file too small for gvar base 0x{GVAR_BASE:X}.");

                prebits = string.Join("", bytes.Take(GVAR_BASE).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                rawBinary = string.Join("", bytes.Skip(GVAR_BASE).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                modifiedBinary = "";

                // ✅ gvar: ONLY write the header fields (do not write mpvr sections)
                WriteFileHeaders(fh);

                // splice: replace header prefix bits inside the gvar region
                int slice = modifiedBinary.Length;
                string tail = rawBinary.Length >= slice ? rawBinary.Substring(slice) : "";

                rawBinary = modifiedBinary + tail;
                rawBinary = prebits + rawBinary;

                byte[] newBytes = Enumerable.Range(0, rawBinary.Length / 8)
                    .Select(i => Convert.ToByte(rawBinary.Substring(i * 8, 8), 2))
                    .ToArray();

                File.WriteAllBytes(filename, newBytes);
                System.Threading.Thread.Sleep(1);
                return;
            }
            else
            {
                // mpvr path: your existing behavior (starts at 0x2F0 / 752)
                prebits = string.Join("", bytes.Take(752).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                rawBinary = string.Join("", bytes.Skip(752).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            }

            modifiedBinary = "";

            WriteFileHeaders(fh);

            WriteGametypeHeaders(gh, gt);
            modifiedBinary += WriteModeSettings(ms);

            if (Settings.Default.DecompiledVersion == 0)
            {
                modifiedBinary += WriteSpawnSettings(ss);
                modifiedBinary += WriteGameSettings(gs);
                // optional sections...
            }

            int sliceMpvr = modifiedBinary.Length;
            rawBinary = rawBinary.Length >= sliceMpvr ? rawBinary.Substring(sliceMpvr) : "";
            rawBinary = modifiedBinary + rawBinary;
            rawBinary = prebits + rawBinary;

            byte[] newBytesMpvr = Enumerable.Range(0, rawBinary.Length / 8)
                .Select(i => Convert.ToByte(rawBinary.Substring(i * 8, 8), 2))
                .ToArray();

            File.WriteAllBytes(filename, newBytesMpvr);
            System.Threading.Thread.Sleep(1);
        }






        public void WriteFileHeaders(FileHeaderViewModel fh)
        {
            if (fh == null)
                return;

            static string SafeSlice(string bits, int start, int len)
            {
                if (len <= 0) return string.Empty;
                if (string.IsNullOrEmpty(bits)) return new string('0', len);
                if (start < 0) start = 0;
                if (start >= bits.Length) return new string('0', len);

                int avail = bits.Length - start;
                if (avail >= len) return bits.Substring(start, len);
                return bits.Substring(start, avail) + new string('0', len - avail);
            }

            static bool IsBinaryString(string s) => !string.IsNullOrEmpty(s) && s.All(c => c == '0' || c == '1');

            string? TryEncode(object? valueObj, int bitSize)
            {
                if (valueObj == null) return null;

                try
                {
                    if (valueObj is string s)
                    {
                        if (IsBinaryString(s)) return s.PadLeft(bitSize, '0');
                        if (int.TryParse(s, out int nStr)) return Convert.ToString(nStr, 2).PadLeft(bitSize, '0');
                        return null;
                    }

                    var t = Nullable.GetUnderlyingType(valueObj.GetType()) ?? valueObj.GetType();

                    if (t.IsEnum)
                    {
                        // ✅ FIX: this works for boxed enums
                        int nEnum = Convert.ToInt32(valueObj);
                        return Convert.ToString(nEnum, 2).PadLeft(bitSize, '0');
                    }

                    if (valueObj is IConvertible)
                    {
                        int n = Convert.ToInt32(valueObj);
                        return Convert.ToString(n, 2).PadLeft(bitSize, '0');
                    }

                    return null;
                }
                catch
                {
                    return null;
                }
            }

            // Explicit binary order
            var orderedNames = Settings.Default.IsGvar
                ? new[]
                {
            nameof(FileHeaderViewModel.Mpvr),         // 32 ("gvar")
            nameof(FileHeaderViewModel.MegaloVersion),// 32 (gvar pad/skip field from ReadBinary: GetValue(32))
            nameof(FileHeaderViewModel.Unknown0x2F8),  // 16
            nameof(FileHeaderViewModel.Unknown0x2FA),  // 16
            nameof(FileHeaderViewModel.Unknown0x318),  // 2
            nameof(FileHeaderViewModel.VariantType),   // 2
            nameof(FileHeaderViewModel.Unknown0x319),  // 4  => packed byte @ offset 0x0C (12)
            nameof(FileHeaderViewModel.Unknown0x31D),  // 32
            nameof(FileHeaderViewModel.Unknown0x31C),  // 32
            nameof(FileHeaderViewModel.FileLength),    // 32
                }
                : new[]
                {
            nameof(FileHeaderViewModel.Mpvr),
            nameof(FileHeaderViewModel.MegaloVersion),
            nameof(FileHeaderViewModel.Unknown0x2F8),
            nameof(FileHeaderViewModel.Unknown0x2FA),
            nameof(FileHeaderViewModel.UnknownHash0x2FC),
            nameof(FileHeaderViewModel.Blank0x310),
            nameof(FileHeaderViewModel.FileUsedSize),
            nameof(FileHeaderViewModel.Unknown0x318),
            nameof(FileHeaderViewModel.VariantType),
            nameof(FileHeaderViewModel.Unknown0x319),
            nameof(FileHeaderViewModel.Unknown0x31D),
            nameof(FileHeaderViewModel.Unknown0x31C),
            nameof(FileHeaderViewModel.FileLength),
                };

            var tFh = typeof(FileHeaderViewModel);
            var props = orderedNames
                .Select(n => tFh.GetProperty(n))
                .Where(p => p != null)
                .ToList();

            var sb = new StringBuilder();
            int bitCursor = 0;

            foreach (var prop in props)
            {
                var bitSizeAttr = (BitSizeAttribute)Attribute.GetCustomAttribute(prop, typeof(BitSizeAttribute));
                int bitSize = bitSizeAttr?.Bits ?? 0;

                // ✅ gvar: ALWAYS preserve the 32-bit skipped pad field (the one you GetValue(32) and never store)
                if (Settings.Default.IsGvar && prop.Name == nameof(FileHeaderViewModel.MegaloVersion))
                {
                    sb.Append(SafeSlice(rawBinary, bitCursor, bitSize));
                    bitCursor += bitSize;
                    continue;
                }

                object? valueObj = null;
                try { valueObj = prop.GetValue(fh); } catch { }

                var encoded = TryEncode(valueObj, bitSize);

                if (encoded == null)
                    sb.Append(SafeSlice(rawBinary, bitCursor, bitSize)); // preserve original bits for this field
                else
                    sb.Append(encoded);

                bitCursor += bitSize;
            }

            // FileHeader is first section, so overwrite prefix
            modifiedBinary = sb.ToString();
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
