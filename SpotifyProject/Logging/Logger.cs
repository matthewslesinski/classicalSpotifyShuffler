using System;
using SpotifyProject.Setup;

namespace SpotifyProject
{
    /**
     * Centralization of logging functionality. 
     */
    public class Logger
    {
        public Logger()
        { }

        public static void Information(string msg, params object[] args) => Log(LogLevel.Info, msg, args);
        public static void Warning(string msg, params object[] args) => Log(LogLevel.Warning, msg, args);
        public static void Verbose(string msg, params object[] args) => Log(LogLevel.Verbose, msg, args);
        public static void Error(string msg, params object[] args) => Log(Console.Error.WriteLine, LogLevel.Error, msg, args);

        private static void Log(LogLevel level, string msg, object[] args) => Log(Console.WriteLine, level, msg, args);

        private static void Log(Action<string, object[]> logAction, LogLevel level, string msg, object[] args)
        {
            try
            {
                if (!TemporarilySuppressLogging && level >= _minLogLevel)
                    logAction(msg, args);
            }
            catch(Exception e)
            {
                Panic($"There was an issue with logging using message {msg}, arguments {args}, and action {logAction.Method.Name}: {e}");
            }
        }

        private static void Panic(string message)
        {
            if (!TemporarilySuppressLogging)
                Console.Error.WriteLine(message);
        }

        public static bool TemporarilySuppressLogging { private get; set; }
        public static LogLevel _minLogLevel = GlobalCommandLine.Store.GetOptionValue<LogLevel>(CommandLineOptions.Names.LogLevel);
    }

    public enum LogLevel
	{
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3
	}
}
