using System;
using ApplicationResources.Setup;
using static ApplicationResources.ApplicationUtils.Parameters.ParameterStore;

namespace ApplicationResources.ApplicationUtils.Parameters
{
	public static class TaskParameters
	{
		public static EnumNamesDictionary AllParameters => _parameterStore.AllSettings;
		private readonly static ParameterStore _parameterStore = new(Settings.UnderlyingSettingsStore);

		public static T Get<T>(Enum parameter) => TryGet<T>(parameter, out var value) ? value : default;
		public static bool TryGet<T>(Enum parameter, out T value) => _parameterStore.TryGetValue(parameter, out value);

		public static void RegisterParameters<EnumT>() where EnumT : struct, Enum => RegisterParameters(typeof(EnumT));
		public static void RegisterParameters(Type enumType)
		{
			if (enumType.IsEnum) _parameterStore.RegisterSettings(enumType);
			else throw new InvalidOperationException($"Only enum types can be used to provide parameters, but the given type, {enumType.Name} is not an enum type");
		}

		public static ParameterBuilder GetBuilder() => _parameterStore.GetBuilder();

	}
}
