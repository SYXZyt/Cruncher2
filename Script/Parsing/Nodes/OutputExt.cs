using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    public class OutputExt(Token token, ParamList paramList) : Node(token)
    {
        private readonly ParamList mParamList = paramList;

        public ParamList Params =>
            mParamList;
    }
}