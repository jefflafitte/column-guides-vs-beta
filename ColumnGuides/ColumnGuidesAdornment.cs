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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ColumnGuides
{
	internal sealed class ColumnGuidesAdornment
	{
		private class AssociationLines
		{
			public FileTypesAssociationViewModel AssociationVm { get; private set; }

			public List<Line> Lines { get; private set; } = [];

			public AssociationLines(FileTypesAssociationViewModel associationVm)
			{
				Debug.Assert(associationVm is not null);

				AssociationVm = associationVm;
			}

			public AssociationLines(
				FileTypesAssociationViewModel associationVm,
				Func<GuideViewModel, Line> createLine) : this(associationVm)
			{
				Debug.Assert(associationVm is not null);

				foreach (var guideVm in associationVm.GuideVms)
				{
					Debug.Assert(guideVm is not null);

					if (guideVm.Visible)
					{
						Lines.Add(createLine(guideVm));
					}
				}
			}

			public Line FindLine(GuideViewModel guideVm) => Lines.Find(x => x.Tag == guideVm);

			public int FindLineIndex(GuideViewModel guideVm) => Lines.FindIndex(x => x.Tag == guideVm);

			public int LineLowerBoundIndex(GuideViewModel guideVm)
			{
				Debug.Assert(guideVm is not null);

				var lineIndex = Lines.FindIndex(x => ((x.Tag as GuideViewModel)?.Index ?? -1) >= guideVm.Index);

				return (lineIndex >= 0) ? lineIndex : Lines.Count;
			}
		}

		[Flags]
		private enum MatchingAssociationFlags : byte { All = 0, SkipEnabled = 1, SkipFileTypes = 2 };

		[Flags]
		private enum MatchingGuideFlags : byte { All = 0, SkipVisible = 1 };

		internal const string Name = "ColumnGuidesAdornment";

		private readonly IWpfTextView _view;
		private readonly ITextDocument _document;
		private readonly OptionsViewModel _optionsVm;

		private readonly TextViewColumnWidth _columnWidth;
		private readonly IAdornmentLayer _layer;

		private readonly List<AssociationLines> _associationLines = [];

		private string _fileName;

		private string FileName => _fileName ??= System.IO.Path.GetFileName(_document.FilePath);

		public ColumnGuidesAdornment(
			IWpfTextView view,
			ITextDocument document,
			IClassificationFormatMap formatMap,
			OptionsViewModel optionsViewModel)
		{
			_ = view ?? throw new ArgumentNullException(nameof(view));
			_ = document ?? throw new ArgumentNullException(nameof(document));
			_ = formatMap ?? throw new ArgumentNullException(nameof(formatMap));

			_view = view;
			_document = document;
			_optionsVm = optionsViewModel ?? throw new ArgumentNullException(nameof(optionsViewModel));

			_columnWidth = new(view, formatMap);
			_layer = view.GetAdornmentLayer(Name);

			SubscribeToEvents();

			InitializeLines();
		}

		private void SubscribeToEvents()
		{
			_view.LayoutChanged += OnLayoutChanged;
			_view.Closed += OnViewClosed;

			_document.FileActionOccurred += OnFileActionOccurred;

			_optionsVm.OptionsReset += OnOptionsReset;
			_optionsVm.PropertyChanged += OnOptionsPropertyChanged;
			_optionsVm.AssociationAdded += OnOptionsAssociationAdded;
			_optionsVm.AssociationRemoved += OnOptionsAssociationRemoved;
			_optionsVm.AssociationMoved += OnOptionsAssociationMoved;
			_optionsVm.AssociationPropertyChanged += OnOptionsAssociationPropertyChanged;
			_optionsVm.GuideAdded += OnOptionsGuideAdded;
			_optionsVm.GuideRemoved += OnOptionsGuideRemoved;
			_optionsVm.GuideMoved += OnOptionsGuideMoved;
			_optionsVm.GuidePropertyChanged += OnOptionsGuidePropertyChanged;
		}

		private void UnsubscribeFromEvents()
		{
			_view.LayoutChanged -= OnLayoutChanged;
			_view.Closed -= OnViewClosed;

			_document.FileActionOccurred -= OnFileActionOccurred;

			_optionsVm.OptionsReset -= OnOptionsReset;
			_optionsVm.PropertyChanged -= OnOptionsPropertyChanged;
			_optionsVm.AssociationAdded -= OnOptionsAssociationAdded;
			_optionsVm.AssociationRemoved -= OnOptionsAssociationRemoved;
			_optionsVm.AssociationMoved -= OnOptionsAssociationMoved;
			_optionsVm.AssociationPropertyChanged -= OnOptionsAssociationPropertyChanged;
			_optionsVm.GuideAdded -= OnOptionsGuideAdded;
			_optionsVm.GuideRemoved -= OnOptionsGuideRemoved;
			_optionsVm.GuideMoved -= OnOptionsGuideMoved;
			_optionsVm.GuidePropertyChanged -= OnOptionsGuidePropertyChanged;
		}

		private void InitializeLines()
		{
			Debug.Assert(_optionsVm.AssociationVms is not null);

			foreach (var associationVm in _optionsVm.AssociationVms)
			{
				Debug.Assert(associationVm is not null);

				if (!IsMatchingAssociation(associationVm))
				{
					continue;
				}

				var associationLines = new AssociationLines(associationVm, CreateLine);

				if (associationLines.Lines.Count > 0)
				{
					_associationLines.Add(associationLines);

					foreach (var line in associationLines.Lines)
					{
						Debug.Assert(line is not null);

						AddAdornment(line);
					}
				}
			}
		}

		private void ClearLines()
		{
			_layer.RemoveAllAdornments();

			_associationLines.Clear();
		}

		private void ResetLines()
		{
			ClearLines();

			InitializeLines();
		}

		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (e.VerticalTranslation || (e.NewViewState.ViewportHeight != e.OldViewState.ViewportHeight))
			{
				foreach (var associationLines in _associationLines)
				{
					Debug.Assert(associationLines is not null);

					foreach (var line in associationLines.Lines)
					{
						Debug.Assert(line is not null);
						Debug.Assert(line.Tag is GuideViewModel);

						line.Y1 = _view.ViewportTop;
						line.Y2 = _view.ViewportBottom;
						line.StrokeDashOffset = CalculateStrokeDashOffset((GuideViewModel)line.Tag);
					}
				}
			}
		}

		private void OnViewClosed(object sender, EventArgs e)
		{
			UnsubscribeFromEvents();

			ClearLines();
		}

		private void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
		{
			if (e.FileActionType == FileActionTypes.DocumentRenamed)
			{
				_fileName = null;

				ResetLines();
			}
		}

		private void OnOptionsReset(object sender, EventArgs e) => ResetLines();

		private void OnOptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_optionsVm.OptionsResetting)
			{
				return;
			}

			switch (e.PropertyName)
			{
				case nameof(OptionsViewModel.ShowGuides):
					OnShowGuidesChanged();
					break;

				case nameof(OptionsViewModel.StickToPage):
					OnStickToPageChanged();
					break;

				case nameof(OptionsViewModel.SnapToPixels):
					OnSnapToPixelsChanged();
					break;
			}
		}

		private void OnOptionsAssociationAdded(object sender, AssociationAddedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);

			if (IsMatchingAssociation(e.AssociationVm))
			{
				AddAssociation(e.AssociationVm);
			}
		}

		private void OnOptionsAssociationRemoved(object sender, AssociationRemovedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);

			if (IsMatchingAssociation(e.AssociationVm))
			{
				RemoveAssociation(e.AssociationVm);
			}
		}

		private void OnOptionsAssociationMoved(object sender, AssociationMovedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);

			if (!IsMatchingAssociation(e.AssociationVm))
			{
				return;
			}

			var oldAssociationLinesIndex = FindAssociationLinesIndex(e.AssociationVm);

			if (oldAssociationLinesIndex == -1)
			{
				return;
			}

			var associationLines = _associationLines[oldAssociationLinesIndex];

			Debug.Assert(associationLines is not null);

			_associationLines.RemoveAt(oldAssociationLinesIndex);

			RemoveAdornments(associationLines);

			var newAssociationLinesIndex = AssociationLinesLowerBoundIndex(e.AssociationVm);

			RemoveAdornments(newAssociationLinesIndex);

			_associationLines.Insert(newAssociationLinesIndex, associationLines);

			AddAdornments(newAssociationLinesIndex);
		}

		private void OnOptionsAssociationPropertyChanged(object sender, AssociationPropertyChangedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);

			switch (e.PropertyName)
			{
				case nameof(FileTypesAssociationViewModel.Enabled):
					OnAssociationEnabledChanged(e.AssociationVm);
					break;

				case nameof(FileTypesAssociationViewModel.FileTypes):
					OnFileTypesChanged(e.AssociationVm);
					break;
			}
		}

		private void OnOptionsGuideAdded(object sender, GuideAddedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);
			Debug.Assert(e.GuideVm is not null);

			if (IsMatchingGuide(e.AssociationVm, e.GuideVm))
			{
				AddGuide(e.AssociationVm, e.GuideVm);
			}
		}

		private void OnOptionsGuideRemoved(object sender, GuideRemovedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);
			Debug.Assert(e.GuideVm is not null);

			if (IsMatchingGuide(e.AssociationVm, e.GuideVm))
			{
				RemoveGuide(e.AssociationVm, e.GuideVm);
			}
		}

		private void OnOptionsGuideMoved(object sender, GuideMovedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);
			Debug.Assert(e.GuideVm is not null);

			if (!IsMatchingGuide(e.AssociationVm, e.GuideVm))
			{
				return;
			}

			var (associationLinesIndex, oldLineIndex) = FindAssociationLinesAndLineIndexes(e.AssociationVm, e.GuideVm);

			if ((associationLinesIndex == -1) || (oldLineIndex == -1))
			{
				return;
			}

			var associationLines = _associationLines[associationLinesIndex];

			Debug.Assert(associationLines is not null);

			var line = associationLines.Lines[oldLineIndex];

			Debug.Assert(line is not null);

			associationLines.Lines.RemoveAt(oldLineIndex);

			_layer.RemoveAdornment(line);

			var newLineIndex = associationLines.LineLowerBoundIndex(e.GuideVm);

			RemoveAdornments(associationLinesIndex, newLineIndex);

			associationLines.Lines.Insert(newLineIndex, line);

			AddAdornments(associationLinesIndex, newLineIndex);
		}

		private void OnOptionsGuidePropertyChanged(object sender, GuidePropertyChangedEventArgs e)
		{
			Debug.Assert(e.AssociationVm is not null);
			Debug.Assert(e.GuideVm is not null);

			switch (e.PropertyName)
			{
				case nameof(GuideViewModel.Visible):
					OnGuideVisibleChanged(e.AssociationVm, e.GuideVm);
					break;

				case nameof(GuideViewModel.Column):
					OnGuideColumnChanged(e.AssociationVm, e.GuideVm);
					break;

				case nameof(GuideViewModel.Color):
					OnGuideColorChanged(e.AssociationVm, e.GuideVm);
					break;

				case nameof(GuideViewModel.Width):
					OnGuideWidthChanged(e.AssociationVm, e.GuideVm);
					break;

				case nameof(GuideViewModel.Dashes):
					OnGuideDashesChanged(e.AssociationVm, e.GuideVm);
					break;
			}
		}

		private void OnShowGuidesChanged()
		{
			if (_optionsVm.ShowGuides)
			{
				InitializeLines();
			}
			else
			{
				_layer.RemoveAllAdornments();

				_associationLines.Clear();
			}
		}

		private void OnStickToPageChanged()
		{
			foreach (var associationLines in _associationLines)
			{
				Debug.Assert(associationLines is not null);

				foreach (var line in associationLines.Lines)
				{
					Debug.Assert(line is not null);
					Debug.Assert(line.Tag is GuideViewModel);

					line.StrokeDashOffset = CalculateStrokeDashOffset((GuideViewModel)line.Tag);
				}
			}
		}

		private void OnSnapToPixelsChanged()
		{
			foreach (var associationLines in _associationLines)
			{
				Debug.Assert(associationLines is not null);

				foreach (var line in associationLines.Lines)
				{
					Debug.Assert(line is not null);

					line.SnapsToDevicePixels = _optionsVm.SnapToPixels;

					_layer.RemoveAdornment(line);

					AddAdornment(line);
				}
			}
		}

		private void OnAssociationEnabledChanged(FileTypesAssociationViewModel associationVm)
		{
			Debug.Assert(associationVm is not null);

			if (!IsMatchingAssociation(associationVm, MatchingAssociationFlags.SkipEnabled))
			{
				return;
			}

			if (associationVm.Enabled)
			{
				AddAssociation(associationVm);
			}
			else
			{
				RemoveAssociation(associationVm);
			}
		}

		private void OnFileTypesChanged(FileTypesAssociationViewModel associationVm)
		{
			Debug.Assert(associationVm is not null);

			if (!IsMatchingAssociation(associationVm, MatchingAssociationFlags.SkipFileTypes))
			{
				return;
			}

			var hasAssociation = HasAssociationFileLines(associationVm);

			if (associationVm.Association.Matches(FileName))
			{
				if (!hasAssociation)
				{
					AddAssociation(associationVm);
				}
			}
			else
			{
				if (hasAssociation)
				{
					RemoveAssociation(associationVm);
				}
			}
		}

		private void OnGuideVisibleChanged(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			if (!IsMatchingGuide(associationVm, guideVm, MatchingGuideFlags.SkipVisible))
			{
				return;
			}

			if (guideVm.Visible)
			{
				AddGuide(associationVm, guideVm);
			}
			else
			{
				RemoveGuide(associationVm, guideVm);
			}
		}

		private void OnGuideColumnChanged(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			if (!IsMatchingGuide(associationVm, guideVm) || FindLine(associationVm, guideVm) is not Line line)
			{
				return;
			}

			var columnX = CalculateColumnXCoordinate(guideVm);

			line.X1 = columnX;
			line.X2 = columnX;
		}

		private void OnGuideColorChanged(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			if (!IsMatchingGuide(associationVm, guideVm) || FindLine(associationVm, guideVm) is not Line line)
			{
				return;
			}

			var brush = new SolidColorBrush(guideVm.Color);

			brush.Freeze();

			line.Stroke = brush;
		}

		private void OnGuideWidthChanged(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			if (!IsMatchingGuide(associationVm, guideVm) || FindLine(associationVm, guideVm) is not Line line)
			{
				return;
			}

			line.StrokeThickness = guideVm.Width;
		}

		private void OnGuideDashesChanged(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			if (!IsMatchingGuide(associationVm, guideVm) || FindLine(associationVm, guideVm) is not Line line)
			{
				return;
			}

			line.StrokeDashArray = guideVm.Dashes;
		}

		private bool IsMatchingAssociation(
			FileTypesAssociationViewModel associationVm,
			MatchingAssociationFlags flags = MatchingAssociationFlags.All)
		{
			Debug.Assert(associationVm is not null);

			return _optionsVm.ShowGuides &&
				(flags.HasFlag(MatchingAssociationFlags.SkipEnabled) || associationVm.Enabled) &&
				(associationVm.GuideVms.Count > 0) &&
				(flags.HasFlag(MatchingAssociationFlags.SkipFileTypes) ||
					associationVm.Association.Matches(FileName)) &&
				associationVm.GuideVms.Any(x => x.Visible);
		}

		private bool IsMatchingGuide(
			FileTypesAssociationViewModel associationVm,
			GuideViewModel guideVm,
			MatchingGuideFlags flags = MatchingGuideFlags.All)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			return _optionsVm.ShowGuides &&
				associationVm.Enabled &&
				(flags.HasFlag(MatchingGuideFlags.SkipVisible) || guideVm.Visible) &&
				associationVm.Association.Matches(FileName);
		}

		private void AddAssociation(FileTypesAssociationViewModel associationVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(IsMatchingAssociation(associationVm));
			Debug.Assert(!_associationLines.Any(x => x.AssociationVm == associationVm));

			var associationLinesIndex = AssociationLinesLowerBoundIndex(associationVm);

			RemoveAdornments(associationLinesIndex);

			_associationLines.Insert(associationLinesIndex, new(associationVm, CreateLine));

			AddAdornments(associationLinesIndex);
		}

		private void RemoveAssociation(FileTypesAssociationViewModel associationVm)
		{
			Debug.Assert(associationVm is not null);

			var associationLinesIndex = FindAssociationLinesIndex(associationVm);

			if (associationLinesIndex == -1)
			{
				return;
			}

			var associationLines = _associationLines[associationLinesIndex];

			Debug.Assert(associationLines != null);

			RemoveAdornments(associationLines);

			_associationLines.RemoveAt(associationLinesIndex);
		}

		private void AddGuide(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);
			Debug.Assert(IsMatchingGuide(associationVm, guideVm));

			var associationLinesIndex = AssociationLinesLowerBoundIndex(associationVm);

			var lineIndex = 0;

			var associationLines = (associationLinesIndex < _associationLines.Count) ?
				_associationLines[associationLinesIndex] : null;

			if ((associationLines is not null) && (associationLines.AssociationVm == associationVm))
			{
				lineIndex = associationLines.LineLowerBoundIndex(guideVm);
			}
			else
			{
				associationLines = new(associationVm);

				_associationLines.Insert(associationLinesIndex, associationLines);
			}

			Debug.Assert(!associationLines.Lines.Any(x => x.Tag == guideVm));

			RemoveAdornments(associationLinesIndex);

			associationLines.Lines.Insert(lineIndex, CreateLine(guideVm));

			AddAdornments(associationLinesIndex);
		}

		private void RemoveGuide(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			var (associationLinesIndex, lineIndex) = FindAssociationLinesAndLineIndexes(associationVm, guideVm);

			if ((associationLinesIndex == -1) || (lineIndex == -1))
			{
				return;
			}

			var associationLines = _associationLines[associationLinesIndex];

			Debug.Assert(associationLines is not null);

			var line = associationLines.Lines[lineIndex];

			Debug.Assert(line is not null);

			_layer.RemoveAdornment(line);

			associationLines.Lines.RemoveAt(lineIndex);

			if (associationLines.Lines.Count == 0)
			{
				_associationLines.RemoveAt(associationLinesIndex);
			}
		}

		private void AddAdornment(Line line)
		{
			Debug.Assert(line is not null);

			_layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, line, null);
		}

		private void RemoveAdornments(AssociationLines associationLines)
		{
			Debug.Assert(associationLines is not null);

			foreach (var line in associationLines.Lines)
			{
				Debug.Assert(line is not null);

				_layer.RemoveAdornment(line);
			}
		}

		private void AddAdornments(int firstAssociationLinesIndex, int firstLineIndex = 0)
		{
			if (firstAssociationLinesIndex < _associationLines.Count)
			{
				var partialLines = _associationLines[firstAssociationLinesIndex].Lines;

				Debug.Assert(partialLines is not null);

				for (var lineIndex = firstLineIndex; lineIndex < partialLines.Count; ++lineIndex)
				{
					Debug.Assert(partialLines[lineIndex] is not null);

					AddAdornment(partialLines[lineIndex]);
				}

				++firstAssociationLinesIndex;
			}

			for (var associationLinesIndex = firstAssociationLinesIndex;
				associationLinesIndex < _associationLines.Count;
				++associationLinesIndex)
			{
				Debug.Assert(_associationLines[associationLinesIndex] is not null);
				Debug.Assert(_associationLines[associationLinesIndex].Lines is not null);

				foreach (var line in _associationLines[associationLinesIndex].Lines)
				{
					Debug.Assert(line is not null);

					AddAdornment(line);
				}
			}
		}

		private void RemoveAdornments(int firstAssociationLinesIndex, int firstLineIndex = 0)
		{
			if (firstAssociationLinesIndex < _associationLines.Count)
			{
				var partialLines = _associationLines[firstAssociationLinesIndex].Lines;

				Debug.Assert(partialLines is not null);

				for (var lineIndex = firstLineIndex; lineIndex < partialLines.Count; ++lineIndex)
				{
					Debug.Assert(partialLines[lineIndex] is not null);

					_layer.RemoveAdornment(partialLines[lineIndex]);
				}

				++firstAssociationLinesIndex;
			}

			for (var associationLinesIndex = firstAssociationLinesIndex;
				associationLinesIndex < _associationLines.Count;
				++associationLinesIndex)
			{
				Debug.Assert(_associationLines[associationLinesIndex] is not null);
				Debug.Assert(_associationLines[associationLinesIndex].Lines is not null);

				foreach (var line in _associationLines[associationLinesIndex].Lines)
				{
					Debug.Assert(line is not null);

					_layer.RemoveAdornment(line);
				}
			}
		}

		private double CalculateColumnXCoordinate(GuideViewModel guideVm)
		{
			Debug.Assert(guideVm is not null);

			return ((_view.TextViewLines?.Count > 0) ?
				(_view.TextViewLines[0]?.Left ?? 0) : 0) + guideVm.Column * _columnWidth.Width;
		}

		private double CalculateStrokeDashOffset(GuideViewModel guideVm)
		{
			Debug.Assert(guideVm is not null);

			return (_optionsVm.StickToPage && (guideVm.Width > 0)) ? _view.ViewportTop / guideVm.Width : 0;
		}

		private Line CreateLine(GuideViewModel guideVm)
		{
			Debug.Assert(guideVm is not null);

			var columnX = CalculateColumnXCoordinate(guideVm);

			var brush = new SolidColorBrush(guideVm.Color);

			brush.Freeze();

			return new Line
			{
				Tag = guideVm,
				X1 = columnX,
				Y1 = _view.ViewportTop,
				X2 = columnX,
				Y2 = _view.ViewportBottom,
				Stroke = brush,
				StrokeDashArray = guideVm.Dashes,
				StrokeThickness = guideVm.Width,
				StrokeDashOffset = CalculateStrokeDashOffset(guideVm),
				SnapsToDevicePixels = _optionsVm.SnapToPixels
			};
		}

		private bool HasAssociationFileLines(FileTypesAssociationViewModel associationVm) =>
			_associationLines.Exists(x => x.AssociationVm == associationVm);

		private AssociationLines FindAssociationLines(FileTypesAssociationViewModel associationVm) =>
			_associationLines.Find(x => x.AssociationVm == associationVm);

		private int FindAssociationLinesIndex(FileTypesAssociationViewModel associationVm) =>
			_associationLines.FindIndex(x => x.AssociationVm == associationVm);

		private Line FindLine(FileTypesAssociationViewModel associationVm, GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			var associationLines = FindAssociationLines(associationVm);

			return associationLines?.FindLine(guideVm);
		}

		private int AssociationLinesLowerBoundIndex(FileTypesAssociationViewModel associationVm)
		{
			Debug.Assert(associationVm is not null);

			var associationLinesIndex = _associationLines.FindIndex(
				x => (x.AssociationVm?.Index ?? -1) >= associationVm.Index);

			return (associationLinesIndex >= 0) ? associationLinesIndex : _associationLines.Count;
		}

		private (int, int) FindAssociationLinesAndLineIndexes(
			FileTypesAssociationViewModel associationVm,
			GuideViewModel guideVm)
		{
			Debug.Assert(associationVm is not null);
			Debug.Assert(guideVm is not null);

			var associationLinesIndex = FindAssociationLinesIndex(associationVm);

			return (associationLinesIndex, (associationLinesIndex != -1) ?
				(_associationLines[associationLinesIndex]?.FindLineIndex(guideVm) ?? -1) : -1);
		}
	}
}
