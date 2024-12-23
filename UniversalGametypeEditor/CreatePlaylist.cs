using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace UniversalGametypeEditor
{
    public static class CreatePlaylist
    {
        private static List<string> mapVariantTitles = new List<string>();
        private static List<string> mapVariantHashes = new List<string>();
        private static List<string> gametypeTitles = new List<string>();
        private static List<string> gametypeHashes = new List<string>();
        private static List<DateTime> gametypeModificationDates = new List<DateTime>();

        public static (List<string> mapVariantTitles, List<string> mapVariantHashes, List<string> gametypeTitles, List<string> gametypeHashes, List<string> mapFolderNames, List<string> gametypeFolderNames) GetUUID()
        {
            // Clear the lists before processing
            mapVariantTitles.Clear();
            mapVariantHashes.Clear();
            gametypeTitles.Clear();
            gametypeHashes.Clear();
            gametypeModificationDates.Clear();
            List<string> mapFolderNames = new List<string>();
            List<string> gametypeFolderNames = new List<string>();

            // Existing functionality
            string baseDirectoryPath = @"C:\Program Files (x86)\Steam\steamapps\common\Halo The Master Chief Collection\haloreach";
            ProcessDirectory(Path.Combine(baseDirectoryPath, "game_variants"), "haloreach\\game_variants\\", alwaysGenerateHash: true, mapFolderNames, gametypeFolderNames);
            ProcessDirectory(Path.Combine(baseDirectoryPath, "map_variants"), "haloreach\\map_variants\\", alwaysGenerateHash: true, mapFolderNames, gametypeFolderNames);

            // New functionality to process LocalFiles directories
            string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\MCC\LocalFiles");
            var directories = Directory.GetDirectories(localLowPath);

            foreach (var directory in directories)
            {
                ProcessDirectory(Path.Combine(directory, "HaloReach\\Map"), "HaloReach\\Map\\", alwaysGenerateHash: false, mapFolderNames, gametypeFolderNames);
                ProcessDirectory(Path.Combine(directory, "HaloReach\\GameType"), "HaloReach\\GameType\\", alwaysGenerateHash: false, mapFolderNames, gametypeFolderNames);
            }

            // Sort the gametype lists by modification dates
            var sortedGametypeData = gametypeTitles
                .Select((title, index) => new { Title = title, Hash = gametypeHashes[index], FolderName = gametypeFolderNames[index], ModificationDate = gametypeModificationDates[index] })
                .OrderByDescending(data => data.ModificationDate)
                .ToList();

            gametypeTitles = sortedGametypeData.Select(data => data.Title).ToList();
            gametypeHashes = sortedGametypeData.Select(data => data.Hash).ToList();
            gametypeFolderNames = sortedGametypeData.Select(data => data.FolderName).ToList();

            // Output the arrays for verification
            Debug.WriteLine("Map Variant Titles: " + string.Join(", ", mapVariantTitles));
            Debug.WriteLine("Map Variant Hashes: " + string.Join(", ", mapVariantHashes));
            Debug.WriteLine("Gametype Titles: " + string.Join(", ", gametypeTitles));
            Debug.WriteLine("Gametype Hashes: " + string.Join(", ", gametypeHashes));
            Debug.WriteLine("Map Folder Names: " + string.Join(", ", mapFolderNames));
            Debug.WriteLine("Gametype Folder Names: " + string.Join(", ", gametypeFolderNames));

            return (mapVariantTitles, mapVariantHashes, gametypeTitles, gametypeHashes, mapFolderNames, gametypeFolderNames);
        }

        static string ReadUnicodeTextAtOffset(string filePath, long offset)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                using (BinaryReader reader = new BinaryReader(fs, Encoding.Unicode))
                {
                    StringBuilder result = new StringBuilder();
                    int zeroByteCount = 0;

                    while (fs.Position < fs.Length)
                    {
                        byte b1 = reader.ReadByte();
                        byte b2 = reader.ReadByte();

                        if (b1 == 0x00 && b2 == 0x00)
                        {
                            zeroByteCount++;
                            if (zeroByteCount == 3)
                            {
                                break;
                            }
                        }
                        else
                        {
                            zeroByteCount = 0;
                            result.Append(Encoding.Unicode.GetString(new byte[] { b1, b2 }));
                        }
                    }

                    return result.ToString();
                }
            }
        }

        private static void ProcessDirectory(string directoryPath, string fullPathPrefix, bool alwaysGenerateHash, List<string> mapFolderNames, List<string> gametypeFolderNames)
        {
            if (Directory.Exists(directoryPath))
            {
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    string fileName = Path.GetFileName(file);
                    string extension = Path.GetExtension(file).ToLower();

                    // Only process .bin and .mvar files
                    if (extension != ".bin" && extension != ".mvar")
                    {
                        continue;
                    }

                    string hash;

                    if (alwaysGenerateHash || !IsValidHash(fileName))
                    {
                        string inputString = file.Replace(directoryPath + "\\", "");
                        string fullPathString = fullPathPrefix + inputString;
                        string reorderedHash = GenerateHash(fullPathString);
                        string formattedHash = FormatHash(reorderedHash);
                        hash = InvertUuidBlocks(formattedHash);
                    }
                    else
                    {
                        hash = Path.GetFileNameWithoutExtension(fileName);
                    }

                    string unicodeText = ReadUnicodeTextAtOffset(file, 0xC0);
                    string folderName = Path.GetFileName(Path.GetDirectoryName(file));
                    DateTime modificationDate = File.GetLastWriteTime(file);

                    if (fullPathPrefix.Contains("map_variants") || fullPathPrefix.Contains("Map"))
                    {
                        mapVariantTitles.Add(unicodeText);
                        mapVariantHashes.Add(hash);
                        mapFolderNames.Add(folderName);
                    }
                    else if (fullPathPrefix.Contains("game_variants") || fullPathPrefix.Contains("GameType"))
                    {
                        gametypeTitles.Add(unicodeText);
                        gametypeHashes.Add(hash);
                        gametypeFolderNames.Add(folderName);
                        gametypeModificationDates.Add(modificationDate);
                    }

                    Debug.WriteLine($"File: {fileName}, Unicode Text: {unicodeText}, Hash: {hash}, Folder: {folderName}, Modification Date: {modificationDate}");
                }
            }
        }

        private static bool IsValidHash(string fileName)
        {
            // Assuming a valid hash is in the format 8-4-4-4-12 hexadecimal characters followed by a file extension
            return System.Text.RegularExpressions.Regex.IsMatch(fileName, @"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\.\w+$");
        }

        static string GenerateHash(string inputString)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                byte[][] chunks = Enumerable.Range(0, hashBytes.Length / 4)
                                            .Select(i => hashBytes.Skip(i * 4).Take(4).ToArray())
                                            .ToArray();

                byte[] reorderedChunks = chunks[0].Reverse().Concat(chunks[1].Reverse()).Concat(chunks[2]).Concat(chunks[3]).ToArray();
                return BitConverter.ToString(reorderedChunks).Replace("-", "").ToLower();
            }
        }

        static string FormatHash(string hexString)
        {
            return $"{hexString.Substring(0, 8)}-{hexString.Substring(8, 4)}-{hexString.Substring(12, 4)}-{hexString.Substring(16, 4)}-{hexString.Substring(20)}";
        }

        static string InvertUuidBlocks(string uuidStr)
        {
            string[] blocks = uuidStr.Split('-');
            if (blocks.Length != 5)
            {
                throw new ArgumentException("Invalid UUID format");
            }

            // Swap the second and third blocks
            string temp = blocks[1];
            blocks[1] = blocks[2];
            blocks[2] = temp;

            return string.Join("-", blocks);
        }
    }
}
