using Cruncher.Script.Lexer;
using Spectre.Console;
using System.Text;

namespace Cruncher.Util
{
    public static class IO
    {
        private static void TryPrintColoured(string txt)
        {
            try
            {
                AnsiConsole.MarkupLine(txt);
            }
            catch (Exception)
            {
                Console.WriteLine(txt);
            }
        }

        public static void Log(string message)
        {
            TryPrintColoured($"[purple]Info[/]: {message}");
        }

        public static void LogError(string message)
        {
            TryPrintColoured($"[red]Error[/]: {message}");
        }

        public static void LogWarning(string message)
        {
            TryPrintColoured($"[yellow]Warning[/]: {message}");
        }

        public static void LogSuccess(string message)
        {
            TryPrintColoured($"[green]Success[/]: {message}");
        }

        public static void StartUpMessage()
        {
            TryPrintColoured($"[yellow]Cruncher Version[/]: [green]{Version.Current}[/] ([purple]{Version.Current.PackedVersion}[/])");
        }

        public static void TokenError(string message, Token token) =>
            TokenError(message, token.line, token.column);

        public static void TokenError(string message, int line, int pos)
        {
            StringBuilder sb = new();
            sb.AppendLine($"[red]Error:[/] {message}");
            sb.AppendLine($"Occurred at line [aqua]{line + 1}[/], at column [aqua]{pos + 1}[/]");

            TryPrintColoured(sb.ToString());
        }
    }
}