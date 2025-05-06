using Cruncher.Util;
using Cruncher.Script.Interpreter;
using System.Runtime.InteropServices;
using Cruncher.Script.Lexer;

namespace Cruncher.Script.Packing
{
    public sealed class Packer(Package[] packages)
    {
        struct Header
        {
            public uint Magic; //0x434E5243 We actually need to reverse this since, otherwise in will be CNRC in a hex editor. We want to store it in big endian
            public uint Version; //This will be the requested version of cruncher, NOT the current version supported by this build
            public ushort FileCount; //The number of files in the package
            public ulong FileTableOffset; //The offset to the start of the file table
            public ulong DataOffset; //The offset to the start of the data section
        }

        struct FileEntry
        {
            public string name;
            public ulong hash;
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
                    size += (ulong)(Marshal.SizeOf<ulong>() * 3) + (ulong)entry.name.Length + 1;

                return size;
            }
        }

        struct FileData
        {
            public byte[] data;
            public byte[] padding; //The next data section should begin on a 16 byte boundary
        }

        private readonly Package[] mPackages = packages;

        private byte[] GetFileContents(string filePath, TokenType fileType)
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

        private void Pack(Package package)
        {
            Header header = new()
            {
                Magic = 0x434E5243,
                Version = Version.Current.PackedVersion,
                FileCount = (ushort)package.Files.Length,
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
                    name = package.Files[i].Item1,
                    hash = package.Files[i].Item1.Hash(),
                    offset = 0,
                    size = 0
                };

                fileTable.entries[i] = entry;
            }

            //We should now know the size of the file table and header combined
            //The data section should be aligned to 16 bytes, so we can add some padding after the file table
            ulong totalSizeOfHeaderAndFileTable = (ulong)Marshal.SizeOf<Header>() + fileTable.GetSize();
            ulong padding = 0;
            if (totalSizeOfHeaderAndFileTable % 16 != 0)
                padding = 16 - (totalSizeOfHeaderAndFileTable % 16);

            header.FileTableOffset = (ulong)Marshal.SizeOf<Header>();
            header.DataOffset = totalSizeOfHeaderAndFileTable + padding;

            //We now know where the data section begins. With this, we can start loading in files
            //and calculating their offsets and sizes
            ulong currentOffset = header.DataOffset;
            for (int i = 0; i < package.Files.Length; i++)
            {
                FileEntry entry = fileTable.entries[i];
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), entry.name);
                byte[] data = GetFileContents(filePath, package.Files[i].Item2);

                if (data == null)
                {
                    IO.LogError($"Failed to load contents of [yellow]'{filePath}'[/]");
                    continue;
                }

                entry.offset = currentOffset;
                entry.size = (ulong)data.Length;
                currentOffset += entry.size;

                //Add padding to ensure the next file is aligned to 16 bytes
                fileDatas[i] = new FileData
                {
                    data = data,
                    padding = []
                };

                if (currentOffset % 16 != 0)
                {
                    ulong pad = 16 - (currentOffset % 16);
                    fileDatas[i].padding = new byte[pad];
                    currentOffset += pad;
                }
            }

            //We should now have all the data we need to write the package to disk
            string packageName = Path.Combine(Directory.GetCurrentDirectory(), $"{package.Name}.crunch");
            using FileStream stream = new(packageName, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(stream);
            writer.Write(header.Magic);
            writer.Write(header.Version);
            writer.Write(header.FileCount);
            writer.Write(header.FileTableOffset);
            writer.Write(header.DataOffset);
            writer.Write(new byte[padding]);
            foreach (FileEntry entry in fileTable.entries)
            {
                writer.Write(entry.hash);
                writer.Write(entry.name);
                writer.Write((byte)0x00); //Null terminator
            }
            foreach (FileData fileData in fileDatas)
            {
                writer.Write(fileData.data);
                writer.Write(fileData.padding);
            }

            writer.Flush();
            stream.Close();

            IO.LogSuccess($"Packed {package.Files.Length} files into {packageName}");
        }

        public void PackPackages()
        {
            foreach (Package package in mPackages)
                Pack(package);
        }
    }
}