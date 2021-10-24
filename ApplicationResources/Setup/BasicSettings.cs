using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApplicationResources.Logging;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using McMaster.Extensions.CommandLineUtils;
using static ApplicationResources.Setup.CommandLineOptions;
using ApplicationExtensions = ApplicationResources.Utils.GeneralExtensions;

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
	public class SettingsSpecifications : IEnumExtensionProvider<BasicSettings, ISettingsSpecification>
	{
		public IReadOnlyDictionary<BasicSettings, ISettingsSpecification> Specifications { get; } = new Dictionary<BasicSettings, ISettingsSpecification>
		{
			{ BasicSettings.LogFileName,                       new SettingsSpecification() },
			{ BasicSettings.LoggerConfigurationFile,           new SettingsSpecification() },
			{ BasicSettings.ProjectRootDirectory,			   new SettingsSpecification { ValueGetter = values => Path.GetFullPath(values.Single()), Default = Environment.CurrentDirectory } },
			{ BasicSettings.RandomSeed,                        new SettingsSpecification { ValueGetter = values => int.Parse(values.Single()) } },
			{ BasicSettings.ConsoleLogLevel,                   new SettingsSpecification { ValueGetter = values => Enum.Parse<LogLevel>(values.Single(), true), Default = LogLevel.Info } },
			{ BasicSettings.OutputFileLogLevel,                new SettingsSpecification { ValueGetter = values => Enum.Parse<LogLevel>(values.Single(), true), Default = LogLevel.Verbose } },
			{ BasicSettings.SupplyUserInput,                   new SettingsSpecification { ValueGetter = values => values, StringFormatter = ApplicationExtensions.ToJsonString } }
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

	public class SettingsSpecification : ISettingsSpecification
	{
		public bool IsRequired { get; set; } = false;
		public object Default { get; set; } = null;
		public Func<IEnumerable<string>, object> ValueGetter { get; set; }
			= rawValues => rawValues.TryGetSingle(out var foundResult) && !string.IsNullOrWhiteSpace(foundResult) ? foundResult : default;
		public Func<object, string> StringFormatter { get; set; } = obj => obj?.ToString();
	}

	public abstract class CommandLineOptionBase : ICommandLineOption
	{
		public string Flag { get; set; }
		public string Desc { get; set; }
		public abstract CommandOptionType Type { get; }
		public abstract IEnumerable<string> GetValues(CommandOption option);
	}

	public class NoValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.NoValue;

		public override IEnumerable<string> GetValues(CommandOption option)
		{
			return option.HasValue() ? new[] { option.ShortName } : Array.Empty<string>();
		}
	}

	public class SingleValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.SingleValue;

		public override IEnumerable<string> GetValues(CommandOption option)
		{
			return option.HasValue() ? new[] { option.Value() } : Array.Empty<string>();
		}
	}

	public class MultiValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.MultipleValue;

		public override IEnumerable<string> GetValues(CommandOption option)
		{
			return option.Values;
		}
	}
}
