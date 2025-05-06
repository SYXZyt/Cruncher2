using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    public sealed class Alias(Token token, ParamList list) : Node(token)
    {
        private readonly Token mFileExt = list[0];
        private readonly Token mFileType = list[1];

        private readonly ParamList mParamList = list;

        public Token FileExt =>
            mFileExt;

        public Token FileType =>
            mFileType;

        public ParamList ParamList =>
            mParamList;
    }
}