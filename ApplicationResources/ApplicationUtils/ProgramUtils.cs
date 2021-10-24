using System;
using System.IO;
using System.Linq;
using ApplicationResources.Logging;
using McMaster.Extensions.CommandLineUtils;
using ApplicationResources.Setup;
using System.Collections.Generic;
using CustomResources.Utils.Extensions;
using System.Threading.Tasks;
using System.Threading;

namespace ApplicationResources.ApplicationUtils
{
	public static class ProgramUtils
	{
		public static void ExecuteProgramAsync(Func<Task> program, StartupArgs startupArgs)
		{
			ExecuteProgram(() =>
			{
				var task = program();
				while (!task.IsCompleted)
				{
					Thread.Sleep(Timeout.Infinite);
				}
				// Unreachable on purpose in case the compiler would want to get rid of the preceding while loop
				Terminate();
			}, startupArgs);
		}

		public static void ExecuteProgram(Action program, StartupArgs startupArgs)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Logger.Error($"An Exception occurred: {args.ExceptionObject}");
			var app = new CommandLineApplication();
			Settings.RegisterProvider(new CommandLineOptions(app));
			Settings.RegisterSettings<BasicSettings>();
			startupArgs.SettingsTypes.Each(Settings.RegisterSettings);
			Action runner = Settings.Load + program + Terminate;
			if (!string.IsNullOrWhiteSpace(startupArgs.XmlSettingsFileFlag))
			{
				var xmlSettingsFileOption = app.Option(startupArgs.XmlSettingsFileFlag, "The file name for settings stored in xml format", CommandOptionType.MultipleValue);
				Action settingsProviderRegister = () =>
				{
					if (xmlSettingsFileOption.HasValue())
						Settings.RegisterProviders(xmlSettingsFileOption.Values.Where(File.Exists).Select(fileName => new XmlSettingsProvider(fileName)));
					if (startupArgs.AdditionalXmlSettingsFiles.Any())
						Settings.RegisterProviders(startupArgs.AdditionalXmlSettingsFiles.Where(File.Exists).Select(fileName => new XmlSettingsProvider(fileName)));
					if (File.Exists(ApplicationConstants.StandardSettingsFile))
						Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile));
				};
				runner = settingsProviderRegister + runner;
			}
			app.OnExecute(runner);
			app.Execute(startupArgs.CommandLineArgs);
		}

		private static void Terminate()
		{
			Logger.Information("Terminating successfully");
			Console.WriteLine("Terminating successfully");
			Environment.Exit(0);
		}

		public class StartupArgs
		{
			public string[] CommandLineArgs { get; }
			public string XmlSettingsFileFlag { get; set; } = null;
			public IEnumerable<Type> SettingsTypes { get; set; } = Array.Empty<Type>();
			public IEnumerable<string> AdditionalXmlSettingsFiles { get; set; } = Array.Empty<string>();

			public StartupArgs(string[] commandLineArgs)
			{
				CommandLineArgs = commandLineArgs;
			}
		}
	}
}
