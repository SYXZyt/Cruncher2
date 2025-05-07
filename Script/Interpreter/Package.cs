using Cruncher.Script.Lexer;
using Cruncher.Util;

namespace Cruncher.Script.Interpreter
{
    public sealed class Package(string name, Version version)
    {
        private readonly string mName = name;
        private string mExt = null;
        private readonly Version mVersion = version;

        private readonly List<Tuple<string, TokenType>> mFiles = [];

        public string Extension
        {
            get => mExt;
            set => mExt = value;
        }

        public string Name =>
            mName;

        public Tuple<string, TokenType>[] Files =>
           [.. mFiles];

        public Version Version =>
            mVersion;

        public void AddFile(string file, TokenType fileType)
        {
            if (!mFiles.Any(f => f.Item1 == file))
                mFiles.Add(new(file, fileType));
            else
                IO.LogWarning($"File {file} already exists in package {mName}");
        }
    }
}