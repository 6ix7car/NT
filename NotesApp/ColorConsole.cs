using System;

namespace Launcher
{
    public static class ColorConsole
    {
        public static void WriteError(string msg) => WriteColored(msg, ConsoleColor.Red);
        public static void WriteLineError(string msg) => WriteColored(msg + "\n", ConsoleColor.Red);
        public static void WriteSuccess(string msg) => WriteColored(msg, ConsoleColor.Green);
        public static void WriteLineSuccess(string msg) => WriteColored(msg + "\n", ConsoleColor.Green);
        public static void WriteWarning(string msg) => WriteColored(msg, ConsoleColor.Yellow);
        public static void WriteLineWarning(string msg) => WriteColored(msg + "\n", ConsoleColor.Yellow);
        public static void WriteInfo(string msg) => WriteColored(msg, ConsoleColor.White);
        public static void WriteLineInfo(string msg) => WriteColored(msg + "\n", ConsoleColor.White);

        private static void WriteColored(string msg, ConsoleColor color)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(msg);
            Console.ForegroundColor = old;
        }
    }
}