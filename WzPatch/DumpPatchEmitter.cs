using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WzPatch
{
    public class DumpPatchEmitter
    {
        protected string path;
        protected BinaryReader br;
        protected DumpBlock dumpB;
        protected RebuildBlock rebuildB;
        protected bool currentRebuildBlock;

        public DumpPatchEmitter(string filePath)
        {
            this.path = filePath;
            this.br = new BinaryReader(File.Open(this.GetPath(), FileMode.Open));
        }

        ~DumpPatchEmitter()
        {
            this.br.Close();
        }

        public string GetPath()
        {
            return this.path;
        }

        public DumpBlock ReadBlockString()
        {
            if(currentRebuildBlock == true)
            {
                this.SkipRebuildBlocks();
            }
            currentRebuildBlock = false;

            this.dumpB = new DumpBlock();
            char readCh;
            string blockString = "";
            string lastChar = "";

            if(this.br.BaseStream.Position == this.br.BaseStream.Length)
            {
                this.dumpB.fileBlockMode = EFileBlockMode.End;
                return this.dumpB;
            }

            while(true)
            {
                readCh = this.br.ReadChar();
                switch((int)readCh)
                {
                    case 0:
                        lastChar = blockString.Substring(blockString.Length - 1);
                        if(lastChar == "\\" || lastChar == "/")
                        {
                            this.dumpB.fileBlockMode = EFileBlockMode.CreateDirectory;
                            this.dumpB.filePath = blockString;
                        }
                        else
                        {
                            this.dumpB.fileBlockMode = EFileBlockMode.CreateFile;
                            this.dumpB.filePath = blockString;
                            this.dumpB.length = this.br.ReadUInt32();
                            this.dumpB.crc = this.br.ReadUInt32();
                            this.dumpB.dumpFile = new byte[this.dumpB.length];
                            this.br.BaseStream.Read(this.dumpB.dumpFile, 0, this.dumpB.dumpFile.Length);
                        }
                        return this.dumpB;
                    case 1:
                        this.dumpB.fileBlockMode = EFileBlockMode.Rebuild;
                        this.dumpB.filePath = blockString;
                        this.dumpB.crcOld = this.br.ReadUInt32();
                        this.dumpB.crc = this.br.ReadUInt32();
                        this.currentRebuildBlock = true;
                        return this.dumpB;
                    case 2:
                        lastChar = blockString.Substring(blockString.Length - 1);
                        if(lastChar == "\\" || lastChar == "/")
                        {
                            this.dumpB.fileBlockMode = EFileBlockMode.DeleteDirectory;
                            this.dumpB.filePath = blockString;
                        }
                        else
                        {
                            this.dumpB.fileBlockMode = EFileBlockMode.DeleteFile;
                            this.dumpB.filePath = blockString;
                        }
                        return this.dumpB;
                    default:
                        blockString += readCh;
                        break;
                }

            }
        }

        public RebuildBlock ReadRebuildBlock()
        {
            this.rebuildB = new RebuildBlock();
            UInt32 command;

            command = this.br.ReadUInt32();

            if((command == 0))
            {
                this.rebuildB.fileBlockRebuild = EFileBlockRebuild.End;
                this.currentRebuildBlock = false;
            }
            else if((command & 0xC0000000) == 0xC0000000)
            {
                this.rebuildB.fileBlockRebuild = EFileBlockRebuild.Repeat;
                this.rebuildB.repeatedByte = (byte)(command & 0xFF);
                this.rebuildB.length = (command & 0x3FFFF00) >> 8;
                this.rebuildB.raw = new byte[this.rebuildB.length];
                for(int i = 0; i < this.rebuildB.raw.Length; i++)
                {
                    this.rebuildB.raw[i] = this.rebuildB.repeatedByte;
                }
            }
            else if((command & 0x80000000) == 0x80000000)
            {
                this.rebuildB.fileBlockRebuild = EFileBlockRebuild.Write;
                this.rebuildB.length = command & 0x7FFFFFFF;
                this.rebuildB.raw = new byte[this.rebuildB.length];
                this.br.Read(this.rebuildB.raw, 0, this.rebuildB.raw.Length);
            }
            else
            {
                this.rebuildB.fileBlockRebuild = EFileBlockRebuild.Copy;
                this.rebuildB.length = command;
                this.rebuildB.oldFileOffset = this.br.ReadUInt32();
            }


            return this.rebuildB;
        }

        public void SkipRebuildBlocks()
        {
            if (this.currentRebuildBlock == true)
            {
                while (true)
                {
                    var rBlock = this.ReadRebuildBlock();
                    if (rBlock.fileBlockRebuild == EFileBlockRebuild.End)
                    {
                        break;
                    }
                }
            }
        }

    }
}
