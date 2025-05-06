using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    internal sealed class RequireVersion(Token token, ParamList paramList) : Node(token)
    {
        private Token mMajor = paramList[0];
        private Token mMinor = paramList[1];
        private Token mPatch = paramList[2];
        private ParamList mParamList = paramList;

        public Token Major =>
            mMajor;

        public Token Minor =>
            mMinor;

        public Token Patch =>
            mPatch;

        public ParamList ParamList =>
            mParamList;
    }
}