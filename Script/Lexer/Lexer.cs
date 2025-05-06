using Cruncher.Interfaces;
using Cruncher.Util;
using System.Text;

namespace Cruncher.Script.Lexer
{
    public sealed class Lexer : IFailable
    {
        private static readonly Dictionary<string, TokenType> keywords = null;

        private readonly string mData;
        private int mPos;
        private int mPosOnLine;
        private int mLine;
        private bool mErrorOccurred;

        public bool DidErrorOccur() =>
            mErrorOccurred;

        private char CurrentChar
        {
            get
            {
                if (mPos >= mData.Length)
                    return '\0';

                return mData[mPos];
            }
        }

        private void Advance(int amount = 1)
        {
            mPos += amount;
            mPosOnLine += amount;

            if (CurrentChar == '\n')
            {
                ++mLine;
                mPosOnLine = -1;
            }
        }

        private void SkipLine()
        {
            while (CurrentChar != '\n' && CurrentChar != '\0')
                Advance();
        }

        public Token[] Tokenise()
        {
            List<Token> tokens = [];

            while (CurrentChar != '\0')
            {
                //Whitespace characters do not mean anything, so skip them
                if (char.IsWhiteSpace(CurrentChar))
                {
                    Advance();
                    continue;
                }

                //We have to check for numbers before identifiers
                //Otherwise, the lexer will think that a number is an identifier
                //This is why in most languages, an identifier cannot start with a number, but can contain them
                if (char.IsDigit(CurrentChar))
                {
                    StringBuilder number = new();

                    do
                    {
                        number.Append(CurrentChar);
                        Advance();
                    } while (char.IsDigit(CurrentChar));

                    tokens.Add(new Token(TokenType.NUMBER, number.ToString(), mLine, mPosOnLine));
                }
                else if (char.IsLetterOrDigit(CurrentChar))
                {
                    StringBuilder identifier = new();

                    do
                    {
                        identifier.Append(CurrentChar);
                        Advance();
                    } while (CurrentChar.IsIdentifier());

                    TokenType tokenType = keywords.ContainsKey(identifier.ToString())
                        ? keywords[identifier.ToString()]
                        : TokenType.IDENTIFIER;

                    tokens.Add(new Token(tokenType, identifier.ToString(), mLine, mPosOnLine));
                }
                else if (CurrentChar == '"')
                {
                    Advance();

                    StringBuilder str = new();
                    while (CurrentChar != '"')
                    {
                        if (CurrentChar is '\0' or '\n')
                        {
                            IO.TokenError("Unterminated string", mLine, mPosOnLine);
                            SkipLine();
                            mErrorOccurred = true;
                            break;
                        }

                        str.Append(CurrentChar);
                        Advance();
                    }

                    Advance();
                    tokens.Add(new Token(TokenType.STRING, str.ToString(), mLine, mPosOnLine));
                }

                else if (CurrentChar == '(')
                {
                    tokens.Add(new Token(TokenType.L_PAREN, "(", mLine, mPosOnLine));
                    Advance();
                }
                else if (CurrentChar == ')')
                {
                    tokens.Add(new Token(TokenType.R_PAREN, ")", mLine, mPosOnLine));
                    Advance();
                }
                else if (CurrentChar == ',')
                {
                    tokens.Add(new Token(TokenType.COMMA, ",", mLine, mPosOnLine));
                    Advance();
                }
                else
                {
                    IO.TokenError($"Unexpected character '{CurrentChar}'", mLine, mPosOnLine);
                    SkipLine();
                    mErrorOccurred = true;
                    Advance();
                }
            }

            return [.. tokens];
        }

        public Lexer(string data)
        {
            mData = data;
            mPos = -1;
            mPosOnLine = -1;
            mLine = 0;
            mErrorOccurred = false;

            Advance();
        }

        static Lexer()
        {
            keywords = [];

            keywords["required_version"] = TokenType.REQUIRE_VERSION;
            keywords["alias"] = TokenType.ALIAS;
            keywords["add_package"] = TokenType.ADD_PACKAGE;
            keywords["add_file"] = TokenType.ADD_FILE;
            keywords["add_folder"] = TokenType.ADD_FOLDER;

            //File types
            keywords["FileType_TEXT"] = TokenType.FT_TEXT;
            keywords["FileType_BIN"] = TokenType.FT_BINARY;
        }
    }
}