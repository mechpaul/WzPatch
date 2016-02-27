using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace WzPatch
{
    public class WzBinaryReader
    {
        int versionOffset = 0;
        ushort encryptedVersion = 0;
        BinaryReader br;

        public WzBinaryReader(Stream BaseStream)
        {
            br = new BinaryReader(BaseStream);
            ParseHeader();
        }

        protected void ParseHeader()
        {
            br.BaseStream.Seek(12, SeekOrigin.Begin);
            versionOffset = br.ReadInt32();
            br.BaseStream.Seek(versionOffset, SeekOrigin.Begin);
            encryptedVersion = br.ReadUInt16();
        }

        protected Int32 ReadPackedInt()
        {
            Int32 check;
            check = br.ReadByte();
            if (check == 0x80)
            {
                check = br.ReadInt32();
            }
            return check;
        }

        public int GetVersion()
        {
            br.BaseStream.Seek(versionOffset + 2, SeekOrigin.Begin);
            var numFiles = ReadPackedInt();
            var containerType = br.ReadByte();
            var lengthString = byte.MaxValue - br.ReadByte() + 1;
            br.BaseStream.Seek(lengthString, SeekOrigin.Current);
            var length = ReadPackedInt();
            var checksum = ReadPackedInt();
            var encryptedOffsetOffset = br.BaseStream.Position;
            var encryptedOffset = br.ReadUInt32();

            for(int i = 0; i < 9999; i++)
            {
                var hashKey = CalculateHashKey(i, encryptedVersion);
                if(hashKey == 0)
                {
                    continue;
                }
                var offset = CalculateOffset(encryptedOffset, hashKey, encryptedOffsetOffset);
                if(offset < br.BaseStream.Length)
                {
                    br.BaseStream.Seek(offset, SeekOrigin.Begin);
                    if(containerType == 0x03)
                    {
                        ReadPackedInt(); //number of directories
                    }
                    var b = br.ReadByte();
                    if (b == 0x73 || b == 0x04)
                    {
                        return i;
                    }
                }

            }
            return 0;
        }

        protected uint CalculateOffset(uint encryptedOffset, uint hashKey, long encryptedOffsetOffset)
        {
            uint offset = ((uint)encryptedOffsetOffset - (uint)versionOffset) ^ uint.MaxValue;
            offset *= hashKey;
            offset -= 0x581c3f6d;
            offset = RotateLeft(offset, (byte)(offset & 0x1F));
            offset ^= encryptedOffset;
            offset += ((uint)versionOffset * 2);
            return offset;
        }

        protected uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        protected uint CalculateHashKey(int version, ushort encryptedVersion)
        {
            byte a, b, c, d;
            uint hashKey = 0;

            var versionstr = version.ToString();
            for (int i = 0; i < versionstr.Length; i++)
            {
                hashKey = (hashKey << 5) + (byte)versionstr[i] + 1;
            }
            a = (byte)((hashKey >> 24) & 0xFF);
            b = (byte)((hashKey >> 16) & 0xFF);
            c = (byte)((hashKey >> 8) & 0xFF);
            d = (byte)((hashKey >> 0) & 0xFF);
            byte checkVersion = (byte)(a ^ b ^ c ^ d ^ 0xFF);
            if (checkVersion == encryptedVersion)
            {
                return hashKey;
            }
            else
            {
                return 0;
            }
        }
    }
}
