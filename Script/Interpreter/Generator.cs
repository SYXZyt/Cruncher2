using Cruncher.Script.Parsing;
using Cruncher.Script.Lexer;
using Cruncher.Interfaces;
using Cruncher.Script.Parsing.Nodes;
using Cruncher.Util;

namespace Cruncher.Script.Interpreter
{
    public sealed class Generator(Node[] nodes) : IFailable
    {
        private bool mErrorOccurred = false;
        private readonly Node[] mNodes = nodes;

        private readonly Dictionary<string, Token> mAlias = [];
        private readonly List<Package> mPackages = [];

        private Version mVersion = Version.Current;

        public bool DidErrorOccur() =>
            mErrorOccurred;

        private void FillAlias()
        {
            foreach (Node node in mNodes)
            {
                if (node is Alias alias)
                {
                    //Validate that fileExt is either iden or string, and that fileType is one of the FT types
                    if (alias.FileExt.type == TokenType.IDENTIFIER ||
                        alias.FileExt.type == TokenType.STRING)
                    {
                        if (alias.FileType.type == TokenType.FT_TEXT ||
                            alias.FileType.type == TokenType.FT_BINARY)
                        {
                            mAlias.Add(alias.FileExt.lexeme, alias.FileType);

                            IO.LogSuccess($"Registered alias: [yellow]'*.{alias.FileExt.lexeme}'[/] for type [yellow]'{alias.FileType.lexeme}'[/]");
                        }
                        else
                        {
                            IO.TokenError("File type must be one of the following: FT_TEXT, FT_BINARY", node.Token);
                            mErrorOccurred = true;
                        }
                    }
                    else
                        mErrorOccurred = true;
                }
            }
        }

        private Package GetPackage(Token token)
        {
            if (token.type != TokenType.IDENTIFIER && token.type != TokenType.STRING)
            {
                mErrorOccurred = true;
                IO.TokenError("Package name must be either an identifier or a string", token);
                return null;
            }

            string packageName = token.lexeme;
            Package pkg = mPackages.FirstOrDefault(p => p.Name == packageName, null);
            if (pkg == null)
            {
                mErrorOccurred = true;
                IO.LogError($"Package with name {packageName} does not exist");
                return null;
            }

            return pkg;
        }

        private void AddFile(Package pkg, string file, TokenType fileType)
        {
            if (!File.Exists(file))
            {
                IO.LogError($"File {Path.GetFullPath(file)} does not exist");
                mErrorOccurred = true;
            }
            else
            {
                //Add the file to the package
                pkg.AddFile(file, fileType);
                IO.LogSuccess($"Added file: [yellow]{file}[/] to package: [yellow]{pkg.Name}[/]");
            }
        }

        private void AddFolder(Package pkg, string folder)
        {
            if (!Directory.Exists(folder))
            {
                IO.LogError($"Folder {folder} does not exist");
                mErrorOccurred = true;
                return;
            }

            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string fileExt = Path.GetExtension(fileName)[1..^0];
                if (mAlias.TryGetValue(fileExt, out Token fileType))
                {
                    pkg.AddFile(file, fileType.type);
                    IO.LogSuccess($"Added file: [yellow]{file}[/] to package: [yellow]{pkg.Name}[/]");
                }
                else
                {
                    IO.LogError($"No alias found for file type: [yellow]'{fileExt}'[/]");
                    mErrorOccurred = true;
                }
            }
        }

        private void CreatePackages()
        {
            foreach (Node node in mNodes)
            {
                if (node is PackageDef package)
                {
                    //Check if package already exists since they must be unique since a package is a file
                    if (mPackages.Any(pkg => pkg.Name == package.PackageName.lexeme))
                    {
                        mErrorOccurred = true;
                        IO.LogError($"Package with name {package.PackageName.lexeme} already exists");
                    }
                    else
                    {
                        mPackages.Add(new Package(package.PackageName.lexeme));
                        IO.LogSuccess($"Created package: [yellow]{package.PackageName.lexeme}[/]");
                    }
                }
            }
        }

        private static bool IsVersionIncompatible(Version requested, Version current)
        {
            if (requested.major < 2)
                return false;

            return true;
        }

        private void CheckVersion()
        {
            if (mNodes.OfType<RequireVersion>().Skip(1).Any())
            {
                mErrorOccurred = true;
                IO.LogError("Only one version requirement is allowed");
            }

            Node node = mNodes.FirstOrDefault(n => n is RequireVersion, null);
            if (node == null)
            {
                IO.LogWarning("No version requested");
                return;
            }
            RequireVersion version = (RequireVersion)node;

            //Make sure that only numbers were provided
            if (
                (version.ParamList.Parameters.Length != 3) &&
                !(
                    version.ParamList.Parameters.Length == 1 &&
                    version.ParamList.Parameters[0].type == TokenType.IDENTIFIER &&
                    version.ParamList.Parameters[0].lexeme == "latest")
                )
            {
                mErrorOccurred = true;
                IO.LogError("Version requirement must have 3 parameters");
            }
            else
            {
                if (
                    (version.Major.type != TokenType.NUMBER ||
                    version.Minor.type != TokenType.NUMBER ||
                    version.Patch.type != TokenType.NUMBER) &&
                !(
                    version.ParamList.Parameters.Length == 1 &&
                    version.ParamList.Parameters[0].type == TokenType.IDENTIFIER &&
                    version.ParamList.Parameters[0].lexeme == "latest")
                )
                {
                    mErrorOccurred = true;
                    IO.LogError("Version requirement must be numbers");
                }
                else
                {
                    Version requested;

                    if (
                    version.ParamList.Parameters.Length == 1 &&
                    version.ParamList.Parameters[0].type == TokenType.IDENTIFIER &&
                    version.ParamList.Parameters[0].lexeme == "latest")
                    {
                        requested = Version.Current;
                    }
                    else
                        requested = new(byte.Parse(version.Major.lexeme), byte.Parse(version.Minor.lexeme), byte.Parse(version.Patch.lexeme));

                    Version current = Version.Current;
                    if (requested > current)
                    {
                        mErrorOccurred = true;
                        IO.LogError($"Version requirement: [yellow]{version.Major.lexeme}.{version.Minor.lexeme}.{version.Patch.lexeme}[/] is newer than current version: [yellow]{current.major}.{current.minor}.{current.patch}[/]");
                    }

                    if (!IsVersionIncompatible(requested, current))
                    {
                        mErrorOccurred = true;
                        IO.LogError($"Version requirement: [yellow]{version.Major.lexeme}.{version.Minor.lexeme}.{version.Patch.lexeme}[/] is incompatible with current version: [yellow]{current.major}.{current.minor}.{current.patch}[/]");
                        return;
                    }

                    IO.LogSuccess($"Version requirement: [yellow]{requested.major}.{requested.minor}.{requested.patch}[/]");
                    mVersion = requested;
                }
            }

        }

        private bool CheckFailure()
        {
            if (mErrorOccurred)
            {
                IO.LogError("Generation failed");
                return true;
            }

            return false;
        }

        public Package[] Generate()
        {
            IO.Log("Generating");
            CheckVersion();
            if (CheckFailure())
                return null;

            FillAlias();
            if (CheckFailure())
                return null;

            CreatePackages();
            if (CheckFailure())
                return null;

            foreach (Node node in mNodes)
            {
                if (node is AddFile addFile)
                {
                    Package package = GetPackage(addFile.ParamList.Parameters[0]);

                    TokenType fileType = addFile.ParamList.Parameters[1].type;

                    if (addFile.ParamList.Parameters.Length < 2)
                    {
                        mErrorOccurred = true;
                        IO.LogError("File name must be specified");
                        continue;
                    }

                    for (int i = 2; i < addFile.ParamList.Parameters.Length; ++i)
                    {
                        ref Token param = ref addFile.ParamList.Parameters[i];

                        if (param.type is not TokenType.IDENTIFIER and not TokenType.STRING)
                        {
                            mErrorOccurred = true;
                            IO.TokenError("File name must be either an identifier or a string", param);
                            continue;
                        }

                        string fileExt = Path.GetExtension(param.lexeme)[1..^0];

                        AddFile(package, param.lexeme, fileType);
                    }
                }
                else if (node is AddFolder addFolder)
                {
                    Package package = GetPackage(addFolder.ParamList.Parameters[0]);

                    if (addFolder.ParamList.Parameters.Length < 2)
                    {
                        mErrorOccurred = true;
                        IO.LogError("Folder name must be specified");
                        continue;
                    }

                    ref Token param = ref addFolder.ParamList.Parameters[1];
                    if (param.type is not TokenType.IDENTIFIER and not TokenType.STRING)
                    {
                        mErrorOccurred = true;
                        IO.TokenError("Folder name must be either an identifier or a string", param);
                        continue;
                    }

                    AddFolder(package, param.lexeme);
                }
                else if (node is OutputExt outputExt)
                {
                    Version required = new(2, 1, 0);
                    if (mVersion < required)
                    {
                        mErrorOccurred = true;
                        IO.LogError($"output_extension requires version: [yellow]{required.major}.{required.minor}.{required.patch}[/]");
                        continue;
                    }

                    if (outputExt.Params.Parameters.Length != 2)
                    {
                        mErrorOccurred = true;
                        IO.LogError("output_extension must have 2 parameters");
                        continue;
                    }

                    Package package = GetPackage(outputExt.Params.Parameters[0]);
                    Token ext = outputExt.Params.Parameters[1];
                    if (ext.type is not TokenType.IDENTIFIER or TokenType.STRING)
                    {
                        mErrorOccurred = true;
                        IO.TokenError("File extension must be either an identifier or a string", ext);
                        continue;
                    }

                    if (ext.lexeme.StartsWith('.'))
                    {
                        mErrorOccurred = true;
                        IO.LogError("Do not include the dot");
                        continue;
                    }

                    if (ext.lexeme.Length == 0)
                    {
                        mErrorOccurred = true;
                        IO.LogError("File extension must not be empty");
                        continue;
                    }

                    if (package.Extension is not null)
                        IO.LogWarning("File extension has already been defined for package '[yellow][/]'");

                    package.Extension = ext.lexeme;
                    IO.LogSuccess($"Set file extension: [yellow]'.{ext.lexeme}'[/] for package: [yellow]{package.Name}[/]");
                }
                else if (node is RequireVersion or Alias or PackageDef) { }
                else
                {
                    mErrorOccurred = true;
                    IO.LogError($"Unknown node: [yellow]'{node.Token.lexeme}'[/]");
                }
            }

            return [.. mPackages];
        }
    }
}