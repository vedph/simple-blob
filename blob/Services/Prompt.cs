using System;

namespace SimpleBlob.Cli.Services
{
    public static class Prompt
    {
        public static char ForChar(string message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            ColorConsole.WriteEmbeddedColorLine(message);
            char c = char.ToLowerInvariant(Console.ReadKey(true).KeyChar);
            Console.WriteLine();
            return c;
        }

        public static bool ForBool(string message, bool defValue = false)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            ColorConsole.WriteWarning(
                message + (defValue ? " (Y/n)?" : " (y/N)?"));
            bool result = defValue;
            ConsoleKeyInfo info = Console.ReadKey();
            if (info.Key == ConsoleKey.Enter || info.Key == ConsoleKey.Escape)
            {
                Console.WriteLine(defValue ? 'y' : 'n');
                return defValue;
            }

            switch (char.ToLowerInvariant(info.KeyChar))
            {
                case 'y':
                    result = true;
                    break;
                case 'n':
                    result = false;
                    break;
                default:
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.WriteLine(defValue ? 'y' : 'n');
                    break;
            }
            Console.WriteLine();

            return result;
        }
    }
}
