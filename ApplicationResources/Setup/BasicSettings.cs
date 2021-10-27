using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApplicationResources.Logging;
using CustomResources.Utils.GeneralUtils;
using static ApplicationResources.Setup.CommandLineOptions;

namespace ApplicationResources.Setup
{
	[EnumExtensionProvider(typeof(SettingsSpecifications))]
	[EnumExtensionProvider(typeof(SettingsCommandLineSpecifications))]
	public enum BasicSettings
	{
		ConsoleLogLevel,
		OutputFileLogLevel,
		LogFileName,
		LoggerConfigurationFile,
		RandomSeed,
		SupplyUserInput,
		ProjectRootDirectory,
	}
	public class SettingsSpecifications : IEnumExtensionProvider<BasicSettings, ISettingSpecification>
	{
		public IReadOnlyDictionary<BasicSettings, ISettingSpecification> Specifications { get; } = new Dictionary<BasicSettings, ISettingSpecification>
		{
			{ BasicSettings.LogFileName,                       new StringSettingSpecification() },
			{ BasicSettings.LoggerConfigurationFile,           new StringSettingSpecification() },
			{ BasicSettings.RandomSeed,                        new ConvertibleSettingSpecification<int>() },
			{ BasicSettings.SupplyUserInput,                   new MultipleStringsSettingSpecification() },
			{ BasicSettings.ConsoleLogLevel,                   new EnumSettingSpecification<LogLevel> { Default = LogLevel.Info } },
			{ BasicSettings.OutputFileLogLevel,                new EnumSettingSpecification<LogLevel> { Default = LogLevel.Verbose } },
			{ BasicSettings.ProjectRootDirectory,			   new StringSettingSpecification { ValueGetter = values => Path.GetFullPath(values.Single()), Default = Environment.CurrentDirectory } },
		};
	}

	public class SettingsCommandLineSpecifications : IEnumExtensionProvider<BasicSettings, ICommandLineOption>
	{
		public IReadOnlyDictionary<BasicSettings, ICommandLineOption> Specifications { get; } = new Dictionary<BasicSettings, ICommandLineOption>
		{
			{ BasicSettings.ConsoleLogLevel,                 new SingleValueOption { Flag = "-cl|--consoleLogLevel", Desc = "The lowest logging level to output to the console. A good default value to provide is \"Info\"" } },
			{ BasicSettings.OutputFileLogLevel,              new SingleValueOption { Flag = "-fl|--fileLogLevel", Desc = "The lowest logging level to output to a file. A good default value to provide is \"Verbose\"" } },
			{ BasicSettings.LogFileName,                     new SingleValueOption { Flag = "--logFileName", Desc = "The name to give to the log file. This should not include the extension or directories" } },
			{ BasicSettings.LoggerConfigurationFile,         new SingleValueOption { Flag = "--loggerConfigFile", Desc = "Where to look for the logger config file (i.e. NLog.config) The default directory to resolve the file in will be the project root" } },
			{ BasicSettings.RandomSeed,                      new SingleValueOption { Flag = "--randomSeed", Desc = "The seed to use for random numbers." } },
			{ BasicSettings.SupplyUserInput,                 new MultiValueOption  { Flag = "--supplyUserInput", Desc = "For testing purposes. Predetermines user input to the terminal." } },
			{ BasicSettings.ProjectRootDirectory,			 new SingleValueOption { Flag = "--projectRoot", Desc = "The root directory containing all the code for this project. This is probably the same as where git info is stored." } },
		};
	}
}
