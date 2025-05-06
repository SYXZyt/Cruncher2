/*
    Cruncher is a tool for packing together multiple files into a single file
    We use a custom script format for describing the files to be packed
    This should always be called `crunch.txt`. For the args, a directory is passed which stores the `crunch.txt` file
    If no directory is passed, the current directory is used
 */

using Cruncher.Script.Interpreter;
using Cruncher.Script.Lexer;
using Cruncher.Script.Packing;
using Cruncher.Script.Parsing;
using Cruncher.Script.Parsing.Nodes;
using Cruncher.Util;

namespace Cruncher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //If more than 1 argument is passed, then the user
            //has made a mistake and we should warn them of this
            if (args.Length > 1)
            {
                IO.LogWarning("Too many arguments passed. Only a directory should be passed.");
                return;
            }

            string directory = args.Length == 0 ? Directory.GetCurrentDirectory() : args[0];

            //Validate that the crunch.txt file exists
            //If it doesn't, then there nothing we can do to continue
            //So report the error to the user
            string crunchFilePath = Path.Combine(directory, "crunch.txt");
            if (!File.Exists(crunchFilePath))
            {
                IO.LogError($"The file {crunchFilePath} does not exist.");
                return;
            }

            IO.StartUpMessage();

            Lexer lexer = new(File.ReadAllText(crunchFilePath));
            Token[] tokens = lexer.Tokenise();

            if (lexer.DidErrorOccur())
                return;

            Parser parser = new(tokens);
            Node[] nodes = parser.Parse();

            if (parser.DidErrorOccur())
                return;

            Generator generator = new(nodes);
            Package[] packages = generator.Generate();

            if (generator.DidErrorOccur())
                return;

            Packer packer = new(packages);
            packer.PackPackages();
        }
    }
}