using Cruncher.Script.Lexer;
using Cruncher.Util;

namespace Cruncher.Script.Interpreter
{
    public sealed class Package(string name)
    {
        private readonly string mName = name;
        private readonly List<Tuple<string, TokenType>> mFiles = [];

        public string Name =>
            mName;

        public Tuple<string, TokenType>[] Files =>
           [.. mFiles];

        public void AddFile(string file, TokenType fileType)
        {
            if (!mFiles.Any(f => f.Item1 == file))
                mFiles.Add(new(file, fileType));
            else
                IO.LogWarning($"File {file} already exists in package {mName}");
        }
    }
}