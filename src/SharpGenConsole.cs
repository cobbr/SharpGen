// Author: Ryan Cobb (@cobbr_io)
// Project: SharpGen (https://github.com/cobbr/SharpGen)
// License: BSD 3-Clause

using System;

namespace SharpGen
{
    public static class SharpGenConsole
    {
        private static ConsoleColor InfoColor = ConsoleColor.Gray;
        private static ConsoleColor HighlightColor = ConsoleColor.Green;
        private static ConsoleColor ProgressColor = ConsoleColor.Green;
        private static ConsoleColor ErrorColor = ConsoleColor.Red;

        private static string InfoLabel = "[-]";
        private static string HighlightLabel = "[*]";
        private static string ProgressLabel = "[+]";
        private static string ErrorLabel = "[!]";
        private static object _ConsoleLock = new object();

        private static void PrintColor(string ToPrint = "", ConsoleColor color = ConsoleColor.DarkGray)
        {
            lock (_ConsoleLock)
            {
                Console.ForegroundColor = color;
                Console.Write(ToPrint);
                Console.ResetColor();
            }
        }

        private static void PrintColorLine(string ToPrint = "", ConsoleColor color = ConsoleColor.DarkGray)
        {
            lock (_ConsoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(ToPrint);
                Console.ResetColor();
            }
        }

        public static void PrintInfo(string ToPrint = "")
        {
            PrintColor(ToPrint, SharpGenConsole.InfoColor);
        }

        public static void PrintInfoLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, SharpGenConsole.InfoColor);
        }

        public static void PrintFormattedInfo(string ToPrint = "")
        {
            PrintColor(SharpGenConsole.InfoLabel + " " + ToPrint, SharpGenConsole.InfoColor);
        }

        public static void PrintFormattedInfoLine(string ToPrint = "")
        {
            PrintColorLine(SharpGenConsole.InfoLabel + " " + ToPrint, SharpGenConsole.InfoColor);
        }

        public static void PrintHighlight(string ToPrint = "")
        {
            PrintColor(ToPrint, SharpGenConsole.HighlightColor);
        }

        public static void PrintHighlightLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, SharpGenConsole.HighlightColor);
        }

        public static void PrintFormattedHighlight(string ToPrint = "")
        {
            PrintColor(SharpGenConsole.HighlightLabel + " " + ToPrint, SharpGenConsole.HighlightColor);
        }

        public static void PrintFormattedHighlightLine(string ToPrint = "")
        {
            PrintColorLine(SharpGenConsole.HighlightLabel + " " + ToPrint, SharpGenConsole.HighlightColor);
        }

        public static void PrintProgress(string ToPrint = "")
        {
            PrintColor(ToPrint, SharpGenConsole.ProgressColor);
        }

        public static void PrintProgressLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, SharpGenConsole.ProgressColor);
        }

        public static void PrintFormattedProgress(string ToPrint = "")
        {
            PrintColor(SharpGenConsole.ProgressLabel + " " + ToPrint, SharpGenConsole.ProgressColor);
        }

        public static void PrintFormattedProgressLine(string ToPrint = "")
        {
            PrintColorLine(SharpGenConsole.ProgressLabel + " " + ToPrint, SharpGenConsole.ProgressColor);
        }

        public static void PrintError(string ToPrint = "")
        {
            PrintColor(ToPrint, SharpGenConsole.ErrorColor);
        }

        public static void PrintErrorLine(string ToPrint = "")
        {
            PrintColorLine(ToPrint, SharpGenConsole.ErrorColor);
        }

        public static void PrintFormattedError(string ToPrint = "")
        {
            PrintColorLine(SharpGenConsole.ErrorLabel + " " + ToPrint, SharpGenConsole.ErrorColor);
        }

        public static void PrintFormattedErrorLine(string ToPrint = "")
        {
            PrintColorLine(SharpGenConsole.ErrorLabel + " " + ToPrint, SharpGenConsole.ErrorColor);
        }
    }

}
