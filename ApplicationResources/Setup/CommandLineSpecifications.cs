using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using static ApplicationResources.Setup.CommandLineSettingsProvider;

namespace ApplicationResources.Setup
{
	public abstract class CommandLineOptionBase : ICommandLineSpecification
	{
		public string Flag { get; set; }
		public string Desc { get; set; }
		public abstract CommandOptionType Type { get; }
		public abstract IEnumerable<string> GetValues(CommandOption option);
	}

	public class NoValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.NoValue;

		public override IEnumerable<string> GetValues(CommandOption option) => option.HasValue() ? new[] { option.ShortName } : Array.Empty<string>();
	}

	public class SingleValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.SingleValue;

		public override IEnumerable<string> GetValues(CommandOption option) => option.HasValue() ? new[] { option.Value() } : Array.Empty<string>();
	}

	public class MultiValueOption : CommandLineOptionBase
	{
		public override CommandOptionType Type => CommandOptionType.MultipleValue;

		public override IEnumerable<string> GetValues(CommandOption option) => option.Values;
	}
}
