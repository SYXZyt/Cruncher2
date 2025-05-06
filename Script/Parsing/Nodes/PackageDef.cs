using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    public sealed class PackageDef(Token token, ParamList paramList) : Node(token)
    {
        private readonly Token mPkgName = paramList[0];
        private readonly ParamList mParamList = paramList;

        public Token PackageName =>
            mPkgName;

        public ParamList ParamList =>
            mParamList;
    }
}