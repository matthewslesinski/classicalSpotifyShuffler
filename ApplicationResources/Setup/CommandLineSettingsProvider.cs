using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using ApplicationResources.Logging;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Immutable;

namespace ApplicationResources.Setup
{
	public class CommandLineSettingsProvider : SettingsParserBase
	{
		public CommandLineSettingsProvider(CommandLineApplication app)
		{
			_app = app;
			app.HelpOption();
		}

		private readonly CommandLineApplication _app;
		private readonly Dictionary<Enum, CommandOption> _options = new();

		public override IEnumerable<Enum> LoadedSettings => _options.Where(kvp => kvp.Value.HasValue()).Select(kvp => kvp.Key);

		public override Task<IEnumerable<Enum>> Load(CancellationToken cancellationToken = default)
		{
			_isLoaded.Value = true;
			return Task.FromResult<IEnumerable<Enum>>(LoadedSettings.ToImmutableArray());
		}

		protected override bool TryGetValues(Enum setting, out IEnumerable<string> values)
		{
			var option = _options[setting];
			if (option.HasValue())
			{
				values = setting.GetExtension<ICommandLineSpecification>().GetValues(option);
				return true;
			}
			values = Array.Empty<string>();
			return false;
		}

		protected override void OnNewSettingsAdded(IEnumerable<Enum> settings, Type enumType)
		{
			if (EnumExtenders<ICommandLineSpecification>.FindExtensionProviderAttributes(enumType).Count() == 1)
			{
				base.OnNewSettingsAdded(settings, enumType);
				settings
					.Select(setting => (setting, setting.GetExtension<ICommandLineSpecification>()))
					.EachIndependently(kvp => _options.Add(kvp.setting, _app.Option(kvp.Item2.Flag, kvp.Item2.Desc, kvp.Item2.Type)));
			}
			else if (EnumExtenders<ICommandLineSpecification>.FindExtensionProviderAttributes(enumType).Count() > 1)
				throw new ArgumentException($"The provided type, {enumType.Name}, does not specify just one provider for {nameof(ICommandLineSpecification)}s");
			else
				Logger.Warning("{className}: Ignoring new settings of type {enumType} because they do not have a provider for {extensionType}s",
					nameof(CommandLineSettingsProvider), enumType.Name, nameof(ICommandLineSpecification));
		}

		public interface ICommandLineSpecification
		{
			string Flag { get; set; }
			string Desc { get; set; }
			CommandOptionType Type { get; }
			IEnumerable<string> GetValues(CommandOption option);
		}
	}
}