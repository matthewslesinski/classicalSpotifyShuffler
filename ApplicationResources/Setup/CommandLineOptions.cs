using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using ApplicationResources.Logging;

namespace ApplicationResources.Setup
{
	public class CommandLineOptions : SettingsProviderBase<IEnumerable<string>>
	{
		public CommandLineOptions(CommandLineApplication app)
		{
			_app = app;
			app.HelpOption();
		}

		private readonly CommandLineApplication _app;
		private readonly Dictionary<Enum, CommandOption> _options = new();

		public override void Load()
		{
			_isLoaded = true;
		}

		public override bool TryGetValue(Enum setting, out IEnumerable<string> values)
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

		protected override void OnNewSettingsAdded(IEnumerable<Enum> settings, Type enumType)
		{
			if (EnumExtenders<ICommandLineOption>.FindExtensionProviderAttributes(enumType).Count() == 1)
			{
				base.OnNewSettingsAdded(settings, enumType);
				settings
					.Select(setting => (setting, setting.GetExtension<ICommandLineOption>()))
					.EachIndependently(kvp => _options.Add(kvp.setting, _app.Option(kvp.Item2.Flag, kvp.Item2.Desc, kvp.Item2.Type)));
			}
			else if (EnumExtenders<ICommandLineOption>.FindExtensionProviderAttributes(enumType).Count() > 1)
				throw new ArgumentException($"The provided type, {enumType.Name}, does not specify just one provider for {nameof(ICommandLineOption)}s");
			else
				Logger.Warning("{className}: Ignoring new settings of type {enumType} because they do not have a provider for {extensionType}s",
					nameof(CommandLineOptions), enumType.Name, nameof(ICommandLineOption));
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
