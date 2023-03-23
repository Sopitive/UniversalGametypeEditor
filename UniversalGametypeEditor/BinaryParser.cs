using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor
{
    using System;
    using System.Diagnostics;
    using System.IO;
using System.Windows.Controls;
using System.Windows.Markup;
    using System.Xml;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

    class BinaryParser
    {

        public static string BinaryToString(string bits) //count forward and convert binary to hex
        {
            int numOfBytes = bits.Length / 8;
            byte[] bytes = new byte[numOfBytes];
            for (int i = 0; i < numOfBytes; ++i)
            {
                bytes[i] = Convert.ToByte(bits.Substring(8 * i, 8), 2);
            }
            return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }

        static string BinaryToHex(string input)
        {
            var hex = string.Join(" ",
            Enumerable.Range(0, input.Length / 8)
            .Select(i => Convert.ToByte(input.Substring(i * 8, 8), 2).ToString("X2")));

            return String.Concat(hex.Where(c => !Char.IsWhiteSpace(c)));
        }

        public static string BinaryToString16(string data)
        {
            //List<Byte> byteList = new List<Byte>();

            //for (int i = 0; i < data.Length; i += 8)
            //{
            //    byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            //}
            //return Encoding.ASCII.GetString(byteList.ToArray());
            var hex = string.Join(" ",
            Enumerable.Range(0, data.Length / 8).Select(i => Convert.ToByte(data.Substring(i * 8, 8), 2).ToString("X2")));
            return String.Concat(hex.Where(c => !Char.IsWhiteSpace(c)));
        }

        public static string FromHexString8(string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return Encoding.UTF8.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
        }
        public static string remainingBits = "";
        private static (string, string) ReadBits(byte[] bytes, int bitPosition, int numBits)
        {
            int byteIndex = bitPosition / 8;  // get the byte index of the starting bit
            int bitOffset = bitPosition % 8;  // get the bit offset within the starting byte
            int result = 0;
            string bitString = "";

            for (int i = 0; i < numBits; i++)
            {
                // read the next bit from the current byte and shift it to the correct position
                int bitValue = (bytes[byteIndex] >> (7 - bitOffset)) & 1;
                result |= (bitValue << (numBits - i - 1));

                // append the bit value to the bit string
                bitString += bitValue.ToString();

                // increment the bit offset and byte index as necessary
                bitOffset++;
                if (bitOffset == 8)
                {
                    bitOffset = 0;
                    byteIndex++;
                }
            }

            // return the parsed integer value and the remaining bit string
            return (Convert.ToString(result,2), bitString.Substring(numBits));
        }


        static string binaryString = "";
        public static void ProcessBin(string thisType, string processNode)
        {
            // Load the XML file that maps out the binary file
            XmlDocument xml = new XmlDocument();
            xml.Load("mpvr.xml");

            // Find the "ExTypes" node in the XML file
            XmlNode exTypesNode = xml.SelectSingleNode($"/base/{thisType}");

            // Open the binary file
            using (var file = System.IO.File.OpenRead("D:\\SteamLibrary\\steamapps\\common\\Halo The Master Chief Collection\\haloreach\\game_variants\\castle_wars.bin"))
            {

                // Set the starting offset to 0x2F0
                file.Position = 0x2F0;

                // Loop through each child node of the "ExTypes" node
                foreach (XmlNode node in exTypesNode.ChildNodes)
                {
                    // Get the name and type of the current variable from the XML file
                    string name = node.Attributes["name"].Value;
                    string type = node.Attributes["type"].Value;

                    

                    // Get the number of bits to read from the binary file
                    int bits = int.Parse(node.Attributes["bits"].Value);

                    // Check if the current node is a container
                    if (type == "Container")
                    {
                        // Loop through each child node of the container
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            // Get the name and type of the current variable from the XML file
                            string childName = childNode.Attributes["name"].Value;
                            string childType = childNode.Attributes["type"].Value;

                            // Get the number of bits to read from the binary file
                            int childBits = int.Parse(childNode.Attributes["bits"].Value);

                            int length = binaryString.Length;

                            byte[] buffer = new byte[2];
                            
                            while (length < 80 || length < childBits)
                            {
                                length = binaryString.Length;
                                file.Read(buffer, 0, buffer.Length);
                                binaryString += string.Concat(buffer.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                            }
                            
                            

                            string fullString = "";
                            bool skipRead = false;

                            // Read the bits from the binary file and convert to the appropriate type
                            if (childBits == 0 && childType.Contains("Enumref") == false)
                            {
                                if (childType == "UString8")
                                {
                                    childBits = 8;
                                }
                                
                                if (childType == "UString16")
                                {
                                    childBits = 16;
                                }
                                string fullText = "";

                                

                                while (true)
                                {


                                    string extracted = binaryString.Substring(0, childBits);
                                    binaryString = binaryString.Substring(childBits);

                                    
                                    //Debug.WriteLine(ReadBits(buffer, 0, childBits));
                                    //Debug.WriteLine(remainingBits);

                                    skipRead = false;
                                    string val = GetValueFromBits(ref extracted, childType, fullString, xml, file, childName);
                                    fullText += val;
                                    byte[] buff = new byte[2];

                                    while (binaryString.Length < 80 || length < childBits)
                                    {
                                        length = binaryString.Length;
                                        file.Read(buff, 0, buff.Length);
                                        binaryString += string.Concat(buff.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                                    }

                                    if (extracted == "00000000" && childBits == 8 || extracted == "0000000000000000" && childBits == 16)
                                    {
                                        Debug.WriteLine("{0}.{1}: {2}", name, childName, fullText);
                                        break;
                                    }


                                }
                                continue;
                            }

                            if (childType == "HCount")
                            {
                                int childChars = int.Parse(childNode.Attributes["chars"].Value);
                                string extracted = binaryString;
                                binaryString = binaryString.Substring(childChars);
                                string val = GetValueFromBits(ref extracted, childType, fullString, xml, file, childName, childChars);
                                Debug.WriteLine("{0}.{1}: {2}", name, childName, val);

                                continue;
                            }

                            string extractedString = binaryString.Substring(0, childBits);
                            binaryString = binaryString.Substring(childBits);
                            //Debug.WriteLine(ReadBits(buffer, 0, childBits));
                            //Debug.WriteLine(remainingBits);

                            string value = GetValueFromBits(ref extractedString, childType, fullString, xml, file, childName);

                            // Print the variable name and value
                            Debug.WriteLine("{0}.{1}: {2}", name, childName, value);
                        }
                    }
                }
            }
        }


        static string GetValueFromBits(ref string bits, string type, string fullString, XmlNode xml, FileStream file, string nodeName, int chars = 5, int bitCount = 1)
        {

            switch (type)
            {
                case "String":
                    return BinaryToString(bits);
                    //int numOfBytes = bits.Length / 8;
                    //byte[] bytes = new byte[numOfBytes];
                    //for (int i = 0; i < numOfBytes; ++i)
                    //{
                    //    bytes[i] = Convert.ToByte(bits.Substring(8 * i, 8), 2);
                    //}
                    //return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                case "Int":
                    byte[] len = new byte[32];
                    //buffer.CopyTo(len, 0);
                    return Convert.ToInt32(bits, 2).ToString();
                //string binaryString = "";
                //for (int i = 0; i < buffer.Length; i++)
                //{
                //    binaryString = Convert.ToString(buffer[i], 2).PadLeft(8, '0');
                //}
                //return Convert.ToInt32(binaryString, 2);
                case "UInt":
                    return Convert.ToInt32(bits, 2).ToString();
                case "Hex":
                    return BinaryToHex(bits);
                case "UString8":
                    //string hex = BinaryToString16(fullString);
                    string text = BinaryToString16(bits);
                    int decNum = Convert.ToInt32(text, 16);
                    char charNum = Convert.ToChar(decNum);
                    string final = charNum.ToString();
                    return final;
                case "UString16":
                    string tex = BinaryToString16(bits);
                    int dec = Convert.ToInt32(tex, 16);
                    char ch = Convert.ToChar(dec);
                    string fin = ch.ToString();
                    return fin;

                case "Enum":
                    string i1 = bits.Substring(0, bitCount);

                    return i1;

                case "HCount":
                    List<LanguageStrings> string_indexes = new();
                    string newString = "";
                    newString = bits.Substring(0, bitCount);

                    int converted = IncrementPosSmall(ref newString, newString.Length, ref bits, true, false);

                    //newString = bits.Substring(bitCount);
                    bits = bits.Substring(converted);

                    int x = 0;
                    for (int i = 0; i < converted; i += 1)
                    {
                        
                        LanguageStrings z = new();
                        //x = IncrementPos(x, ref newString, chars, ref bits, file);
                        //newString = newString.Substring(1);
                        z.EnglishStringIndex = (Convert.ToInt32(newString.Substring(0,1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.JapaneseStringIndex = (Convert.ToInt32(newString.Substring(0,1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.GermanStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.FrenchStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.SpanishStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.MexicanStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.ItalianStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.KoreanStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.Chinese1StringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.Chinese2StringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.PortugeseStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        z.PolishStringIndex = (Convert.ToInt32(newString.Substring(0, 1), 2) == 0) ? -1 : x = IncrementPos(x, ref newString, chars, ref bits, file);
                        string_indexes.Add(z);
                    }
                    string compressed = "";
                    bool compressionState = true;
                    

                    if (converted > 0)
                    {
                        int m3 = 0;
                        m3 = IncrementPos(x, ref newString, chars, ref bits, file, 32767);


                        int d = IncrementPosSmall(ref newString, 1, ref bits, true);

                        if (d == 0)
                        {
                            string substring = newString.Substring(0, m3 * 8);
                            compressed = BinaryToHex(substring);
                            IncrementPosSmall(ref newString, m3 * 8, ref bits, false);
                            compressionState = false;
                        }
                        else
                        {
                            int m1 = IncrementPos(x, ref newString, chars, ref bits, file, 5000);
                            string b = BinaryToHex(newString.Substring(0, m1*8));
                            IncrementPosSmall(ref newString, m1 * 8, ref bits, false);
                        }
                    }

                    binaryString = newString;
                    return FromHexString8(compressed);



                //return finalString;
                default:
                    if (type.Contains("Enumref"))
                    {
                        
                        string[] w = type.Split(":");
                        string parentType = nodeName;

                        XmlDocument mpvr = new XmlDocument();
                        mpvr.Load("mpvr.xml");
                        XmlNode refTypesNode = xml.SelectSingleNode($"/base/RefTypes");

                        foreach (XmlNode node in refTypesNode.ChildNodes)
                        {
                            // Get the name and type of the current variable from the XML file
                            string name = node.Attributes["name"].Value;
                            string reftype = node.Attributes["type"].Value;

                            // Get the number of bits to read from the binary file
                            int refbits = int.Parse(node.Attributes["bits"].Value);

                            if (parentType == "BluePlayerTraits")
                            {

                            }
                            
                            // Check if the current node is a container
                            if (reftype == "Container" && name == w[1])
                            {
                                // Loop through each child node of the container
                                foreach (XmlNode childNode in node.ChildNodes)
                                {
                                    // Get the name and type of the current variable from the XML file
                                    string childName = childNode.Attributes["name"].Value;
                                    string childType = childNode.Attributes["type"].Value;

                                    if (name == "LoadoutOptions")
                                    {

                                    }

                                    // Get the number of bits to read from the binary file
                                    int childBits = int.Parse(childNode.Attributes["bits"].Value);

                                    int length = binaryString.Length;

                                    byte[] buffer = new byte[2];

                                    while (length < 80 || length < childBits)
                                    {
                                        length = binaryString.Length;
                                        file.Read(buffer, 0, buffer.Length);
                                        binaryString += string.Concat(buffer.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                                    }

                                    

                                    string reffullString = "";
                                    bool skipRead = false;

                                    // Read the bits from the binary file and convert to the appropriate type
                                    if (childBits == 0)
                                    {
                                        if (childType == "UString8")
                                        {
                                            childBits = 8;
                                        }

                                        if (childType == "UString16")
                                        {
                                            childBits = 16;
                                        }
                                        string fullText = "";



                                        while (true)
                                        {


                                            string extracted = binaryString.Substring(0, childBits);
                                            binaryString = binaryString.Substring(childBits);


                                            //Debug.WriteLine(ReadBits(buffer, 0, childBits));
                                            //Debug.WriteLine(remainingBits);

                                            skipRead = false;
                                            string val = GetValueFromBits(ref extracted, childType, reffullString, xml, file, childName);
                                            fullText += val;
                                            byte[] buff = new byte[2];

                                            while (binaryString.Length < 80 || length < childBits)
                                            {
                                                length = binaryString.Length;
                                                file.Read(buff, 0, buff.Length);
                                                binaryString += string.Concat(buff.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                                            }

                                            if (extracted == "00000000" && childBits == 8 || extracted == "0000000000000000" && childBits == 16)
                                            {
                                                Debug.WriteLine("{0}.{1}.{2}: {3}", parentType, name, childName, fullText);
                                                break;
                                            }


                                        }
                                        continue;
                                    }

                                    if (childType == "HCount")
                                    {
                                        int childChars = int.Parse(childNode.Attributes["chars"].Value);
                                        string extracted = binaryString;
                                        //binaryString = binaryString.Substring(childChars);
                                        string val = GetValueFromBits(ref extracted, childType, fullString, xml, file, childName, childChars, childBits);
                                        Debug.WriteLine("{0}.{1}: {2}", name, childName, val);

                                        continue;
                                    }

                                    string extractedString = binaryString.Substring(0, childBits);
                                    binaryString = binaryString.Substring(childBits);
                                    //Debug.WriteLine(ReadBits(buffer, 0, childBits));
                                    //Debug.WriteLine(remainingBits);

                                    skipRead = false;
                                    string value = GetValueFromBits(ref extractedString, childType, reffullString, xml, file, childName);

                                    // Print the variable name and value
                                    Debug.WriteLine("{0}.{1}.{2}: {3}", parentType, name, childName, value);
                                }
                            }
                        }

                    }
                    return null;
            }

        }

        static int IncrementPosSmall(ref string newString, int increment, ref string data, bool getResult, bool inc = true)
        {
            int result = 1;
            if (getResult)
            {
                result = Convert.ToInt32(newString.Substring(0, increment));
            }
            newString = data.Substring(increment);
            if (inc)
            {
                data = data.Substring(increment);
            }
            

            return result;
        }

        static int IncrementPos(int x, ref string newString, int increment, ref string data, FileStream file, int customSize = 80)
        {
            
            int result = Convert.ToInt32(newString.Substring(1, increment),2);
            newString = data.Substring(increment+1);
            data = data.Substring(increment+1);
            //data = data.Substring(increment+1);
            byte[] buff = new byte[2];
            //data = newString;
            //file.Read(buff, 0, buff.Length);

            while (data.Length < 80 || data.Length < customSize)
            {
                file.Read(buff, 0, buff.Length);
                data += string.Concat(buff.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                newString = data;
            }
            return result;
        }

        

    }
    public struct LanguageStrings
    {
        public int EnglishStringIndex;
        public int JapaneseStringIndex;
        public int GermanStringIndex;
        public int FrenchStringIndex;
        public int SpanishStringIndex;
        public int MexicanStringIndex;
        public int ItalianStringIndex;
        public int KoreanStringIndex;
        public int Chinese1StringIndex;
        public int Chinese2StringIndex;
        public int PortugeseStringIndex;
        public int PolishStringIndex;
        // H4 & 2A
        public int RussianStringIndex;
        public int DanishStringIndex;
        public int FinnishStringIndex;
        public int DutchStringIndex;
        public int NorwegianStringIndex;
    }

}
