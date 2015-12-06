using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace WzPatch
{
    class WzReader
    {
        BinaryReader br;

        public WzReader(string inputFile)
        {
            br = new BinaryReader(File.Open(inputFile, FileMode.Open));

        }

        public int GetVersion()
        {
            br.BaseStream.Seek(0, SeekOrigin.End);
            long eof = br.BaseStream.Position;
            br.BaseStream.Position = 12;
            int versionOffset = br.ReadInt32();
            br.BaseStream.Position = versionOffset;
            Int16 versionhash = br.ReadInt16();
            
            

        }

        protected UInt32 CalculateHashKey(UInt16 version, UInt16 encryptedVersion)
        {
            byte a, b, c, d;
            UInt32 hashKey = 0;

            var versionstr = version.ToString();
            for(int i = 0; i < versionstr.Length; i++)
            {
                hashKey = (hashKey << 5) + (byte)versionstr[i] + 1;
            }
            a = (byte) ((hashKey >> 24) & 0xFF);
            b = (byte) ((hashKey >> 16) & 0xFF);
            c = (byte) ((hashKey >> 8) & 0xFF);
            d = (byte) ((hashKey >> 0) & 0xFF);
            byte checkVersion = (byte) (a ^ b ^ c ^ d ^ 0xFF);
            if(checkVersion == (byte)encryptedVersion)
            {
                return hashKey;
            }   
            else
            {
                return 0;
            }
        }

        protected 

        public static uint RotateLeft(this uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        public static uint RotateRight(this uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }

        protected Int32 rPackedInt()
        {
            Int32 check;
            check = br.ReadByte();
            if(check == 0x80)
            {
                check = br.ReadInt32();
            }
            return check;
        }
    }
}
