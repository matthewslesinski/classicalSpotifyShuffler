using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;

namespace ApplicationResources.Setup
{
	public static class Settings
	{
		public static EnumNamesDictionary AllSettings => _settingsStore.AllSettings;
		internal static SettingsStore UnderlyingSettingsStore => _settingsStore;
		private readonly static SettingsStore _settingsStore = new();

		public static T Get<T>(Enum setting) => TryGet<T>(setting, out var value) ? value : default;
		public static bool TryGet<T>(Enum setting, out T value) => _settingsStore.TryGetValue(setting, out value);

		public static void RegisterSettings<EnumT>() where EnumT : struct, Enum => RegisterSettings(typeof(EnumT));
		public static void RegisterSettings(Type enumType)
		{
			if (enumType.IsEnum) _settingsStore.RegisterSettings(enumType);
			else throw new InvalidOperationException($"Only enum types can be used to provide settings, but the given type, {enumType.Name} is not an enum type");
		}
		public static void RegisterProviders(params ISettingsProvider[] providers) => RegisterProviders(providers.As<IEnumerable<ISettingsProvider>>());
		public static void RegisterProviders(IEnumerable<ISettingsProvider> providers) => providers.EachIndependently(RegisterProvider);
		public static void RegisterProvider(ISettingsProvider provider) => _settingsStore.RegisterProvider(provider);
		public static void RegisterHighestPriorityProviders(params ISettingsProvider[] providers) => RegisterHighestPriorityProviders(providers.As<IEnumerable<ISettingsProvider>>());
		public static void RegisterHighestPriorityProviders(IEnumerable<ISettingsProvider> providers) => providers.Reverse().EachIndependently(RegisterHighestPriorityProvider);
		public static void RegisterHighestPriorityProvider(ISettingsProvider provider) => _settingsStore.RegisterHighestPriorityProvider(provider);
		public static void Load() => _settingsStore.Load();

		public static IDisposable AddOverrides(params (Enum key, object value)[] keyValuePairs) => _settingsStore.AddOverrides(keyValuePairs);
		public static IDisposable AddOverrides(IEnumerable<(Enum key, object value)> keyValuePairs) => _settingsStore.AddOverrides(keyValuePairs);
		public static IDisposable AddOverride(Enum key, object value) => _settingsStore.AddOverride(key, value);

		public static IEnumerable<(Enum setting, bool isValueSet, string stringValue)> GetAllSettingsAsStrings() => _settingsStore.GetAllSettingsAsStrings();
	}

	public class EnumNamesDictionary : BijectiveDictionary<Enum, string>, ICollection<Enum>, IReadOnlyCollection<Enum>, IEnumerable<string>, IReadOnlyDictionary<Enum, string>, IReadOnlyDictionary<string, Enum>
	{
		public EnumNamesDictionary(bool ignoreCase = true)
			: base(destinationEquality: ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
		{ }

		public int Count => _mapping.Count;
		public bool IsReadOnly => false;

		public IEnumerable<Enum> Keys => InputSpace;
		public IEnumerable<string> Values => OutputSpace;
		IEnumerable<string> IReadOnlyDictionary<string, Enum>.Keys => OutputSpace;
		IEnumerable<Enum> IReadOnlyDictionary<string, Enum>.Values => InputSpace;

		public string this[Enum key] => _mapping[key];
		public Enum this[string key] => _inverseMapping[key];

		public void Add(Enum item) => Expand(item, item.ToString());
		public bool Contains(Enum item) => InputSpace.Contains(item);
		public void CopyTo(Enum[] array, int arrayIndex) => InputSpace.CopyTo(array, arrayIndex);
		public IEnumerator<Enum> GetEnumerator() => InputSpace.GetEnumerator();
		IEnumerator<string> IEnumerable<string>.GetEnumerator() => OutputSpace.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool ContainsKey(Enum key) => _mapping.ContainsKey(key);
		public bool TryGetValue(Enum key, out string value) => _mapping.TryGetValue(key, out value);
		IEnumerator<KeyValuePair<Enum, string>> IEnumerable<KeyValuePair<Enum, string>>.GetEnumerator() => _mapping.GetEnumerator();

		public bool ContainsKey(string key) => _inverseMapping.ContainsKey(key);
		public bool TryGetValue(string key, out Enum value) => _inverseMapping.TryGetValue(key, out value);
		IEnumerator<KeyValuePair<string, Enum>> IEnumerable<KeyValuePair<string, Enum>>.GetEnumerator() => _inverseMapping.GetEnumerator();

		public bool Remove(Enum item) => throw new NotSupportedException();
		public void Clear() => throw new NotSupportedException();
	}
}
