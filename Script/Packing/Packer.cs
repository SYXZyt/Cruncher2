using Cruncher.Util;
using Cruncher.Script.Interpreter;
using System.Runtime.InteropServices;
using Cruncher.Script.Lexer;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.GZip;

namespace Cruncher.Script.Packing
{
    public sealed class Packer(Package[] packages)
    {
        private static Version mVersion;

        struct Header
        {
            public uint Magic; //0x434E5243 We actually need to reverse this since, otherwise in will be CNRC in a hex editor. We want to store it in big endian
            public uint Version; //This will be the requested version of cruncher, NOT the current version supported by this build
            public ulong FileCount; //The number of files in the package
            public ulong FileTableOffset; //The offset to the start of the file table
            public ulong DataOffset; //The offset to the start of the data section
        }

        struct FileEntry
        {
            public string name;
            public ulong hash;
            public bool compressed;
            public ulong offset;
            public ulong size;
        }

        struct FileTable
        {
            public FileEntry[] entries;

            public readonly ulong GetSize()
            {
                ulong size = 0;
                foreach (FileEntry entry in entries)
                {
                    size += (ulong)(Marshal.SizeOf<ulong>() * 3) + (ulong)entry.name.Length + 1;

                    //Add compression byte
                    if (mVersion >= new Version(2, 2, 0))
                        ++size;
                }

                return size;
            }
        }

        struct FileCompression
        {
            public byte[] compressed;
            public float reductionPercent;
        }

        struct FileData
        {
            public byte[] data;
            public FileCompression compression;
            public bool isCompressed;
        }

        private readonly Package[] mPackages = packages;
        private const float AcceptableCompressionPercent = 40; //40% reduction. I.e. 100 bytes -> 60 bytes
        private static byte[] GetFileContents(string filePath, TokenType fileType)
        {
            if (!File.Exists(filePath))
            {
                IO.LogError($"The file {filePath} does not exist.");
                return Array.Empty<byte>();
            }

            if (fileType == TokenType.FT_TEXT)
            {
                string text = File.ReadAllText(filePath);
                return System.Text.Encoding.UTF8.GetBytes(text);
            }
            else if (fileType == TokenType.FT_BINARY)
            {
                return File.ReadAllBytes(filePath);
            }

            return null;
        }

        private static byte[] Compress(byte[] data)
        {
            using MemoryStream output = new();
            using GZipOutputStream gzipStream = new(output);
            gzipStream.Write(data, 0, data.Length);

            return output.ToArray();
        }

        private static void Pack(Package package, string outputDir)
        {
            mVersion = package.Version;

            Header header = new()
            {
                Magic = 0x434E5243,
                Version = Version.Current.PackedVersion,
                FileCount = (ulong)package.Files.Length,
                FileTableOffset = (ulong)Marshal.SizeOf<Header>(),
                DataOffset = 0
            };

            FileTable fileTable = new()
            {
                entries = new FileEntry[package.Files.Length]
            };

            FileData[] fileDatas = new FileData[package.Files.Length];

            for (int i = 0; i < package.Files.Length; i++)
            {
                FileEntry entry = new()
                {
                    name = package.Files[i].Item1.NormaliseDirectory(),
                    hash = package.Files[i].Item1.NormaliseDirectory().Hash(),
                    offset = 0,
                    size = 0
                };

                fileTable.entries[i] = entry;
            }

            ulong totalSizeOfHeaderAndFileTable = (ulong)Marshal.SizeOf<Header>() + fileTable.GetSize();

            header.FileTableOffset = (ulong)Marshal.SizeOf<Header>();
            header.DataOffset = totalSizeOfHeaderAndFileTable;

            //We now know where the data section begins. With this, we can start loading in files
            //and calculating their offsets and sizes
            ulong currentOffset = 0ul;
            for (int i = 0; i < package.Files.Length; i++)
            {
                ref FileEntry entry = ref fileTable.entries[i];
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), entry.name);
                byte[] data = GetFileContents(filePath, package.Files[i].Item2);

                if (data == null)
                {
                    IO.LogError($"Failed to load contents of [yellow]'{filePath}'[/]");
                    continue;
                }

                fileDatas[i] = new FileData
                {
                    data = data,
                    compression = new FileCompression
                    {
                        compressed = Compress(data),
                    },
                    isCompressed = false,
                };

                float compressedSize = fileDatas[i].compression.compressed.Length;
                float originalSize = data.Length;

                fileDatas[i].compression.reductionPercent = (1.0f - ((float)compressedSize / (float)originalSize)) * 100.0f;
                fileDatas[i].isCompressed = fileDatas[i].compression.reductionPercent >= AcceptableCompressionPercent;

                entry.offset = currentOffset;
                entry.size = fileDatas[i].isCompressed ? (ulong)fileDatas[i].compression.compressed.Length : (ulong)data.Length;
                entry.compressed = fileDatas[i].isCompressed;

                //If compression is not allowed, i.e. we are on version 2.1.0- then we need to set the size to the uncompressed size
                if (package.Version < new Version(2, 2, 0))
                {
                    fileDatas[i].isCompressed = false;
                }

                currentOffset += entry.size;

                if (fileDatas[i].isCompressed)
                    IO.Log($"Compressing file '[yellow]{entry.name}[/]' with reduction of [green]{fileDatas[i].compression.reductionPercent:f1}%[/]");
            }

            //We should now have all the data we need to write the package to disk
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + outputDir + "/");
            string packageName = Path.Combine(Directory.GetCurrentDirectory() + "/" + outputDir + "/", $"{package.Name}.{package.Extension ?? "crunch"}");
            using FileStream stream = new(packageName, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(stream);
            writer.Write(header.Magic);
            writer.Write(header.Version);
            writer.Write(header.FileCount);
            writer.Write(header.FileTableOffset);
            writer.Write(header.DataOffset);
            foreach (FileEntry entry in fileTable.entries)
            {
                writer.Write(entry.hash);

                //If we write the string directly, then it will write an encoded length, which we don't want
                //since we use null terminators
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(entry.name);
                writer.Write(bytes);
                writer.Write((byte)0x00); //Null terminator

                //If we are on a build that supports compression
                //then we have to write the compressed flag
                if (package.Version >= new Version(2, 2, 0))
                {
                    writer.Write(entry.compressed ? (byte)0xff : (byte)0);
                }

                writer.Write(entry.size);
                writer.Write(entry.offset);
            }
            foreach (FileData fileData in fileDatas)
            {
                writer.Write(fileData.isCompressed ? fileData.compression.compressed : fileData.data);
            }

            writer.Flush();
            stream.Close();

            IO.LogSuccess($"Packed {package.Files.Length} files into {packageName}");
        }

        public void PackPackages(string outputDir)
        {
            foreach (Package package in mPackages)
                Pack(package, outputDir);
        }
    }
}