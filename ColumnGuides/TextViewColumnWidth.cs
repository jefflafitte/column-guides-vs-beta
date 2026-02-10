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
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ColumnGuides
{
	internal sealed class TextViewColumnWidth
	{
		private sealed class ColumnTextSource(TextRunProperties properties) : TextSource
		{
			private static readonly string _text = new(
				DefaultSettings.Instance.ProportionalColumnWidthCharacter,
				DefaultSettings.Instance.ProportionalColumnMeasurementStringLength);

			private readonly TextRunProperties _properties = properties;

			public override TextRun GetTextRun(int textSourceCharacterIndex) =>
				(textSourceCharacterIndex < _text.Length) ?
					new TextCharacters(
						_text,
						textSourceCharacterIndex,
						_text.Length - textSourceCharacterIndex,
						_properties) :
					new TextEndOfParagraph(1);

			public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(
				int textSourceCharacterIndexLimit) =>
				new (
					0,
					new CultureSpecificCharacterBufferRange(
						_properties.CultureInfo,
						new CharacterBufferRange("", 0, 0)));

			public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(
				int textSourceCharacterIndex) => textSourceCharacterIndex;
		}

		private sealed class ColumnParagraphProperties(TextRunProperties properties) : TextParagraphProperties
		{
			private readonly TextRunProperties _properties = properties;

			public override TextRunProperties DefaultTextRunProperties => _properties;

			public override bool FirstLineInParagraph => true;

			public override FlowDirection FlowDirection => FlowDirection.LeftToRight;

			public override double Indent => 0;

			public override double LineHeight => 0;

			public override TextAlignment TextAlignment => TextAlignment.Left;

			public override TextMarkerProperties TextMarkerProperties => null;

			public override TextWrapping TextWrapping => TextWrapping.NoWrap;
		}

		private const double MonospaceComparisonThreshold = 1E-10;

		private readonly IWpfTextView _view;
		private readonly IClassificationFormatMap _formatMap;
		private double? _width = null;

		public double Width => _width ??= CalculateWidth() ?? 0;

		public TextViewColumnWidth(IWpfTextView view, IClassificationFormatMap formatMap)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));

			_formatMap = formatMap ?? throw new ArgumentNullException(nameof(formatMap));

			_view.Closed += OnViewClosed;

			_formatMap.ClassificationFormatMappingChanged += OnFormatMappingChanged;
		}

		private void OnViewClosed(object sender, EventArgs e)
		{
			_view.Closed -= OnViewClosed;

			_formatMap.ClassificationFormatMappingChanged -= OnFormatMappingChanged;
		}

		private void OnFormatMappingChanged(object sender, EventArgs e) => _width = null;

		private double? CalculateWidth()
		{
			double? width = null;

			if (_view.FormattedLineSource is not null)
			{
				width = _view.FormattedLineSource.ColumnWidth;

				if (_view.FormattedLineSource.DefaultTextProperties.Typeface.TryGetGlyphTypeface(out var glyphTypeface))
				{
					var settings = DefaultSettings.Instance;

					var glyphIndex1 = glyphTypeface.CharacterToGlyphMap[settings.MonospaceTestCharacter1];
					var glyphIndex2 = glyphTypeface.CharacterToGlyphMap[settings.MonospaceTestCharacter2];

					var width1 = glyphTypeface.AdvanceWidths[glyphIndex1];
					var width2 = glyphTypeface.AdvanceWidths[glyphIndex2];

					if (Math.Abs(width1 - width2) >= MonospaceComparisonThreshold)
					{
						using var textFormatter = TextFormatter.Create(TextFormattingMode.Display);

						var textSource = new ColumnTextSource(_view.FormattedLineSource.DefaultTextProperties);

						var paragraphProperties = new ColumnParagraphProperties(
							_view.FormattedLineSource.DefaultTextProperties);

						var maxParagraphWidth = textFormatter.FormatMinMaxParagraphWidth(
							textSource,
							0,
							paragraphProperties).MaxWidth;

						using var textLine = textFormatter.FormatLine(
							textSource,
							0,
							maxParagraphWidth,
							paragraphProperties,
							null);

						width = textLine.WidthIncludingTrailingWhitespace /
							(double)settings.ProportionalColumnMeasurementStringLength;
					}
				}
			}

			return width;
		}
	}
}
