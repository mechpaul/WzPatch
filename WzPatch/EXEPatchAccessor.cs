using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WzPatch
{
    public class PEXEPatch
    {
        protected const UInt32 magicBytesContents = 0xF2F7FBF3;
        protected UInt32 lengthNotice;
        protected UInt32 lengthPatch;
        protected UInt32 lengthEXE;
        
        protected string path;

        public string GetPath()
        {
            return this.path;
        }
    }

    public class EXEPatchWriter : PEXEPatch
    {
        protected BinaryWriter exepatch;

        public EXEPatchWriter(string filePath)
        {
            this.path = filePath;
            this.exepatch = new BinaryWriter(File.Create(this.GetPath()));
        }

        public void WritePatch(Stream baseEXE, Stream patch, Stream notice)
        {
            baseEXE.CopyTo(this.exepatch.BaseStream);
            patch.CopyTo(this.exepatch.BaseStream);
            notice.CopyTo(this.exepatch.BaseStream);
            this.exepatch.Write((UInt32)patch.Length);
            this.exepatch.Write((UInt32)notice.Length);
            this.exepatch.Write(EXEPatchWriter.magicBytesContents);
        }
    }

    public class EXEPatchAccessor : PEXEPatch
    {
        protected BinaryReader exepatch;

        protected UInt32 magicBytes;


        protected UInt32 offsetEXE;
        protected UInt32 offsetPatch;
        protected UInt32 offsetNotice;



        protected const int lengthFooter = 12;
        protected const int offsetMagicBytes = -4;
        protected const int offsetLengthNotice = -8;
        protected const int offsetLengthPatch = -12;

        

        public EXEPatchAccessor(string filePath)
        {
            this.path = filePath;
            this.exepatch = new BinaryReader(File.Open(this.GetPath(), FileMode.Open));
            this.Parse();
        }

        ~EXEPatchAccessor()
        {
            this.exepatch.Close();
        }



        public UInt32 GetMagicBytes()
        {
            this.exepatch.BaseStream.Seek(EXEPatchAccessor.offsetMagicBytes, SeekOrigin.End);
            return this.exepatch.ReadUInt32();
        }

        public bool ValidateMagicBytes()
        {
            return this.GetMagicBytes() == EXEPatchAccessor.magicBytesContents;
        }

        public UInt32 GetNoticeLength()
        {
            this.exepatch.BaseStream.Seek(EXEPatchAccessor.offsetLengthNotice, SeekOrigin.End);
            return this.exepatch.ReadUInt32();
        }

        public UInt32 GetPatchLength()
        {
            this.exepatch.BaseStream.Seek(EXEPatchAccessor.offsetLengthPatch, SeekOrigin.End);
            return this.exepatch.ReadUInt32();
        }

        public string GetBasePath()
        {
            return Path.ChangeExtension(this.GetPath(), "base");
        }

        public void DumpBase(string fileName = "")
        {
            fileName = fileName.Length > 0 ? fileName : this.GetBasePath();
            var f = File.Create(fileName);
            f.Write(this.GetBase(), 0, (int)this.GetBaseLength());
            f.Close();
        }

        public UInt32 GetBaseLength()
        {
            return this.lengthEXE;
        }

        public byte[] GetBase()
        {
            this.exepatch.BaseStream.Seek(this.offsetEXE, SeekOrigin.Begin);
            var bytes = new byte[this.lengthEXE];
            this.exepatch.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public string GetPatchPath()
        {
            return Path.ChangeExtension(this.GetPath(), "patch");
        }

        public byte[] GetPatch()
        {
            this.exepatch.BaseStream.Seek(this.offsetPatch, SeekOrigin.Begin);
            var bytes = new byte[this.lengthPatch];
            this.exepatch.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public void DumpPatch(string fileName = "")
        {
            fileName = fileName.Length > 0 ? fileName : this.GetPatchPath();
            var f = File.Create(fileName);
            f.Write(this.GetPatch(), 0, (int)this.GetPatchLength());
            f.Close();
        }

        public byte[] GetNotice()
        {
            this.exepatch.BaseStream.Seek(this.offsetNotice, SeekOrigin.Begin);
            var bytes = new byte[this.lengthNotice];
            this.exepatch.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public string GetNoticePath()
        {
            return Path.ChangeExtension(this.GetPath(), "txt");
        }

        public void DumpNotice(string fileName = "")
        {
            fileName = fileName.Length > 0 ? fileName : this.GetNoticePath();
            var f = File.Create(fileName);
            f.Write(this.GetNotice(), 0, (int)this.GetNoticeLength());
            f.Close();
        }

        protected void Parse()
        {
            if (this.ValidateMagicBytes())
            {

                this.magicBytes = this.GetMagicBytes();
                this.lengthNotice = this.GetNoticeLength();
                this.lengthPatch = this.GetPatchLength();
                this.lengthEXE = (UInt32)this.exepatch.BaseStream.Length - EXEPatchAccessor.lengthFooter - this.lengthPatch - this.lengthNotice;

                this.offsetEXE = 0;
                this.offsetPatch = this.offsetEXE + this.lengthEXE;
                this.offsetNotice = this.offsetPatch + this.lengthPatch;
            }
            else
            {
                throw new System.InvalidOperationException(String.Format("EXEPatchAccessor: File {0} does not contain the proper magic bytes in the footer. This file is invalid", this.GetPath()));
            }
        }
    }
}
