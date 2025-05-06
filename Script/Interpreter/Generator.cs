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

        private void AddFile(string package, string file, TokenType fileType)
        {
            Package pkg = mPackages.FirstOrDefault(p => p.Name == package, null);

            if (!File.Exists(file))
            {
                IO.LogError($"File {Path.GetFullPath(file)} does not exist");
                mErrorOccurred = true;
            }
            else
            {
                //Add the file to the package
                pkg.AddFile(file, fileType);
                IO.LogSuccess($"Added file: [yellow]{file}[/] to package: [yellow]{package}[/]");
            }
        }

        private void AddFolder(string package, string folder)
        {
            Package pkg = mPackages.FirstOrDefault(p => p.Name == package, null);
            if (!Directory.Exists(folder))
            {
                IO.LogError($"Folder {Path.GetFullPath(folder)} does not exist");
                mErrorOccurred = true;
                return;
            }

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
                    IO.LogSuccess($"Added file: [yellow]{file}[/] to package: [yellow]{package}[/]");
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
            if (version.ParamList.Parameters.Length != 3)
            {
                mErrorOccurred = true;
                IO.LogError("Version requirement must have 3 parameters");
            }
            else
            {
                if (version.Major.type != TokenType.NUMBER ||
                    version.Minor.type != TokenType.NUMBER ||
                    version.Patch.type != TokenType.NUMBER)
                {
                    mErrorOccurred = true;
                    IO.LogError("Version requirement must be numbers");
                }
                else
                {
                    Version requested = new(byte.Parse(version.Major.lexeme), byte.Parse(version.Minor.lexeme), byte.Parse(version.Patch.lexeme));
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

                    IO.LogSuccess($"Version requirement: [yellow]{version.Major.lexeme}.{version.Minor.lexeme}.{version.Patch.lexeme}[/]");
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
                    string packageName = addFile.ParamList.Parameters[0].lexeme;
                    if (!mPackages.Any(pkg => pkg.Name == packageName))
                    {
                        mErrorOccurred = true;
                        IO.LogError($"Package with name {packageName} does not exist");
                        continue;
                    }

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

                        AddFile(packageName, param.lexeme, fileType);
                    }
                }
                else if (node is AddFolder addFolder)
                {
                    string packageName = addFolder.ParamList.Parameters[0].lexeme;
                    if (!mPackages.Any(pkg => pkg.Name == packageName))
                    {
                        mErrorOccurred = true;
                        IO.LogError($"Package with name {packageName} does not exist");
                        continue;
                    }

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

                    AddFolder(packageName, param.lexeme);
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