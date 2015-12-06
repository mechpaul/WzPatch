using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace WzPatch
{
    public class PatchAccessor
    {
        protected BinaryReader patch;
        protected string path;

        protected const int MagicBytesOffset = 0;
        protected const string MagicBytesContents = "WzPatch\x1A";

        protected const int VersionOffset = 8;
        protected const int VersionContents = 2;

        protected const int CRCOffset = 12;

        protected const int ZlibOffset = 16;
        protected const int DeflateOffset = 18;

        public PatchAccessor(string filePath)
        {
            this.path = filePath;
            this.patch = new BinaryReader(File.Open(this.GetPath(), FileMode.Open));
        }

        public string GetMagicBytes()
        {
            var s = new byte[PatchAccessor.MagicBytesContents.Length];
            this.patch.BaseStream.Position = PatchAccessor.MagicBytesOffset;
            this.patch.Read(s, 0, s.Length);
            string magicBytes = Encoding.UTF8.GetString(s);
            return magicBytes;
        }

        public bool ValidateMagicBytes()
        {
            return this.GetMagicBytes() == PatchAccessor.MagicBytesContents;
        }

        public UInt32 GetCRC()
        {
            this.patch.BaseStream.Position = PatchAccessor.CRCOffset;
            return this.patch.ReadUInt32();
        }

        public bool ValidateCRC()
        {
            var crc = new Crc32();
            this.patch.BaseStream.Position = PatchAccessor.ZlibOffset;
            crc.Update(this.patch);
            UInt32 calculatedcrc, patchcrc;
            calculatedcrc = crc.GetCRC();
            patchcrc = this.GetCRC();
            return calculatedcrc == patchcrc;
        }

        public UInt32 GetVersion()
        {
            this.patch.BaseStream.Position = PatchAccessor.VersionOffset;
            return this.patch.ReadUInt32();
        }

        public bool ValidateVersion()
        {
            return this.GetVersion() == PatchAccessor.VersionContents;
        }

        public DeflateStream GetDeflateStream()
        {
            this.patch.BaseStream.Position = PatchAccessor.DeflateOffset;
            return new DeflateStream(this.patch.BaseStream, CompressionMode.Decompress);
        }

        public string GetDeflatePath()
        {
            return Path.ChangeExtension(this.GetPath(), "patch.dump");
        }

        public void DumpDeflateStream(string filePath = "")
        {
            filePath = filePath.Length > 0 ? filePath : this.GetDeflatePath();
            var f = new FileStream(filePath, FileMode.Create);
            var d = this.GetDeflateStream();
            d.CopyTo(f);
            f.Close();
        }

        public string GetPath()
        {
            return this.path;
        }


    }
}
