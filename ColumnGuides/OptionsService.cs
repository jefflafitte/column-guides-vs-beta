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
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel.Composition;

namespace ColumnGuides
{
	internal interface IOptionsService
	{
		public event EventHandler OptionsLoaded;

		public Options Options { get; }

		public void LoadOptionsFromStorage();

		public void SaveOptionsToStorage();
	}

	[Export(typeof(IOptionsService))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	internal sealed class OptionsService : IOptionsService
	{
		public event EventHandler OptionsLoaded;

		private const string StorageCollectionPath = "ColumnGuides";
		private const string StorageOptionsPropertyName = "Options";

		private Options _options;

		public Options Options => _options;

		public OptionsService() => _options = new Options
		{
			ShowGuides = DefaultSettings.Instance.ShowGuides,
			StickToPage = DefaultSettings.Instance.StickToPage,
			SnapToPixels = DefaultSettings.Instance.SnapToPixels
		};

		public void LoadOptionsFromStorage()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var manager = new ShellSettingsManager(ServiceProvider.GlobalProvider);

			var store = manager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

			if (store.PropertyExists(StorageCollectionPath, StorageOptionsPropertyName))
			{
				var data = store.GetString(StorageCollectionPath, StorageOptionsPropertyName, "");

				var converter = new OptionsConverter();

				_options = converter.ConvertFromString(data) as Options ?? _options;
			}

			OptionsLoaded?.Invoke(this, EventArgs.Empty);
		}

		public void SaveOptionsToStorage()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var manager = new ShellSettingsManager(ServiceProvider.GlobalProvider);

			var store = manager.GetWritableSettingsStore(SettingsScope.UserSettings);

			if (!store.CollectionExists(StorageCollectionPath))
			{
				store.CreateCollection(StorageCollectionPath);
			}

			if (new OptionsConverter().ConvertToString(_options) is string data)
			{
				store.SetString(StorageCollectionPath, StorageOptionsPropertyName, data);
			}
		}
	}
}
