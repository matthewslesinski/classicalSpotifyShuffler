using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CustomResources.Utils.Extensions;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.Setup
{
	public class XmlSettingsProvider : ISettingsProvider<BasicSettings>
	{
		private readonly string _fileName;
		private readonly IEnumerable<BasicSettings> _requiredSettings;
		private Dictionary<BasicSettings, IEnumerable<string>> _loadedValues;
		public XmlSettingsProvider(string fileName, params BasicSettings[] requiredSettings)
		{
			_fileName = fileName;
			_requiredSettings = requiredSettings;
		}

		public bool IsLoaded => _isLoaded;
		private bool _isLoaded = false;

		public void Load()
		{
			Util.LoadOnce(ref _isLoaded, _fileName, () =>
			{
				var doc = XElement.Load(_fileName);
				_loadedValues = doc.Descendants(_settingNodeName)
					.Select(node => (node.Attribute(_settingNodeIdentifier).Value, node.Value))
					.GroupBy(GeneralExtensions.GetFirst, GeneralExtensions.GetSecond)
					.ToDictionary<IGrouping<string, string>, BasicSettings, IEnumerable<string>>(group => Enum.Parse<BasicSettings>(group.Key, true), group => group.ToList());
				var missingSettings = _requiredSettings.Where(requiredSetting => !_loadedValues.ContainsKey(requiredSetting));
				if (missingSettings.Any())
					throw new KeyNotFoundException($"In order to use {_fileName} for settings, it must specify a value for the following settings: {string.Join(", ", missingSettings)}");
				
			});
		}

		public bool TryGetValues(BasicSettings setting, out IEnumerable<string> values) => _loadedValues.TryGetValue(setting, out values);
		
		private const string _settingNodeName = "Setting";
		private const string _settingNodeIdentifier = "name";
	}
}
