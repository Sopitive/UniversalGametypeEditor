using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGametypeEditor
{
    public class WriteGametype
    {
        private string rawBinary = "";
        private byte[] newBytes = new byte[20536];
        private string newBinary = "";
        public void WriteBinaryFile(string filename)
        {
            //Write the bytes for the file, starting at file offset 2F0
            byte[] bytes = System.IO.File.ReadAllBytes(filename).Skip(752).ToArray();

            //Convert to a string of 0s and 1s
            rawBinary = string.Join("", bytes.Select(n => Convert.ToString(n, 2).PadLeft(8, '0')));
            Debug.WriteLine(rawBinary);
            
        }
        
        private void WriteFileHeaders()
        {
            //mpvr = 32 bits
            newBinary += "01101101011100000111011001110010";
            //megaloversion = 32 bits
            newBinary += "00000000000000000101000000101000";
            //Unknown0x2F8 = 16 bits
            newBinary += "0000000000110110";
            //Unknown0x2FA = 16 bits
            newBinary += "0000000000000001";
            //UnknownHash0x2FC = 160 bits
            newBinary += "1011010110011000110101110001000101101101100100011100000000110000110110100101011111110000010100111110011001110101010010101010111111001011011000101100010101101011";
            //Blank0x310 = 32 bits
            newBinary += "0000000000000000";
            //Fileusedsize = 32 bits
            newBinary += "0000000000000000";
            //Unknown0x318 = 8 bits
            newBinary += "01011110";
            //Unknown0x319 = 32 bits
            newBinary += "0010110000000000";
            //Unknown0x31D = 32 bits
            newBinary += "0000000000100000";
            //FileLength = 32 bits

        }

    }
}
