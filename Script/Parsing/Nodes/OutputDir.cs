using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    public sealed class OutputDir(Token token, ParamList paramList) : Node(token)
    {
        private readonly ParamList mParamList = paramList;

        public ParamList ParamList =>
            mParamList;
    }
}