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
            return path;
        }
    }

    public class EXEFileWriter
    {
        uint magicBytes = 0xF2F7FBF3;
        BinaryWriter bw;
        public EXEFileWriter(string filePath)
        {
            bw = new BinaryWriter(File.Create(filePath));
        }

        public void WriteEXE(Stream exe, Stream patch, Stream notice)
        {
            exe.CopyTo(bw.BaseStream);
            patch.CopyTo(bw.BaseStream);
            notice.CopyTo(bw.BaseStream);
            bw.Write((uint)patch.Length);
            bw.Write((uint)notice.Length);
            bw.Write(magicBytes);
        }
        
    }

    public class EXEPatchWriter : PEXEPatch
    {
        protected BinaryWriter exepatch;

        public EXEPatchWriter(string filePath)
        {
            path = filePath;
            exepatch = new BinaryWriter(File.Create(GetPath()));
        }

        public void WritePatch(Stream baseEXE, Stream patch, Stream notice)
        {
            baseEXE.CopyTo(exepatch.BaseStream);
            patch.CopyTo(exepatch.BaseStream);
            notice.CopyTo(exepatch.BaseStream);
            exepatch.Write((UInt32)patch.Length);
            exepatch.Write((UInt32)notice.Length);
            exepatch.Write(EXEPatchWriter.magicBytesContents);
        }
    }

    public class EXEPatchReader
    {
        protected int sizeHeader = 12;
        protected int magicFooterOffset = -4;
        protected int noticeFooterOffset = -8;
        protected int patchFooterOffset = -12;
        protected BinaryReader br;

        public EXEPatchReader(string filePath)
        {
            br = new BinaryReader(File.Open(filePath, FileMode.Open));
        }

        protected int ReadIntFromFooter(int offsetFromEnd)
        {
            br.BaseStream.Seek(offsetFromEnd, SeekOrigin.End);
            return br.ReadInt32();
        }

        protected int ReadMagicBytes()
        {
            return ReadIntFromFooter(magicFooterOffset);
        }

        protected int ReadNoticeLength()
        {
            return ReadIntFromFooter(noticeFooterOffset);
        }

        protected int ReadPatchLength()
        {
            return ReadIntFromFooter(patchFooterOffset);
        }

        protected Stream CreateStreamFromOffset(long offset, int length)
        {
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            byte[] arr = new byte[length];
            br.Read(arr, 0, arr.Length);
            return new MemoryStream(arr);
        }

        protected int CalculateEXELength()
        {
            return (int)br.BaseStream.Length - ReadNoticeLength() - ReadPatchLength() - sizeHeader;
        }

        public Stream GetEXE()
        {
            
            return CreateStreamFromOffset(0, CalculateEXELength());
        }

        protected void WriteStreamToFile(string toFile, Stream data)
        {
            var writer = File.Open(toFile, FileMode.Create);
            data.CopyTo(writer);
            writer.Close();
        }

        public void WriteEXE(string toFile)
        {
            WriteStreamToFile(toFile, GetEXE());
        }

        public Stream GetPatch()
        {
            return CreateStreamFromOffset(CalculateEXELength(), ReadPatchLength());
        }

        public void WritePatch(string toFile)
        {
            WriteStreamToFile(toFile, GetPatch());
        }

        public Stream GetNotice()
        {
            return CreateStreamFromOffset(CalculateEXELength() + ReadPatchLength(), ReadNoticeLength());
        }

        public void WriteNotice(string toFile)
        {
            WriteStreamToFile(toFile, GetNotice());
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
            path = filePath;
            exepatch = new BinaryReader(File.Open(GetPath(), FileMode.Open));
            Parse();
        }

        ~EXEPatchAccessor()
        {
            exepatch.Close();
        }



        public UInt32 GetMagicBytes()
        {
            exepatch.BaseStream.Seek(EXEPatchAccessor.offsetMagicBytes, SeekOrigin.End);
            return exepatch.ReadUInt32();
        }

        public bool ValidateMagicBytes()
        {
            return GetMagicBytes() == EXEPatchAccessor.magicBytesContents;
        }

        public UInt32 GetNoticeLength()
        {
            exepatch.BaseStream.Seek(EXEPatchAccessor.offsetLengthNotice, SeekOrigin.End);
            return exepatch.ReadUInt32();
        }

        public UInt32 GetPatchLength()
        {
            exepatch.BaseStream.Seek(EXEPatchAccessor.offsetLengthPatch, SeekOrigin.End);
            return exepatch.ReadUInt32();
        }

        public string GetBasePath()
        {
            return Path.ChangeExtension(GetPath(), "base");
        }

        public void DumpBase(string fileName = "")
        {
            fileName = fileName.Length > 0 ? fileName : GetBasePath();
            var f = File.Create(fileName);
            f.Write(GetBase(), 0, (int)GetBaseLength());
            f.Close();
        }

        public UInt32 GetBaseLength()
        {
            return lengthEXE;
        }

        public byte[] GetBase()
        {
            exepatch.BaseStream.Seek(offsetEXE, SeekOrigin.Begin);
            var bytes = new byte[lengthEXE];
            exepatch.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public string GetPatchPath()
        {
            return Path.ChangeExtension(GetPath(), "patch");
        }

        public byte[] GetPatch()
        {
            exepatch.BaseStream.Seek(offsetPatch, SeekOrigin.Begin);
            var bytes = new byte[lengthPatch];
            exepatch.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public void DumpPatch(string fileName = "")
        {
            fileName = fileName.Length > 0 ? fileName : GetPatchPath();
            var f = File.Create(fileName);
            f.Write(GetPatch(), 0, (int)GetPatchLength());
            f.Close();
        }

        public byte[] GetNotice()
        {
            exepatch.BaseStream.Seek(offsetNotice, SeekOrigin.Begin);
            var bytes = new byte[lengthNotice];
            exepatch.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public string GetNoticePath()
        {
            return Path.ChangeExtension(GetPath(), "txt");
        }

        public void DumpNotice(string fileName = "")
        {
            fileName = fileName.Length > 0 ? fileName : GetNoticePath();
            var f = File.Create(fileName);
            f.Write(GetNotice(), 0, (int)GetNoticeLength());
            f.Close();
        }

        protected void Parse()
        {
            if (ValidateMagicBytes())
            {

                magicBytes = GetMagicBytes();
                lengthNotice = GetNoticeLength();
                lengthPatch = GetPatchLength();
                lengthEXE = (UInt32)exepatch.BaseStream.Length - EXEPatchAccessor.lengthFooter - lengthPatch - lengthNotice;

                offsetEXE = 0;
                offsetPatch = offsetEXE + lengthEXE;
                offsetNotice = offsetPatch + lengthPatch;
            }
            else
            {
                throw new System.InvalidOperationException(String.Format("EXEPatchAccessor: File {0} does not contain the proper magic bytes in the footer. This file is invalid", GetPath()));
            }
        }
    }
}
