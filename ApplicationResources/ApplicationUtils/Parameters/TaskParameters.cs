using System;
using System.Threading.Tasks;
using ApplicationResources.Setup;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using static ApplicationResources.ApplicationUtils.Parameters.ParameterStore;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.ApplicationUtils.Parameters
{
	public static class TaskParameters
	{
		public static EnumNamesDictionary AllParameters => _parameterStore.AllSettings;
		internal static SettingsStore UnderlyingParameterStore => _parameterStore;
		private readonly static ParameterStore _parameterStore = new();
		private static readonly AsyncLockProvider _lock = new();
		private static readonly MutableReference<bool> _isReady = new(false);

		public static Task Initialize()
		{
			return Util.LoadOnceBlockingAsync(_isReady, _lock, () => _parameterStore.AttachTo(Settings.UnderlyingSettingsStore));
		}

		public static T Get<T>(Enum parameter) => TryGet<T>(parameter, out var value) ? value : default;
		public static bool TryGet<T>(Enum parameter, out T value)
		{
			if (!_isReady)
				throw new InvalidOperationException("Parameters have finished initialization");
			return _parameterStore.TryGetValue(parameter, out value);
		}

		public static void RegisterParameters<EnumT>() where EnumT : struct, Enum => RegisterParameters(typeof(EnumT));
		public static void RegisterParameters(Type enumType)
		{
			if (enumType.IsEnum && _isReady) _parameterStore.RegisterSettings(enumType);
			else throw new InvalidOperationException($"Only enum types can be used to provide parameters, but the given type, {enumType.Name} is not an enum type");
		}

		public static ParameterBuilder GetBuilder() => _parameterStore.GetBuilder();
	}
}
