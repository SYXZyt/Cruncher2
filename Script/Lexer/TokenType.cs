namespace Cruncher.Script.Lexer
{
    public enum TokenType : byte
    {
        EOF,

        IDENTIFIER,
        NUMBER,
        STRING,
        BOOL,

        L_PAREN,
        R_PAREN,

        COMMA,

        //File Type
        FT_TEXT,
        FT_BINARY,

        //Functions

        #region 2.0.0
        REQUIRE_VERSION,
        ALIAS,
        ADD_PACKAGE,
        ADD_FILE,
        ADD_FOLDER,
        #endregion

        #region 2.1.0
        OUTPUT_EXTENSION,
        OUTPUT_DIR,
        #endregion
    public static class TokenTypeExtensions
    {
        public static bool IsFileType(this TokenType type) =>
            type == TokenType.FT_TEXT || type == TokenType.FT_BINARY;
    }
}