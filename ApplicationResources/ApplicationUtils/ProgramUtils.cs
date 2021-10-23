using System;
using System.IO;
using System.Linq;
using ApplicationResources.Logging;
using McMaster.Extensions.CommandLineUtils;
using ApplicationResources.Setup;

namespace ApplicationResources.ApplicationUtils
{
	public static class ProgramUtils
	{
		public static void ExecuteProgram(string[] args, Action program, string xmlSettingsFileFlag = null)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Logger.Error($"An Exception occurred: {args.ExceptionObject}");
			var app = new CommandLineApplication();
			var commandLineSettingsProvider = new CommandLineOptions(app);
			Settings.RegisterProvider(commandLineSettingsProvider);
			Settings.RegisterSettings<BasicSettings>();
			Action runner = Settings.Load + program;
			if (!string.IsNullOrWhiteSpace(xmlSettingsFileFlag))
			{
				var xmlSettingsFileOption = app.Option(xmlSettingsFileFlag, "The file name for settings stored in xml format", CommandOptionType.MultipleValue);
				Action settingsProviderRegister = () =>
				{
					if (xmlSettingsFileOption.HasValue())
						Settings.RegisterProviders(xmlSettingsFileOption.Values.Where(File.Exists).Select(fileName => new XmlSettingsProvider(fileName)));
					if (File.Exists(ApplicationConstants.SuggestedAuthorizationSettingsFile))
						Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.SuggestedAuthorizationSettingsFile));
					if (File.Exists(ApplicationConstants.StandardSettingsFile))
						Settings.RegisterProvider(new XmlSettingsProvider(ApplicationConstants.StandardSettingsFile));
				};
				runner = settingsProviderRegister + runner;
			}
			app.OnExecute(runner);
			app.Execute(args);
		}
	}
}
