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
			RegisterProvider(defaultValueProvider);
			defaultValueProvider.OnLoad += Load;
			if (defaultValueProvider.IsLoaded)
				Load();
		}

		protected override void OnNewSettingsAdded(IEnumerable<Enum> newSettings, Type enumType)
		{
			if (EnumExtenders<IParameterSpecification>.FindExtensionProviderAttributes(enumType).Count() != 1)
				throw new ArgumentException($"The provided type, {enumType.Name}, does not specify a provider for {nameof(IParameterSpecification)}s");
			base.OnNewSettingsAdded(newSettings, enumType);
		}

		public ParameterBuilder GetBuilder() => new ParameterBuilder(this);

		public class ParameterBuilder
		{
			private readonly List<(Enum parameter, object value)> paramsToSet = new();
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
				if (!_paramStore.AllSettings.ContainsKey(parameter))
					throw new ArgumentException($"Cannot set a parameter that has not been registered, {parameter}");
				var specification = parameter.GetExtension<IParameterSpecification>();
				if (!specification.IsValueAllowed(value))
					throw new ArgumentException($"The parameter {parameter} does not allow the given value {value}");
				paramsToSet.Add((parameter, value));
				return this;
			}

			public IDisposable Apply() => _paramStore.AddOverrides(paramsToSet);
		}

	}

}
