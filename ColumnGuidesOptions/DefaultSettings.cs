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

using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace ColumnGuidesOptions
{
	public sealed class DefaultSettings
	{
		private static DefaultSettings _instance;

		private int[] _predefinedGuideWidths = [1, 2, 3];

		private int _defaultPredefinedGuideWidthIndex = 0;

		private DoubleCollection[] _predefinedGuideDashes =
			[[], [1, 1], [1, 2], [1, 4], [2, 1], [2, 2], [2, 4], [4, 2], [4, 4], [4, 8]];

		private int _defaultPredefinedGuideDashesIndex = 0;

		public static DefaultSettings Instance => _instance ??= (InitialzeFromSettings() ?? new());

		public bool ShowGuides { get; set; } = true;

		public bool StickToPage { get; set; } = true;

		public bool SnapToPixels { get; set; } = true;

		public int MaxAssociationCount { get; set; } = 64;

		public bool NewAssociationEnabled { get; set; } = true;

		public int MaxAssociationFileTypesLength { get; set; } = 64;

		public string NewAssociationFileTypes { get; set; } = "*.*";

		public bool NewAssociationAddGuide {  get; set; } = true;

		public int MaxAssociationGuideCount { get; set; } = 64;

		public bool NewGuideVisible { get; set; } = true;

		public int MaxGuideColumn { get; set; } = 999;

		public int NewGuideColumn { get; set; } = 80;

		public System.Drawing.Color NewGuideColor { get; set; } = System.Drawing.Color.Gray;

		public int NewGuideWidth { get; set; } = 1;

		public int[] PredefinedGuideWidths
		{
			get => _predefinedGuideWidths;

			set
			{
				_predefinedGuideWidths = ((value?.Length ?? 0) > 0) ? value : _predefinedGuideWidths;

				_defaultPredefinedGuideWidthIndex = Math.Min(
					_defaultPredefinedGuideWidthIndex,
					_predefinedGuideWidths.Length - 1);
			}
		}

		public int DefaultPredefinedGuideWidthIndex
		{
			get => _defaultPredefinedGuideWidthIndex;

			set => _defaultPredefinedGuideWidthIndex =
				((value >= 0) && (value < _predefinedGuideWidths.Length)) ? value : 0;
		}

		public DoubleCollection[] PredefinedGuideDashes
		{
			get => _predefinedGuideDashes;

			set
			{
				_predefinedGuideDashes = ((value?.Length ?? 0) > 0) ? value : _predefinedGuideDashes;

				_defaultPredefinedGuideDashesIndex = Math.Min(
					_defaultPredefinedGuideDashesIndex,
					_predefinedGuideDashes.Length - 1);
			}
		}

		public int DefaultPredefinedGuideDashesIndex
		{
			get => _defaultPredefinedGuideDashesIndex;

			set => _defaultPredefinedGuideDashesIndex =
				((value >= 0) && (value < _predefinedGuideDashes.Length)) ? value : 0;
		}

		public char MonospaceTestCharacter1 { get; set; } = 'M';

		public char MonospaceTestCharacter2 { get; set; } = '.';

		public char ProportionalColumnWidthCharacter { get; set; } = 'x';

		public int ProportionalColumnMeasurementStringLength { get; set; } = 128;

		private static DefaultSettings InitialzeFromSettings()
		{
			DefaultSettings defaultSettings = null;

			var settingsPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"ColumnGuides",
				"Settings.json");

			if (File.Exists(settingsPath))
			{
				try
				{
					var json = File.ReadAllText(settingsPath);

					defaultSettings = JsonSerializer.Deserialize<DefaultSettings>(json);
				}
				catch (Exception ex)
				{
					ActivityLog.TryLogWarning("ColumnGuides", $"Failed to load settings: {ex.Message}");
				}
			}

			return defaultSettings;
		}
	}
}
