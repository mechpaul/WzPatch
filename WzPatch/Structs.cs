using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WzPatch
{
    public enum EFileBlockMode
    {
        CreateFile,
        CreateDirectory,
        Rebuild,
        DeleteFile,
        DeleteDirectory,
        End
    }

    public enum EFileBlockRebuild
    {
        Repeat,
        Write,
        Copy,
        End
    }

    public struct DumpBlock
    {
	    public string filePath;
        public UInt32 length;
        public EFileBlockMode fileBlockMode;
        public Crc32 crc;
        public Crc32 crcOld;
        public byte[] dumpFile;

        public static implicit operator DumpBlock(string s)
        {
            return new DumpBlock { crc = new Crc32(), crcOld = new Crc32()};
        }
    }

    public struct RebuildBlock
    {
        public EFileBlockRebuild fileBlockRebuild;
        public UInt32 length;
        public byte repeatedByte;
        public byte[] raw;
        public UInt32 oldFileOffset;
    }
}
