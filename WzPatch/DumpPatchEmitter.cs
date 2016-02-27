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
            path = filePath;
            br = new BinaryReader(File.Open(GetPath(), FileMode.Open));
        }

        ~DumpPatchEmitter()
        {
            br.Close();
        }

        public string GetPath()
        {
            return path;
        }

        public DumpBlock ReadBlockString()
        {
            if(currentRebuildBlock == true)
            {
                SkipRebuildBlocks();
            }
            currentRebuildBlock = false;

            dumpB = new DumpBlock();
            char readCh;
            string blockString = "";
            string lastChar = "";

            if(br.BaseStream.Position == br.BaseStream.Length)
            {
                dumpB.fileBlockMode = EFileBlockMode.End;
                return dumpB;
            }

            while(true)
            {
                readCh = br.ReadChar();
                switch((int)readCh)
                {
                    case 0:
                        lastChar = blockString.Substring(blockString.Length - 1);
                        if(lastChar == "\\" || lastChar == "/")
                        {
                            dumpB.fileBlockMode = EFileBlockMode.CreateDirectory;
                            dumpB.filePath = blockString;
                        }
                        else
                        {
                            dumpB.fileBlockMode = EFileBlockMode.CreateFile;
                            dumpB.filePath = blockString;
                            dumpB.length = br.ReadUInt32();
                            dumpB.crc = new Crc32(br.ReadUInt32());
                            dumpB.dumpFile = new byte[dumpB.length];
                            br.BaseStream.Read(dumpB.dumpFile, 0, dumpB.dumpFile.Length);
                        }
                        return dumpB;
                    case 1:
                        dumpB.fileBlockMode = EFileBlockMode.Rebuild;
                        dumpB.filePath = blockString;
                        dumpB.crcOld = new Crc32(br.ReadUInt32());
                        dumpB.crc = new Crc32(br.ReadUInt32());
                        currentRebuildBlock = true;
                        return dumpB;
                    case 2:
                        lastChar = blockString.Substring(blockString.Length - 1);
                        if(lastChar == "\\" || lastChar == "/")
                        {
                            dumpB.fileBlockMode = EFileBlockMode.DeleteDirectory;
                            dumpB.filePath = blockString;
                        }
                        else
                        {
                            dumpB.fileBlockMode = EFileBlockMode.DeleteFile;
                            dumpB.filePath = blockString;
                        }
                        return dumpB;
                    default:
                        blockString += readCh;
                        break;
                }

            }
        }

        public RebuildBlock ReadRebuildBlock()
        {
            rebuildB = new RebuildBlock();
            var command = br.ReadUInt32();

            if(command == 0)
            {
                rebuildB.fileBlockRebuild = EFileBlockRebuild.End;
                currentRebuildBlock = false;
            }
            else if((command & 0xC0000000) == 0xC0000000)
            {
                rebuildB.fileBlockRebuild = EFileBlockRebuild.Repeat;
                rebuildB.repeatedByte = (byte)(command & 0xFF);
                rebuildB.length = (command & 0x3FFFF00) >> 8;
                rebuildB.raw = new byte[rebuildB.length];
                for(int i = 0; i < rebuildB.raw.Length; i++)
                {
                    rebuildB.raw[i] = rebuildB.repeatedByte;
                }
            }
            else if((command & 0x80000000) == 0x80000000)
            {
                rebuildB.fileBlockRebuild = EFileBlockRebuild.Write;
                rebuildB.length = command & 0x7FFFFFFF;
                rebuildB.raw = new byte[rebuildB.length];
                br.Read(rebuildB.raw, 0, rebuildB.raw.Length);
            }
            else
            {
                rebuildB.fileBlockRebuild = EFileBlockRebuild.Copy;
                rebuildB.length = command;
                rebuildB.oldFileOffset = br.ReadUInt32();
            }


            return rebuildB;
        }

        public void SkipRebuildBlocks()
        {
            if (currentRebuildBlock == true)
            {
                while (true)
                {
                    var rBlock = ReadRebuildBlock();
                    if (rBlock.fileBlockRebuild == EFileBlockRebuild.End)
                    {
                        break;
                    }
                }
            }
        }

    }
}
