using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    public sealed class ParamList(Token token, Token[] parameters) : Node(token)
    {
        private readonly Token[] mParams = parameters;

        public Token this[int key]
        {
            get
            {
                if (key < 0 || key >= mParams.Length)
                    return Token.Null;

                return mParams[key];
            }
        }

        public Token[] Parameters =>
            mParams;
    }
}