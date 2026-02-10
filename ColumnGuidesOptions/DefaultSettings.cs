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
using System.Linq;
using System.Text.Json;
using System.Windows.Media;

namespace ColumnGuidesOptions
{
	public sealed class DefaultSettings
	{
		private const string FactoryDefaultFileTypes = "*.*";

		private const int FactoryDefaultGuideColumn = 80;

		private const int FactoryDefaultGuideWidth = 1;

		private static readonly Color FactoryDefaultGuideColor = Colors.Gray;

		private static readonly int[] FactoryDefaultPredefinedGuideWidths = [1, 2, 3];

		private static readonly DoubleCollection[] FactoryDefaultPredefinedGuideDashes =
			[[], [1, 1], [1, 2], [1, 4], [2, 1], [2, 2], [2, 4], [4, 2], [4, 4], [4, 8]];

		private static readonly Options FactoryDefaultInitialOptions = new()
		{
			ShowGuides = true,
			StickToPage = true,
			SnapToPixels = true,
			Associations = [new FileTypesAssociation
			{
				Enabled = true,
				FileTypes = FactoryDefaultFileTypes,
				Guides = [new Guide
				{
					Visible = true,
					Column = FactoryDefaultGuideColumn,
					Color = FactoryDefaultGuideColor,
					Width = FactoryDefaultGuideWidth
				}]
			}]
		};

		private static DefaultSettings _instance;

		public static DefaultSettings Instance => _instance ??= InitializeFromSettings() ?? new();

		public Options InitialOptions { get; init; } = FactoryDefaultInitialOptions.Clone();

		public int MaxAssociationCount { get; init; } = 64;

		public bool NewAssociationEnabled { get; init; } = true;

		public int MaxAssociationFileTypesLength { get; init; } = 64;

		public string NewAssociationFileTypes { get; init; } = FactoryDefaultFileTypes;

		public bool NewAssociationAddGuide { get; init; } = true;

		public int MaxAssociationGuideCount { get; init; } = 64;

		public bool NewGuideVisible { get; init; } = true;

		public int MaxGuideColumn { get; init; } = 999;

		public int NewGuideColumn { get; init; } = FactoryDefaultGuideColumn;

		public Color NewGuideColor { get; init; } = FactoryDefaultGuideColor;

		public int NewGuideWidth { get; init; } = FactoryDefaultGuideWidth;

		public int[] PredefinedGuideWidths { get; init; } = FactoryDefaultPredefinedGuideWidths.ToArray();

		public int DefaultPredefinedGuideWidthIndex { get; init; } = 0;

		public DoubleCollection[] PredefinedGuideDashes { get; init; } = FactoryDefaultPredefinedGuideDashes.ToArray();

		public int DefaultPredefinedGuideDashesIndex { get; init; } = 0;

		public char MonospaceTestCharacter1 { get; init; } = 'M';

		public char MonospaceTestCharacter2 { get; init; } = '.';

		public char ProportionalColumnWidthCharacter { get; init; } = 'x';

		public int ProportionalColumnMeasurementStringLength { get; init; } = 128;

		[System.Text.Json.Serialization.JsonConstructor]
		public DefaultSettings()
		{
			InitialOptions ??= FactoryDefaultInitialOptions.Clone();

			MaxAssociationCount = Math.Max(MaxAssociationCount, 0);
			MaxAssociationFileTypesLength = Math.Max(MaxAssociationFileTypesLength, 0);
			NewAssociationFileTypes ??= FactoryDefaultFileTypes;
			MaxAssociationGuideCount = Math.Max(MaxAssociationGuideCount, 0);
			MaxGuideColumn = Math.Max(MaxGuideColumn, 0);
			NewGuideColumn = Math.Max(NewGuideColumn, 0);
			NewGuideWidth = Math.Max(NewGuideWidth, 1);

			if ((PredefinedGuideWidths?.Length ?? 0) == 0)
			{
				PredefinedGuideWidths = FactoryDefaultPredefinedGuideWidths.ToArray();
			}

			DefaultPredefinedGuideWidthIndex = Math.Min(
				Math.Max(DefaultPredefinedGuideWidthIndex, 0),
				PredefinedGuideWidths.Length - 1);

			if ((PredefinedGuideDashes?.Length ?? 0) == 0)
			{
				PredefinedGuideDashes = FactoryDefaultPredefinedGuideDashes.ToArray();
			}

			DefaultPredefinedGuideDashesIndex = Math.Min(
				Math.Max(DefaultPredefinedGuideDashesIndex, 0),
				PredefinedGuideDashes.Length - 1);
		}

		private static DefaultSettings InitializeFromSettings()
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
