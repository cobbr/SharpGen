// Author: Ryan Cobb (@cobbr_io)
// Project: SharpGen (https://github.com/cobbr/SharpGen)
// License: BSD 3-Clause

using System;
using Confuser.Core;

namespace SharpGen
{
    class ConfuserConsoleLogger : ILogger
    {
        readonly DateTime begin;

        public ConfuserConsoleLogger()
        {
            begin = DateTime.Now;
        }

        public int ReturnValue { get; private set; }

        static void WriteLineWithColor(ConsoleColor color, string text)
        {
            ConsoleColor original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = original;
        }

        public void Debug(string msg)
        {
            WriteLineWithColor(ConsoleColor.Gray, "[DEBUG] " + msg);
        }

        public void DebugFormat(string format, params object[] args)
        {
            WriteLineWithColor(ConsoleColor.Gray, "[DEBUG] " + string.Format(format, args));
        }

        public void Info(string msg)
        {
            WriteLineWithColor(ConsoleColor.White, " [INFO] " + msg);
        }

        public void InfoFormat(string format, params object[] args)
        {
            WriteLineWithColor(ConsoleColor.White, " [INFO] " + string.Format(format, args));
        }

        public void Warn(string msg)
        {
            WriteLineWithColor(ConsoleColor.Yellow, " [WARN] " + msg);
        }

        public void WarnFormat(string format, params object[] args)
        {
            WriteLineWithColor(ConsoleColor.Yellow, " [WARN] " + string.Format(format, args));
        }

        public void WarnException(string msg, Exception ex)
        {
            WriteLineWithColor(ConsoleColor.Yellow, " [WARN] " + msg);
            WriteLineWithColor(ConsoleColor.Yellow, "Exception: " + ex);
        }

        public void Error(string msg)
        {
            WriteLineWithColor(ConsoleColor.Red, "[ERROR] " + msg);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            WriteLineWithColor(ConsoleColor.Red, "[ERROR] " + string.Format(format, args));
        }

        public void ErrorException(string msg, Exception ex)
        {
            WriteLineWithColor(ConsoleColor.Red, "[ERROR] " + msg);
            WriteLineWithColor(ConsoleColor.Red, "Exception: " + ex);
        }

        public void Progress(int progress, int overall) { }

        public void EndProgress() { }

        public void Finish(bool successful)
        {
            DateTime now = DateTime.Now;
            string timeString = string.Format(
                "at {0}, {1}:{2:d2} elapsed.",
                now.ToShortTimeString(),
                (int)now.Subtract(begin).TotalMinutes,
                now.Subtract(begin).Seconds);
            if (successful)
            {
                Console.Title = "ConfuserEx - Success";
                WriteLineWithColor(ConsoleColor.Green, "Finished " + timeString);
                ReturnValue = 0;
            }
            else
            {
                Console.Title = "ConfuserEx - Fail";
                WriteLineWithColor(ConsoleColor.Red, "Failed " + timeString);
                ReturnValue = 1;
            }
        }
    }
}
