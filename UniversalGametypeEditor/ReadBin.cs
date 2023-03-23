using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor
{
    public static class ReadBin
    {

        public static string ReadBitsFromFile(int numBitsToRead, long fileOffset, BinaryReader binaryReader, ref string remainingBits)
        {
            string bits = "";
            int bitsRead = 0;

            // Check if there are remaining bits from the previous read
            if (!string.IsNullOrEmpty(remainingBits))
            {
                bits = remainingBits;
                bitsRead = bits.Length;
                remainingBits = "";
            }

            // Calculate the number of bytes needed to read the required bits
            int numBytesToRead = (int)Math.Ceiling((double)(numBitsToRead - bitsRead) / 8);

            // Read the bytes from the file
            byte[] bytes = binaryReader.ReadBytes(numBytesToRead);

            // Append the bits from the bytes to the string
            for (int i = 0; i < bytes.Length; i++)
            {
                bits += Convert.ToString(bytes[i], 2).PadLeft(8, '0');
            }

            // Get the bits to return
            string bitsToReturn = bits.Substring(0, numBitsToRead);

            // Get the remaining bits for the next read
            remainingBits = bits.Substring(numBitsToRead);

            return bitsToReturn;
        }


        public static string GetGameTypeFromBits(string bits)
        {
            int gameTypeValue = Convert.ToInt32(bits, 2);

            switch (gameTypeValue)
            {
                case 0:
                    return "Capture the Flag";
                case 1:
                    return "Slayer";
                case 2:
                    return "Oddball";
                case 3:
                    return "King of the Hill";
                case 4:
                    return "Juggernaut";
                case 5:
                    return "Territories";
                case 6:
                    return "Assault";
                case 7:
                    return "Infection";
                case 8:
                    return "Blank";
                case 9:
                    return "Invasion";
                case 10:
                    return "Stockpile";
                case 11:
                    return "Unknown1";
                case 12:
                    return "Race";
                case 13:
                    return "Headhunter";
                case 14:
                    return "Unknown2";
                case 15:
                    return "Unknown3";
                case 16:
                    return "Action Sack";
                case 30:
                    return "Pregame";
                default:
                    return "None";
            }
        }



        public static void ReadFile(string file)
        {
            using (BinaryReader binaryReader = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                // Set the file offset to 842
                binaryReader.BaseStream.Seek(842, SeekOrigin.Begin);

                // Call the function to read the first 4 bits
                string remainingBits = "";
                string bits = ReadBitsFromFile(4, 842, binaryReader, ref remainingBits);

                // Display the bits that were read
                Debug.WriteLine(bits);
                bits = ReadBitsFromFile(5, 842, binaryReader, ref remainingBits);
                int num = Convert.ToInt32(bits, 2);
                Debug.WriteLine(bits);
                Debug.WriteLine(num);
                Debug.WriteLine(GetGameTypeFromBits(bits));
            }





            //FileStream fs = new FileStream(file, FileMode.Open);
            //fs.Seek(842, SeekOrigin.Begin); // Seek to 752 bytes from the beginning of the file or 2F0 to get to mpvr
            //byte[] buffer = new byte[fs.Length - fs.Position];
            //fs.Read(buffer, 0, buffer.Length); // Read all bytes after the offset into the buffer

            ////The very first portion of data in the gametype is the gametype category.

            //for (int i=0; i<buffer.Length - 1; i++)
            //{
            //    string combinedString = string.Concat(Convert.ToString(buffer[i], 2).PadLeft(8, '0'), Convert.ToString(buffer[i+1], 2).PadLeft(8, '0'));

            //    Debug.WriteLine(combinedString);

            //    int startIndex = 7; // The starting index of the 4-bit substring
            //    int substringLength = 4; // The length of the 4-bit substring

            //    // Extract the 4-bit substring starting from the 8th position
            //    string bitSubstring = combinedString.Substring(startIndex, substringLength);

            //    // Convert the 4-bit substring to an integer value using a base of 2
            //    int value = Convert.ToInt32(bitSubstring, 2);
            //    Debug.WriteLine(value);
            //    return;
            //}

            //string combinedString = String.Concat(Convert.ToString(buffer[148], 2).PadLeft(8, '0'), Convert.ToString(buffer[149], 2).PadLeft(8, '0'));


            //Debug.WriteLine(Convert.ToString(buffer[150], 2).PadLeft(8, '0'));
            //Debug.WriteLine(Convert.ToString(buffer[151], 2).PadLeft(8, '0'));
        }

    }
}
