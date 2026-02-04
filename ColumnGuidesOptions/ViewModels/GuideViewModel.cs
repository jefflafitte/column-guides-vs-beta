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

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace ColumnGuidesOptions
{
	public partial class GuideViewModel(int index, Guide guide) : ObservableObject
	{
		public static ObservableCollection<int> PredefinedWidths { get; } =
			new(DefaultSettings.Instance.PredefinedGuideWidths);

		public static int DefaultPredefinedWidth { get; } =
			PredefinedWidths[DefaultSettings.Instance.DefaultPredefinedGuideWidthIndex];

		public static ObservableCollection<DoubleCollection> PredefinedDashes { get; } =
			new(DefaultSettings.Instance.PredefinedGuideDashes);

		public static DoubleCollection DefaultPredefinedDashes { get; } =
			PredefinedDashes[DefaultSettings.Instance.DefaultPredefinedGuideDashesIndex];

		[ObservableProperty]
		private int _index = index;

		[ObservableProperty]
		private bool _visible = guide.Visible;

		[ObservableProperty]
		private int _column = guide.Column;

		[ObservableProperty]
		private Color _color = guide.Color;

		[ObservableProperty]
		private int _alpha = guide.Color.A;

		[ObservableProperty]
		private int _red = guide.Color.R;

		[ObservableProperty]
		private int _green = guide.Color.G;

		[ObservableProperty]
		private int _blue = guide.Color.B;

		[ObservableProperty]
		private int _width = guide.Width;

		[ObservableProperty]
		private DoubleCollection _dashes =
			PredefinedDashes.FirstOrDefault(x => x.SequenceEqual(guide.Dashes)) ?? DefaultPredefinedDashes;

		public Guide Guide { get; } = guide ?? throw new ArgumentNullException(nameof(guide));

		public void SetRgbFromSystemDrawingColor(System.Drawing.Color color) =>
			Color = Color.FromArgb(Color.A, color.R, color.G, color.B);

		public System.Drawing.Color GetRgbAsSystemDrawingColor() =>
			System.Drawing.Color.FromArgb(255, Color.R, Color.G, Color.B);

		partial void OnVisibleChanged(bool visible) => Guide.Visible = visible;

		partial void OnColumnChanged(int column)
		{
			var clampedColumn = Math.Max(column, 0);

			if (column != clampedColumn)
			{
				Column = clampedColumn;
			}
			else
			{
				Guide.Column = column;
			}
		}

		partial void OnColorChanged(Color color)
		{
			Guide.Color = color;

			if (Alpha != color.A)
			{
				Alpha = color.A;
			}

			if (Red != color.R)
			{
				Red = color.R;
			}

			if (Green != color.G)
			{
				Green = color.G;
			}

			if (Blue != color.B)
			{
				Blue = color.B;
			}
		}

		partial void OnAlphaChanged(int alpha)
		{
			var clampedAlpha = Math.Min(Math.Max(alpha, 0), 255);

			if (alpha != clampedAlpha)
			{
				Alpha = clampedAlpha;
			}
			else
			{
				Color = Color.FromArgb((byte)alpha, Color.R, Color.G, Color.B);
			}
		}

		partial void OnRedChanged(int red)
		{
			var clampedRed = Math.Min(Math.Max(red, 0), 255);

			if (red != clampedRed)
			{
				Red = clampedRed;
			}
			else
			{
				Color = Color.FromArgb(Color.A, (byte)red, Color.G, Color.B);
			}
		}

		partial void OnGreenChanged(int green)
		{
			var clampedGreen = Math.Min(Math.Max(green, 0), 255);

			if (green != clampedGreen)
			{
				Green = clampedGreen;
			}
			else
			{
				Color = Color.FromArgb(Color.A, Color.R, (byte)green, Color.B);
			}
		}

		partial void OnBlueChanged(int blue)
		{
			var clampedBlue = Math.Min(Math.Max(blue, 0), 255);

			if (blue != clampedBlue)
			{
				Blue = clampedBlue;
			}
			else
			{
				Color = Color.FromArgb(Color.A, Color.R, Color.G, (byte)blue);
			}
		}

		partial void OnWidthChanged(int width)
		{
			var clampedWidth = Math.Max(width, 1);

			if (width != clampedWidth)
			{
				Width = clampedWidth;
			}
			else
			{
				Guide.Width = width;
			}
		}

		partial void OnDashesChanged(DoubleCollection dashes) => Guide.Dashes = dashes ?? DefaultPredefinedDashes;
	}
}
