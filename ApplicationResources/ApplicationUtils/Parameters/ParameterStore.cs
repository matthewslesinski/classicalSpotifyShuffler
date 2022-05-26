using System;
using System.Collections.Generic;
using System.Linq;
using ApplicationResources.Setup;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace ApplicationResources.ApplicationUtils.Parameters
{
	public class ParameterStore : SettingsStore
	{
		public ParameterStore(SettingsStore defaultValueProvider) : base(MemoryScope.AsyncLocal)
		{
			TODO
			RegisterProvider(defaultValueProvider);
			defaultValueProvider.OnLoad += OnProviderLoaded;
			if (defaultValueProvider.IsLoaded)
				Load();
		}

		protected override void OnNewSettingsAdded(IEnumerable<Enum> newSettings, Type enumType)
		{
			if (EnumExtenders<IParameterSpecification>.FindExtensionProviderAttributes(enumType).Count() != 1)
				throw new ArgumentException($"The provided type, {enumType.Name}, does not specify a provider for {nameof(IParameterSpecification)}s");
			base.OnNewSettingsAdded(newSettings, enumType);
		}

		private void OnProviderLoaded(IEnumerable<Enum> loadedSettings)
		{
			if (!IsLoaded)
				Load();
			else
				OnSettingsReloaded(loadedSettings);
		}

		public ParameterBuilder GetBuilder() => new ParameterBuilder(this);

		public class ParameterBuilder
		{
			private readonly Dictionary<Enum, object> paramsToSet = new();
			private readonly ParameterStore _paramStore;

			internal ParameterBuilder(ParameterStore parameterStore)
			{
				_paramStore = parameterStore;
			}

			public ParameterBuilder WithAll(IEnumerable<(Enum parameter, object value)> values)
			{
				values.EachIndependently(tup => With(tup.parameter, tup.value));
				return this;
			}

			public ParameterBuilder With(Enum parameter, object value)
			{
				_paramStore.EnsureSettingValueIsAllowed(parameter, value);
				paramsToSet[parameter] = value;
				return this;
			}

			public IDisposable Apply() => _paramStore.AddOverrides(paramsToSet.Select(kvp => (kvp.Key, kvp.Value)));
		}

	}

}
