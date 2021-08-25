using System;
using Microsoft.Extensions.Logging;
using SpotifyProject.Setup;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;
namespace SpotifyProject
{
    /**
     * Centralization of logging functionality. 
     */
    public static class Logger
    {
        public static void Information(string msg, params object[] args) => Loggers.GeneralLogger.Log(LogLevel.Info, msg, args);
        public static void Warning(string msg, params object[] args) => Loggers.GeneralLogger.Log(LogLevel.Warning, msg, args);
        public static void Verbose(string msg, params object[] args) => Loggers.GeneralLogger.Log(LogLevel.Verbose, msg, args);
        public static void Error(string msg, params object[] args) => Loggers.GeneralLogger.Log(LogLevel.Error, msg, args);
        public static void Log(LogLevel level, string msg, params object[] args) => Loggers.GeneralLogger.Log(level, msg, args);

        public static void Information(this LoggerWrapper logger, string msg, params object[] args) => logger.Log(LogLevel.Info, msg, args);
        public static void Warning(this LoggerWrapper logger, string msg, params object[] args) => logger.Log(LogLevel.Warning, msg, args);
        public static void Verbose(this LoggerWrapper logger, string msg, params object[] args) => logger.Log(LogLevel.Verbose, msg, args);
        public static void Error(this LoggerWrapper logger, string msg, params object[] args) => logger.Log(LogLevel.Error, msg, args);

        private static void Log(this LoggerWrapper logger, LogLevel level, string msg, object[] args)
        {
            try
            {
                logger.Log(LogLevelMappings[(int) level], msg, args);
            }
            catch(Exception e)
            {
                Panic($"There was an issue with logging using message {msg}, arguments {args}, logLevel {level}, and logger {logger.Name}: {e}");
            }
        }

        private static void Panic(string message)
        {
            Console.Error.WriteLine(message);
        }

        public static readonly MicrosoftLogLevel[] LogLevelMappings = new MicrosoftLogLevel[]
        {
            MicrosoftLogLevel.Trace,
            MicrosoftLogLevel.Information,
            MicrosoftLogLevel.Warning,
            MicrosoftLogLevel.Error
        };

        private const string _logDirectoryParentKeyName = "logDirectoryParent";
        private const string _logFileNameKeyName = "logFileName";
        private const string _consoleMinLogLevelKeyName = "consoleMinLogLevel";
        private const string _logFileMinLogLevelKeyName = "logFileMinLogLevel";

        static Logger()
		{
            var minConsoleLogLevel = LogLevelMappings[(int) Settings.Get<LogLevel>(SettingsName.ConsoleLogLevel)];
            var minFileLogLevel = LogLevelMappings[(int) Settings.Get<LogLevel>(SettingsName.OutputFileLogLevel)];
            var logDirectoryParent = Settings.Get<string>(SettingsName.SpotifyProjectRootDirectory);
            var logFileName = Settings.Get<string>(SettingsName.LogFileName);
            NLog.GlobalDiagnosticsContext.Set(_logDirectoryParentKeyName, logDirectoryParent);
            NLog.GlobalDiagnosticsContext.Set(_logFileNameKeyName, logFileName);
            NLog.GlobalDiagnosticsContext.Set(_consoleMinLogLevelKeyName, minConsoleLogLevel);
            NLog.GlobalDiagnosticsContext.Set(_logFileMinLogLevelKeyName, minFileLogLevel);
        }
    }

    public static class Loggers
	{
        public static readonly LoggerWrapper GeneralLogger;
        public static readonly LoggerWrapper HTTPLogger;


        private static LoggerWrapper CreateLogger<T>() => CreateLogger(typeof(T).FullName);
        private static LoggerWrapper CreateLogger(string name) => new LoggerWrapper(name, _nlogLoggerProvider.CreateLogger(name));
        private static readonly ILoggerProvider _nlogLoggerProvider;
        static Loggers()
		{
            _nlogLoggerProvider = new NLog.Extensions.Logging.NLogLoggerProvider();
            GeneralLogger = CreateLogger("GeneralLogger");
            HTTPLogger = CreateLogger("HTTPLogger");
        }
	}

    public class LoggerWrapper : ILogger
	{
        private readonly string _name;
        private readonly ILogger _wrappedLogger;
        internal LoggerWrapper(string name, ILogger wrappedLogger)
		{
            _name = name;
            _wrappedLogger = wrappedLogger;
		}

        public IDisposable BeginScope<TState>(TState state) => _wrappedLogger.BeginScope(state);

        public bool IsEnabled(MicrosoftLogLevel logLevel) => _wrappedLogger.IsEnabled(logLevel);

        public void Log<TState>(MicrosoftLogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            _wrappedLogger.Log(logLevel, eventId, state, exception, formatter);
        public string Name => _name;
	}


	public enum LogLevel
	{
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3
	}
}
