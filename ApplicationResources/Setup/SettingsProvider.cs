using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationResources.Logging;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.Setup
{
	public interface ISettingsProvider
	{
		Task<IEnumerable<Enum>> Load(CancellationToken cancellationToken = default);
		bool IsLoaded { get; }
		MemoryScope Scope { get; }
		IEnumerable<Enum> LoadedSettings { get; }
		bool TryGetValue<R>(Enum setting, out R value);
		bool TryGetValue(Enum setting, out object value);

		void OnSettingsAdded(IEnumerable<Enum> newSettings);
		void OnSettingsAdded(IEnumerable<Enum> newSettings, Type settingType);
	}

	public abstract class SettingsProviderBase : ISettingsProvider
	{
		public IEnumerable<Type> SettingsTypes => AllSettings.Select<Enum, Type>(setting => setting.GetType()).Distinct();
		public EnumNamesDictionary AllSettings { get; } = new();
		public abstract IEnumerable<Enum> LoadedSettings { get; }
		public bool IsLoaded => _isLoaded;
		public abstract MemoryScope Scope { get; }

		public abstract Task<IEnumerable<Enum>> Load(CancellationToken cancellationToken = default);

		public abstract bool TryGetValue(Enum setting, out object value);
		public bool TryGetValue<R>(Enum setting, out R value)
		{
			try
			{
				var foundValue = TryGetValue(setting, out var uncastedValue);
				value = !foundValue ? default : (R)uncastedValue;
				return foundValue;
			}
			catch (InvalidCastException e)
			{
				var message = $"Could not retrieve setting {setting} because its value is of the wrong type. Attempting to cast to type {typeof(R).Name}. {e}";
				Console.Error.WriteLine(message);
				Logger.Error(message);
				value = default;
				return false;
			}
		}

		public void OnSettingsAdded(IEnumerable<Enum> newSettings) => newSettings.GroupBy(setting => setting.GetType()).EachIndependently(group => OnSettingsAdded(group, group.Key));
		public void OnSettingsAdded(IEnumerable<Enum> settings, Type enumType)
		{
			lock (_settingsAddedLock)
			{
				if (IsLoaded || _startedLoading)
					throw new NotSupportedException("Cannot add new settings types after they have already been loaded");

				var newSettings = settings.Where(AllSettings.NotContains).ToList();
				if (newSettings.Any())
					OnNewSettingsAdded(newSettings, enumType);
			}
		}

		protected virtual void OnNewSettingsAdded(IEnumerable<Enum> newSettings, Type enumType)
		{
			if (EnumExtenders<ISettingSpecification>.FindExtensionProviderAttributes(enumType).Count() != 1)
				throw new ArgumentException($"The provided type, {enumType.Name}, does not specify a provider for {nameof(ISettingSpecification)}s");
			newSettings.EachIndependently(setting => AllSettings.Expand(setting, setting.ToString()));
		}

		protected readonly object _settingsAddedLock = new object();
		protected readonly AsyncLockProvider _lock = new();
		protected readonly MutableReference<bool> _isLoaded = new(false);
		protected bool _startedLoading = false;
	}

	public abstract class SettingsParserBase : SettingsProviderBase
	{
		public override MemoryScope Scope => MemoryScope.Global;
		public override bool TryGetValue(Enum setting, out object value)
		{
			var foundValue = TryGetValues(setting, out var rawValues);
			value = !foundValue ? null : ParseSetting(setting, rawValues);
			return foundValue;
		}
		protected abstract bool TryGetValues(Enum setting, out IEnumerable<string> values);

		private static object ParseSetting(Enum setting, IEnumerable<string> rawValueStrings)
		{
			var specification = setting.GetExtension<ISettingSpecification>();
			var parsedValues = specification.ValueGetter(rawValueStrings);
			return parsedValues;
		}
	}

	public class SettingsStore : SettingsProviderBase, IOverrideableDictionary<Enum, object>
	{
		public event Action<IEnumerable<Enum>, Type> SettingsAdded;
		public event TaskUtils.AsyncEvent<IEnumerable<Enum>> OnLoad;

		public override IEnumerable<Enum> LoadedSettings => _overriddenSettings.Count > 0
			? _parsedSettings.Keys.Concat(_overriddenSettings.Keys).Distinct()
			: _parsedSettings.Keys;
		public override MemoryScope Scope { get; }

		public SettingsStore(MemoryScope? scope = null)
		{
			Scope = scope ?? MemoryScope.Global;
			_parsedSettings = new Dictionary<Enum, object>();
			_overriddenSettings = new ScopedConcurrentDictionary<Enum, object>(Scope);
		}

		protected readonly IDictionary<Enum, object> _overriddenSettings;
		protected readonly IDictionary<Enum, object> _parsedSettings;
		private readonly List<ISettingsProvider> _settingsProviders = new();

		public override async Task<IEnumerable<Enum>> Load(CancellationToken cancellationToken = default)
		{
			var wasPerformed = await Util.LoadOnceBlockingAsync(_isLoaded, _lock, async () =>
			{
				_startedLoading = true;
				await _settingsProviders.AsAsyncEnumerable().EachIndependently(async provider =>
				{
					if (!provider.IsLoaded)
						await provider.Load(cancellationToken).WithoutContextCapture();
				}).WithoutContextCapture();
				ResolveAndSetSettings(AllSettings);
			}).WithoutContextCapture();
			if (OnLoad != null && wasPerformed)
				await OnLoad.InvokeAsync(AllSettings).WithoutContextCapture();
			return AllSettings;
		}

		public override bool TryGetValue(Enum setting, out object value)
		{
			var foundValue = _overriddenSettings.TryGetValue(setting, out var uncastedValue)
				|| _parsedSettings.TryGetValue(setting, out uncastedValue);
			value = !foundValue ? null : uncastedValue;
			return foundValue;
		}

		public void RegisterSettings(params Type[] enumTypes) => RegisterSettings(enumTypes.As<IEnumerable<Type>>());
		public void RegisterSettings(IEnumerable<Type> enumTypes) => enumTypes.EachIndependently(enumType => RegisterSettings(Enum.GetValues(enumType).Cast<Enum>(), enumType));
		public void RegisterSettings(IEnumerable<Enum> settings, Type enumType) => OnSettingsAdded(settings, enumType);

		public Task RegisterHighestPriorityProvider(ISettingsProvider provider) => RegisterProvider(provider, 0);
		public async Task RegisterProvider(ISettingsProvider provider, int position = -1)
		{
			if (provider.Scope != MemoryScope.Global && provider.Scope != this.Scope)
				throw new ArgumentException("Cannot attach to a different non-global scope");
			if (provider.IsLoaded)
				throw new ArgumentException($"Cannot accept an already loaded settings provider");
			IEnumerable<Enum> newlyLoadedSettings = null;
			using (await _lock.AcquireToken().WithoutContextCapture())
			{
				if (position < 0 || position >= _settingsProviders.Count)
					_settingsProviders.Add(provider);
				else
					_settingsProviders.Insert(position, provider);
				provider.OnSettingsAdded(AllSettings);
				SettingsAdded -= provider.OnSettingsAdded;
				SettingsAdded += provider.OnSettingsAdded;
				if (IsLoaded)
				{
					if (!provider.IsLoaded)
						newlyLoadedSettings = await provider.Load().WithoutContextCapture();
					ResolveAndSetSettings(newlyLoadedSettings);
				}
			}
			if (OnLoad != null && newlyLoadedSettings != null)
				await OnLoad.InvokeAsync(provider.LoadedSettings).WithoutContextCapture();
		}

		#region Overrides

		public IDisposable AddOverrides(params (Enum key, object value)[] keyValuePairs) => AddOverrides(keyValuePairs.As<IEnumerable<(Enum key, object value)>>());
		public IDisposable AddOverrides(IEnumerable<(Enum key, object value)> keyValuePairs) => _overriddenSettings.AddOverrides(keyValuePairs, OnAddOverride, OnRemoveOverride);
		public IDisposable AddOverride(Enum key, object value) => _overriddenSettings.AddOverride(key, value, OnAddOverride, OnRemoveOverride);

		private void OnAddOverride(Enum key, bool settingAlreadySet, object existingValue, object overrideValue)
		{
			EnsureSettingValueIsAllowed(key, overrideValue);
			var settingStringFormatter = key.GetExtension<ISettingSpecification>().StringFormatter;
			Logger.Verbose("{className}: Overriding {settingType}.{settingName} to value {overrideValue} from value {existingValue}",
				GetType().Name, key.GetType().Name, key, settingStringFormatter(overrideValue), settingAlreadySet ? settingStringFormatter(existingValue) : "<Not Set>");
		}

		private void OnRemoveOverride(Enum key, bool wasSettingAlreadySet, object existingValue, object overrideValue)
		{
			var settingStringFormatter = key.GetExtension<ISettingSpecification>().StringFormatter;
			Logger.Verbose("{className}: Removing override for {settingType}.{settingName}. Removing the override {overrideValue} and setting it back to {existingValue}",
				GetType().Name, key.GetType().Name, key, settingStringFormatter(overrideValue), wasSettingAlreadySet ? settingStringFormatter(existingValue) : "<Not Set>");
		}

		#endregion

		internal IEnumerable<(Enum setting, bool isValueSet, string stringValue)> GetAllSettingsAsStrings(IEnumerable<Enum> enumsToGet = null) =>
			(enumsToGet ?? AllSettings).Select(setting =>
			{
				var isValueSet = TryGetValue(setting, out var parsedSettingValue);
				return (setting, isValueSet, isValueSet ? setting.GetExtension<ISettingSpecification>().StringFormatter(parsedSettingValue) : null);
			});

		protected override void OnNewSettingsAdded(IEnumerable<Enum> newSettings, Type enumType)
		{
			base.OnNewSettingsAdded(newSettings, enumType);
			SettingsAdded?.Invoke(newSettings, enumType);
		}

		protected async Task OnSettingsReloaded(IEnumerable<Enum> settingsToReload)
		{
			if (!IsLoaded)
				throw new NotSupportedException("Cannot reload settings if they haven't been loaded");
			using (await _lock.AcquireToken().WithoutContextCapture())
			{
				ResolveAndSetSettings(settingsToReload);
			}
			if (OnLoad != null)
				await OnLoad.InvokeAsync(settingsToReload).WithoutContextCapture();
		}

		protected void EnsureSettingValueIsAllowed(Enum setting, object value)
		{
			if (!AllSettings.ContainsKey(setting))
				throw new ArgumentException($"Cannot set a {setting.GetType().Name} that has not been registered, {setting}");
			var specification = setting.GetExtension<ISettingSpecification>();
			if (!specification.IsValueAllowed(value))
				throw new ArgumentException($"The {setting.GetType().Name} {setting} does not allow the given value {value}");
		}

		private void ResolveAndSetSettings(IEnumerable<Enum> settingsToResolve = null)
		{
			IEnumerable<Enum> settings;
			// Lock to make sure there are no active calls for registering settings
			lock (_settingsAddedLock)
			{
				settings = settingsToResolve ?? AllSettings;
				settings.Where(AllSettings.Contains).EachIndependently(settingName =>
				{
					var specification = settingName.GetExtension<ISettingSpecification>();
					var didFindValue = _settingsProviders
						.Where(provider => provider != null && provider.IsLoaded)
						.TryGetFirst((ISettingsProvider provider, out object value) => provider.TryGetValue(settingName, out value), out var foundValue);
					if (didFindValue)
						_parsedSettings[settingName] = foundValue;
					else if (specification.IsRequired)
						throw new KeyNotFoundException($"A value for setting {settingName.GetType().Name}.{settingName} is required but nothing was provided");
					else if (specification.HasDefault)
						_parsedSettings[settingName] = specification.Default;
				});
			}
			GetAllSettingsAsStrings(settings).Each(settingState =>
			{
				if (settingState.isValueSet)
					Logger.Verbose("{className}: {settingType}.{settingName} was set to value {settingValue}",
									GetType().Name, settingState.setting.GetType().Name, settingState.setting, settingState.stringValue ?? "<null>");
				else
					Logger.Verbose("{className}: {settingType}.{settingName} was not set to any value", GetType().Name, settingState.setting.GetType().Name, settingState.setting);
			});
		}
	}
}
