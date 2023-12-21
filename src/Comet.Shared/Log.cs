// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Shared - Log.cs
// Description:
// 
// Creator: FELIPEVIEIRAVENDRAMI [FELIPE VIEIRA VENDRAMINI]
// 
// Developed by:
// Felipe Vieira Vendramini <felipevendramini@live.com>
// 
// Programming today is a race between software engineers striving to build bigger and better
// idiot-proof programs, and the Universe trying to produce bigger and better idiots.
// So far, the Universe is winning.
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace Comet.Shared
{
    public enum LogLevel
    {
        Info,
        Debug,
        Warning,
        Error,
        Exception,
        Deadloop,
        Cheat,
        Action,
        Socket
    }

    internal enum LogFolder
    {
        SystemLog,
        GameLog
    }

    internal struct LogFile
    {
        public LogFolder Folder;
        public string Path;
        public string Filename;
        public DateTime Date;
    }

    public static class Log
    {
        private static readonly Dictionary<string, LogFile> Files = new Dictionary<string, LogFile>();

        public static string DefaultFileName = "Server";

        static Log()
        {
            RefreshFolders();
        }

        public static async Task WriteLogAsync(LogLevel level, string message, params object[] values)
        {
            await WriteLogAsync(DefaultFileName, level, message, values);
        }

        public static async Task WriteLogAsync(string file, LogLevel level, string message, params object[] values)
        {
            RefreshFolders();

            if (level == LogLevel.Action)
                file = "GameAction";

            message = string.Format(message, values);
            message = $"{DateTime.Now:HH:mm:ss.fff} [{level,-10}] - {message}";

            await WriteToFile(file, LogFolder.SystemLog, message);

            if (level != LogLevel.Action)
            {
                switch (level)
                {
                    case LogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Exception:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                }

                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static async Task GmLogAsync(string file, string message, params object[] values)
        {
            RefreshFolders();

            if (values.Length > 0)
                message = string.Format(message, values);

            message = $"{DateTime.Now:HHmmss.fff} - {message}";

            await WriteToFile(file, LogFolder.GameLog, message);
        }

        private static async Task WriteToFile(string file, LogFolder eFolder, string value)
        {
            var now = DateTime.Now;
            if (!Files.TryGetValue(file, out var fileHandle))
                Files.Add(file, fileHandle = CreateHandle(file, eFolder));

            if (fileHandle.Date.Year != now.Year
                || fileHandle.Date.DayOfYear != now.DayOfYear)
            {
                fileHandle.Date = now;
                fileHandle.Path = $"{GetDirectory(eFolder)}{Path.DirectorySeparatorChar}{file}.log";
            }

            try
            {
                await using var fWriter = new FileStream(fileHandle.Path, FileMode.Append, FileAccess.Write,
                    FileShare.Write, 4096);
                await using var writer = new StreamWriter(fWriter);
                await writer.WriteLineAsync(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static LogFile CreateHandle(string file, LogFolder folder)
        {
            if (Files.ContainsKey(file))
                return Files[file];

            var now = DateTime.Now;
            var logFile = new LogFile
            {
                Date = now,
                Filename = $"{now:YYYYMMdd)} - {file}.log",
                Path = $"{GetDirectory(folder)}{Path.DirectorySeparatorChar}{file}.log",
                Folder = folder
            };
            return logFile;
        }

        private static string GetDirectory(LogFolder folder)
        {
            var now = DateTime.Now;
            return string.Join(Path.DirectorySeparatorChar.ToString(), ".", $"{folder}", $"{now.Year:0000}",
                $"{now.Month:00}", $"{now.Day:00}");
        }

        private static void RefreshFolders()
        {
            foreach (var eVal in Enum.GetValues(typeof(LogFolder)).Cast<LogFolder>())
                if (!Directory.Exists(GetDirectory(eVal)))
                    Directory.CreateDirectory(GetDirectory(eVal));
        }
    }
}