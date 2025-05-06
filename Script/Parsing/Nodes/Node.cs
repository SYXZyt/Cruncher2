using Cruncher.Script.Lexer;

namespace Cruncher.Script.Parsing.Nodes
{
    public class Node(Token token)
    {
        private Token mToken = token;

        public Token Token =>
            mToken;
    }
}