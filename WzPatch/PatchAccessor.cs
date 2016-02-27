using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace WzPatch
{
    public class PatchBinaryReader : BinaryReader
    {
        protected int MagicBytesOffset = 0;
        protected string MagicBytesContents = "WzPatch\x1A";

        protected int VersionOffset = 8;
        protected int VersionContents = 2;

        protected int CRCOffset = 12;

        protected int ZlibOffset = 16;
        protected int DeflateOffset = 18;

        public PatchBinaryReader(Stream baseStream) : base(baseStream) { }

        public string GetMagicBytes()
        {
            var s = new byte[MagicBytesContents.Length];
            BaseStream.Position = MagicBytesOffset;
            Read(s, 0, s.Length);
            string magicBytes = Encoding.UTF8.GetString(s);
            return magicBytes;
        }

        public bool ValidateMagicBytes()
        {
            return GetMagicBytes() == MagicBytesContents;
        }

        public UInt32 GetCRC()
        {
            BaseStream.Position = CRCOffset;
            return ReadUInt32();
        }

        public Crc32 CalculateCRC()
        {
            var crc = new Crc32();
            BaseStream.Position = ZlibOffset;
            crc.Update(this);
            return crc;
        }

        public bool ValidateCRC()
        {
            var patchBinCRC = new Crc32(GetCRC());
            return CalculateCRC() == patchBinCRC;
        }

        public UInt32 GetVersion()
        {
            BaseStream.Position = VersionOffset;
            return ReadUInt32();
        }

        public bool ValidateVersion()
        {
            return GetVersion() == VersionContents;
        }

        public DeflateStream GetDeflateStream()
        {
            BaseStream.Position = DeflateOffset;
            return new DeflateStream(BaseStream, CompressionMode.Decompress);
        }

    }

    public class PatchAccessor
    {
        protected PatchBinaryReader patch;
        protected string path;
        protected Stream pbrstream;

        public PatchAccessor(string filePath)
        {
            path = filePath;
            pbrstream = File.Open(GetPath(), FileMode.Open);
            patch = new PatchBinaryReader(pbrstream);
        }

        public PatchAccessor(Stream s)
        {
            path = "";
            pbrstream = s;
            patch = new PatchBinaryReader(pbrstream);

        }

        public Stream GetStream()
        {
            return pbrstream;
        }

        public bool ValidatePatch()
        {
            return patch.ValidateMagicBytes() && patch.ValidateCRC() && patch.ValidateVersion();
        }

        public string GetDeflatePath()
        {
            return Path.ChangeExtension(GetPath(), "patch.dump");
        }

        public string DumpDeflateStream(string filePath = "")
        {
            filePath = filePath.Length > 0 ? filePath : GetDeflatePath();
            var f = new FileStream(filePath, FileMode.Create);
            var d = patch.GetDeflateStream();
            d.CopyTo(f);
            f.Close();
            return filePath;
        }

        public string GetPath()
        {
            return path;
        }


    }
}
