using System;
using Microsoft.Extensions.Logging;
using ApplicationResources.Setup;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.IO;
using System.Threading.Tasks;
using Util = CustomResources.Utils.GeneralUtils.Utils;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using ApplicationResources.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Xml;
using System.Threading;

namespace ApplicationResources.Logging
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
                logger.Log(LogLevelMappingContainer.LevelMapping.Invoke(level), msg, args);
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

        private const string _logDirectoryParentKeyName = "logDirectoryParent";
        private const string _logFileNameKeyName = "logFileName";
        private const string _consoleMinLogLevelKeyName = "consoleMinLogLevel";
        private const string _logFileMinLogLevelKeyName = "logFileMinLogLevel";
        private const string _methodCallMinLogLevelKeyName = "methodCallMinLogLevel";

        static Logger()
		{
            var minConsoleLogLevel = LogLevelMappingContainer.LevelMapping.Invoke(Settings.Get<LogLevel>(BasicSettings.ConsoleLogLevel));
            var minFileLogLevel = LogLevelMappingContainer.LevelMapping.Invoke(Settings.Get<LogLevel>(BasicSettings.OutputFileLogLevel));
            var logDirectoryParent = Settings.Get<string>(BasicSettings.ProjectRootDirectory);
            var logFileName = Settings.Get<string>(BasicSettings.LogFileName);
            var minMethodCallLogLevel = LogLevelMappingContainer.LevelMapping.Invoke(Settings.Get<LogLevel>(BasicSettings.MethodCallLogLevel));

            NLog.GlobalDiagnosticsContext.Set(_logDirectoryParentKeyName, logDirectoryParent);
            NLog.GlobalDiagnosticsContext.Set(_logFileNameKeyName, logFileName);
            NLog.GlobalDiagnosticsContext.Set(_consoleMinLogLevelKeyName, minConsoleLogLevel);
            NLog.GlobalDiagnosticsContext.Set(_logFileMinLogLevelKeyName, minFileLogLevel);
            NLog.GlobalDiagnosticsContext.Set(_methodCallMinLogLevelKeyName, minMethodCallLogLevel);
        }
    }

    public static class LoggerConfigurationProvider
	{
        private static readonly MutableReference<bool> _isReady = new(false);
        private static readonly AsyncLockProvider _lock = new();

        public static Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            return Util.LoadOnceBlockingAsync(_isReady, _lock, async (cancellationToken) =>
            {
                if (Settings.TryGet<string>(BasicSettings.LoggerConfigurationFile, out var configFileLocation))
                {
                    var logDirectoryParent = Settings.Get<string>(BasicSettings.ProjectRootDirectory);
                    var filepath = Path.Combine(logDirectoryParent, configFileLocation);
                    var dataStoreAccessor = GlobalDependencies.Get<IDataStoreAccessor>();
                    if (await dataStoreAccessor.ExistsAsync(filepath, cancellationToken).WithoutContextCapture())
                    {
                        var fileContents = await dataStoreAccessor.GetAsync(filepath, cancellationToken).WithoutContextCapture();
                        var reader = XmlReader.Create(new StringReader(fileContents));
                        NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(reader, filepath);
                    }
                    else
                        throw new ArgumentException($"The filepath for the NLog configuration does not exist {filepath}");
                } 
            }, cancellationToken);
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

    public static class LoggerTargetProvider
    {
        public record struct LogActionArgs(string FullMessage, string BareMessage, LogLevel Level, string LoggerName);

        public static event Action<LogActionArgs> OnLog;

        public static void LogAction(string fullMessage, string bareMessage, string levelString, string loggerName)
        {
            if (OnLog != null)
            {
                var logLevel = Enum.TryParse<LogLevel>(levelString, out var parsedLogLevel)
                    ? parsedLogLevel
                    : LogLevelMappingContainer.LevelMapping.InvokeInverse(Enum.Parse<MicrosoftLogLevel>(levelString));
                OnLog(new(fullMessage, bareMessage, logLevel, loggerName));
            }
        }
    }

    public enum LogLevel
	{
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3
	}

    internal static class LogLevelMappingContainer
	{
        internal static readonly BijectiveDictionary<LogLevel, MicrosoftLogLevel> LevelMapping = new BijectiveDictionary<LogLevel, MicrosoftLogLevel>
        {
            { LogLevel.Verbose, MicrosoftLogLevel.Trace },
            { LogLevel.Info, MicrosoftLogLevel.Information },
            { LogLevel.Warning, MicrosoftLogLevel.Warning },
            { LogLevel.Error, MicrosoftLogLevel.Error }
        };
	}
}
