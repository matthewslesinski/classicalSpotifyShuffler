using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.Linq;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.Setup
{
	public class CommandLineOptions : ISettingsProvider<BasicSettings>
	{
		private readonly Dictionary<BasicSettings, CommandOption> _options;
		private CommandLineOptions(Dictionary<BasicSettings, CommandOption> options) => _options = options;

		public static ISettingsProvider<BasicSettings> Initialize(CommandLineApplication app)
		{
			app.HelpOption();
			var result = Enum.GetValues<BasicSettings>()
				.Select(setting => (setting, setting.GetExtension<ICommandLineOption>()))
				.ToDictionary(kvp => kvp.setting, kvp => app.Option(kvp.Item2.Flag, kvp.Item2.Desc, kvp.Item2.Type));
			var settingsProvider = new CommandLineOptions(result);
			return settingsProvider;
		}

		public bool IsLoaded => true;

		public void Load()
		{
			// no need
		}

		public bool TryGetValues(BasicSettings setting, out IEnumerable<string> values)
		{
			var option = _options[setting];
			if (option.HasValue())
			{
				values = setting.GetExtension<ICommandLineOption>().GetValues(option);
				return true;
			}
			values = Array.Empty<string>();
			return false;
		}

		public interface ICommandLineOption
		{
			string Flag { get; set; }
			string Desc { get; set; }
			CommandOptionType Type { get; }
			IEnumerable<string> GetValues(CommandOption option);
		}
	}
}
