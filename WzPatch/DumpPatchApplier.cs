using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WzPatch
{
    public class DumpPatchApplier
    {
        protected string dumpedPatchFile;
        protected string dirApply;
        protected string dirOut;
        protected DumpPatchEmitter dpe;

        public DumpPatchApplier(string dumpedPatchFilePath, string applyDirectory, string outputDirectory)
        {
            dumpedPatchFile = dumpedPatchFilePath;
            dirApply = applyDirectory;
            dirOut = outputDirectory;
            if(!Directory.Exists(dirApply))
            {
                throw new System.InvalidProgramException("DumpPatchApplier - Cannot set the directory to apply the patch file when that directory does not exist!");
            }
            Directory.CreateDirectory(dirOut);
        }

        public void ApplyPatch()
        {
            dpe = new DumpPatchEmitter(dumpedPatchFile);
            string path = "";
            Stream fs;
            bool end = false;
            while(true)
            {
                var dumpblock = dpe.ReadBlockString();
                switch(dumpblock.fileBlockMode)
                {
                    case EFileBlockMode.CreateDirectory:
                        Directory.CreateDirectory(dumpblock.filePath);
                        break;
                    case EFileBlockMode.CreateFile:
                        var crc = new Crc32();
                        crc.Update(dumpblock.dumpFile, dumpblock.dumpFile.Length);
                        if(crc == dumpblock.crc)
                        {
                            path = Path.Combine(dirOut, dumpblock.filePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            fs = File.Create(path);
                            
                            fs.Write(dumpblock.dumpFile, 0, dumpblock.dumpFile.Length);
                            fs.Close();
                        }
                        else
                        {
                            throw new System.InvalidProgramException(String.Format(strings.DumpPatchApplierCRCFail, dumpblock.filePath));
                            //throw new System.InvalidProgramException(String.Format("DumpPatchApplier: CRC failed for created file {1}. This means the patch file is corrupted. Please redownload the patch", dumpblock.filePath));
                        }
                        break;
                    case EFileBlockMode.DeleteDirectory:
                        path = Path.Combine(dirOut, "deletedfiles.txt");
                        var sw = File.AppendText(path);
                        sw.WriteLine(dumpblock.filePath);
                        sw.Close();
                        break;
                    case EFileBlockMode.Rebuild:
                        RebuildFile(dumpblock);
                        break;
                    case EFileBlockMode.End:
                        end = true;
                        break;
                }
                if(end == true)
                {
                    break;
                }

            }
        
        }

        public void RebuildFile(DumpBlock dumpblock)
        {
            var pathOut = Path.Combine(dirOut, dumpblock.filePath);
            var pathIn = Path.Combine(dirApply, dumpblock.filePath);
            var crc = new Crc32();
            var crcOld = new Crc32();
            byte[] buf;
            bool end = false;
             
            Directory.CreateDirectory(Path.GetDirectoryName(pathOut));

            var oldfs = new BinaryReader(File.Open(pathIn, FileMode.Open));
            var newfs = new BinaryWriter(File.Create(pathOut));

            crcOld.Update(oldfs);

            if(crcOld == dumpblock.crcOld)
            {
                while(true)
                {
                    var rb = dpe.ReadRebuildBlock();
                    switch(rb.fileBlockRebuild)
                    {
                        case EFileBlockRebuild.Copy:
                            buf = new byte[rb.length];
                            oldfs.BaseStream.Position = rb.oldFileOffset;
                            oldfs.Read(buf, 0, buf.Length);
                            crc.Update(buf, buf.Length);
                            newfs.Write(buf, 0, buf.Length);                            
                            break;
                        case EFileBlockRebuild.Repeat:
                            crc.Update(rb.raw, rb.raw.Length);
                            newfs.Write(rb.raw, 0, rb.raw.Length);
                            break;
                        case EFileBlockRebuild.Write:
                            crc.Update(rb.raw, rb.raw.Length);
                            newfs.Write(rb.raw, 0, rb.raw.Length);
                            break;
                        case EFileBlockRebuild.End:
                            end = true;
                            break;
                    }
                    if(end == true)
                    {
                        break;
                    }
                }

                if(crc != dumpblock.crc)
                {
                    throw new System.InvalidProgramException(String.Format("DumpPatchApplier: The calculated CRC for the newly created file is wrong. Please contact Fiel to fix this as there's a problem with the algorithm"));
                }
            }
            else
            {
                throw new System.InvalidProgramException(String.Format("DumpPatchApplier: The calculated CRC for the old file {0} is wrong. Expected: {1}, Calculated: {2}", pathIn, dumpblock.crcOld.ToString(), crcOld.ToString()));
            }
        }
    }
}
