using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    public sealed class Reject(Token token, ParamList paramList) : Node(token)
    {
        private readonly ParamList mParamList = paramList;
        public ParamList ParamList =>
            mParamList;
    }

    public sealed class RejectFolder(Token token, ParamList paramList) : Node(token)
    {
        private readonly ParamList mParamList = paramList;
        public ParamList ParamList =>
            mParamList;
    }

    public sealed class RejectExtension(Token token, ParamList paramList) : Node(token)
    {
        private readonly ParamList mParamList = paramList;
        public ParamList ParamList =>
            mParamList;
    }
}
