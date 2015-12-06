using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WzPatch
{
    class Crc32
    {
        private const UInt32 Polynomial = 0x04C11DB7;
        private UInt32[] table;
        private UInt32 crc;

        public Crc32()
        {
            this.CreateTable();
        }
        /*
        public static bool operator ==(Crc32 x, UInt32 y)
        {
            return x.crc == y;
        }

        public static bool operator == (Crc32 x, Crc32 y)
        {
            return x.crc == y.crc;
        }

        public static bool operator == (UInt32 x, Crc32 y)
        {
            return x == y.crc;
        }
        */
        protected void CreateTable()
        {
            table = new UInt32[256];

            UInt32 remain;
            UInt32 dividend;
            UInt32 bit;

            for (dividend = 0; dividend < table.Length; dividend++)
            {
                remain = dividend << 24;
                for (bit = 0; bit < 8; bit++)
                {
                    if ((remain & 0x80000000) != 0)
                    {
                        remain <<= 1;
                        remain ^= Polynomial;
                    }
                    else
                    {
                        remain <<= 1;
                    }
                }
                table[dividend] = remain;
            }
        }

        public void Update(byte[] buf, long length)
        {
            byte indexLookup;
            long blockPos;

            for (blockPos = 0; blockPos < length; blockPos++)
            {
                indexLookup = (byte)((this.crc >> 0x18) ^ buf[blockPos]);
                this.crc = (this.crc << 0x08) ^ this.table[indexLookup];
            }
        }

        public void Update(BinaryReader buf)
        {
            var b = new byte[16384];
            int readLength;
            do
            {
                readLength = buf.Read(b, 0, b.Length);
                this.Update(b, readLength);
            } while (readLength > 0);
        }

        public UInt32 GetCRC()
        {
            return this.crc;
        }
    }
}
