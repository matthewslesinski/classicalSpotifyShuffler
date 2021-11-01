using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CustomResources.Utils.Extensions;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.Setup
{
	public class XmlSettingsProvider : SettingsParserBase
	{
		private readonly string _fileName;
		private readonly IEnumerable<Enum> _requiredSettings;
		private Dictionary<Enum, IEnumerable<string>> _loadedValues;
		public XmlSettingsProvider(string fileName, params Enum[] requiredSettings)
		{
			_fileName = fileName;
			_requiredSettings = requiredSettings;
		}

		public override IEnumerable<Enum> LoadedSettings => _loadedValues.Keys;

		public override void Load()
		{
			Util.LoadOnce(ref _isLoaded, _fileName, () =>
			{
				var doc = XElement.Load(_fileName);
				_loadedValues = doc.Descendants(_settingNodeName)
					.Select(node => (node.Attribute(_settingNodeIdentifier).Value, node.Value))
					.GroupBy(GeneralExtensions.GetFirst, GeneralExtensions.GetSecond)
					.ToDictionary<IGrouping<string, string>, Enum, IEnumerable<string>>(group => AllSettings[group.Key], group => group.ToList());
				var missingSettings = _requiredSettings.Where(_loadedValues.NotContainsKey);
				if (missingSettings.Any())
					throw new KeyNotFoundException($"In order to use {_fileName} for settings, it must specify a value for the following settings: {string.Join(", ", missingSettings)}");
			});
		}

		public override string ToString() => $"{nameof(XmlSettingsProvider)}({_fileName})";

		protected override bool TryGetValues(Enum setting, out IEnumerable<string> values) => _loadedValues.TryGetValue(setting, out values);

		private const string _settingNodeName = "Setting";
		private const string _settingNodeIdentifier = "name";
	}
}
