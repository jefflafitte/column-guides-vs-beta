// Copyright 2026 Jeff Lafitte
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using ColumnGuidesOptions;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel.Composition;
using System.Windows.Markup;

namespace ColumnGuides
{
	internal interface IOptionsService
	{
		public event EventHandler OptionsChanged;

		public Options Options { get; }

		public void LoadFromStorage();

		public void LoadFromXml(IVsSettingsReader reader);

		public void SaveToStorage();

		public void SaveToXml(IVsSettingsWriter writer);

		public void Reset();
	}

	[Export(typeof(IOptionsService))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	internal sealed class OptionsService : IOptionsService
	{
		public event EventHandler OptionsChanged;

		private const string StorageCollectionPath = "ColumnGuides";
		private const string StoragePropertyName = "Options";
		private const string SettingName = "Options";

		private Options _options;

		public Options Options => _options;

		public OptionsService() => InitializeFromDefaults();

		public void LoadFromStorage()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var manager = new ShellSettingsManager(ServiceProvider.GlobalProvider);

			var store = manager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

			if (!store.PropertyExists(StorageCollectionPath, StoragePropertyName))
			{
				return;
			}

			if (!LoadFromJson(store.GetString(StorageCollectionPath, StoragePropertyName, "")))
			{
				return;
			}

			OptionsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void LoadFromXml(IVsSettingsReader reader)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var error = reader.ReadSettingString(SettingName, out string json);

			if (error != 0)
			{
				ActivityLog.TryLogWarning("ColumnGuides", $"Failed to import settings. Error code: {error}");

				return;
			}

			if (!LoadFromJson(json))
			{
				return;
			}

			SaveToStorage();

			OptionsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void SaveToStorage()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var manager = new ShellSettingsManager(ServiceProvider.GlobalProvider);

			var store = manager.GetWritableSettingsStore(SettingsScope.UserSettings);

			if (!store.CollectionExists(StorageCollectionPath))
			{
				store.CreateCollection(StorageCollectionPath);
			}

			if (SaveToJson() is string json)
			{
				store.SetString(StorageCollectionPath, StoragePropertyName, json);
			}
		}

		public void SaveToXml(IVsSettingsWriter writer)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (SaveToJson() is string json)
			{
				var error = writer.WriteSettingString(SettingName, json);

				if (error != 0)
				{
					ActivityLog.TryLogWarning("ColumnGuides", $"Failed to export settings. Error code: {error}");
				}
			}
		}

		public void Reset()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			InitializeFromDefaults();

			SaveToStorage();

			OptionsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void InitializeFromDefaults() => _options = DefaultSettings.Instance.InitialOptions.Clone();

		private bool LoadFromJson(string json)
		{
			if (!string.IsNullOrEmpty(json) &&
				(new OptionsConverter().ConvertFromString(json) is Options loadedOptions))
			{
				_options = loadedOptions;

				return true;
			}

			return false;
		}

		private string SaveToJson() => ((new OptionsConverter().ConvertToString(_options) is string json) &&
			!string.IsNullOrEmpty(json)) ? json : null;
	}
}
