namespace Cruncher.Script.Lexer
{
    public struct Token(
        TokenType type,
        string lexeme,
        int line,
        int column
    )
    {
        public string lexeme = lexeme;
        public int line = line;
        public int column = column;
        public TokenType type = type;

        public static Token Null =>
            new(TokenType.EOF, "", -1, -1);

        public readonly override string ToString()
        {
            return $"[Token: {type} {lexeme} (line: {line}, column: {column})]";
        }
    }
}