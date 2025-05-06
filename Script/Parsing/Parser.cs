using Cruncher.Interfaces;
using Cruncher.Script.Lexer;
using Cruncher.Script.Parsing.Nodes;
using Cruncher.Util;

namespace Cruncher.Script.Parsing
{
    internal sealed class Parser(Token[] tokens) : IFailable
    {
        private readonly Token[] mTokens = tokens;
        private int mCurrent = 0;
        private bool mErrorOccurred = false;

        public bool DidErrorOccur() =>
            mErrorOccurred;

        private Token Current =>
            mTokens[mCurrent];

        private bool IsEof =>
            mCurrent >= mTokens.Length;

        private ParamList ParseParamList()
        {
            Token function = Current;
            List<Token> parameters = [];

            //Skip the opening parenthesis
            ++mCurrent;

            while (Current.type != TokenType.R_PAREN && !IsEof)
            {
                if (Current.type is
                    TokenType.IDENTIFIER or
                    TokenType.NUMBER or
                    TokenType.STRING or
                    TokenType.FT_TEXT or
                    TokenType.FT_BINARY)
                {
                    parameters.Add(Current);
                    ++mCurrent;
                }
                else if (Current.type == TokenType.COMMA)
                {
                    ++mCurrent;
                }
                else
                {
                    IO.TokenError("Expected parameter or comma", Current);
                    mErrorOccurred = true;
                    return null;
                }
            }
            if (IsEof)
            {
                IO.TokenError("Expected closing parenthesis", function);
                mErrorOccurred = true;
                return null;
            }

            ++mCurrent;
            return new ParamList(function, [.. parameters]);
        }

        private RequireVersion ParseVersionRequirement()
        {
            Token function = Current;
            ++mCurrent;

            ParamList paramList = ParseParamList();
            if (paramList is null)
                return null;

            return new RequireVersion(function, paramList);
        }

        private Alias ParseAlias()
        {
            Token function = Current;
            ++mCurrent;

            ParamList paramList = ParseParamList();
            if (paramList is null)
                return null;

            return new Alias(function, paramList);
        }

        private PackageDef ParsePackageDef()
        {
            Token function = Current;
            ++mCurrent;

            ParamList paramList = ParseParamList();
            if (paramList is null)
                return null;

            return new PackageDef(function, paramList);
        }

        private AddFile ParseAddFile()
        {
            Token function = Current;
            ++mCurrent;

            ParamList paramList = ParseParamList();
            if (paramList is null)
                return null;

            return new AddFile(function, paramList);
        }

        private AddFolder ParseAddFolder()
        {
            Token function = Current;
            ++mCurrent;

            ParamList paramList = ParseParamList();
            if (paramList is null)
                return null;

            return new AddFolder(function, paramList);
        }

        private OutputExt ParseOutputExtension()
        {
            Token function = Current;
            ++mCurrent;

            ParamList paramList = ParseParamList();
            if (paramList is null)
                return null;

            if (paramList.Parameters.Length != 2)
            {
                IO.TokenError("Expected two parameters for output_extension", function);
                mErrorOccurred = true;
                return null;
            }

            return new OutputExt(function, paramList);
        }

        private Node ParseFunctionCall()
        {
            if (Current.type == TokenType.REQUIRE_VERSION)
                return ParseVersionRequirement();
            else if (Current.type == TokenType.ALIAS)
                return ParseAlias();
            else if (Current.type == TokenType.ADD_PACKAGE)
                return ParsePackageDef();
            else if (Current.type == TokenType.ADD_FILE)
                return ParseAddFile();
            else if (Current.type == TokenType.ADD_FOLDER)
                return ParseAddFolder();
            else if (Current.type == TokenType.OUTPUT_EXTENSION)
                return ParseOutputExtension();

            string msg;
            if (Current.type == TokenType.IDENTIFIER)
                msg = $"Unknown function [yellow]'{Current.lexeme}'[/]";
            else
                msg = $"Expected function call. Got [yellow]\"{Current.lexeme}\"[/]";

            IO.TokenError(msg, Current);
            mErrorOccurred = true;
            ++mCurrent;
            return null;
        }

        public Node[] Parse()
        {
            List<Node> nodes = [];

            while (!IsEof)
                nodes.Add(ParseFunctionCall());

            return [.. nodes];
        }
    }
}