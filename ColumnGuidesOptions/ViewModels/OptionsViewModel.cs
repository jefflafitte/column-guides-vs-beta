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
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;

namespace ColumnGuidesOptions
{
	public partial class OptionsViewModel : ObservableObject
	{
		public event EventHandler OptionsReset;

		public event EventHandler<AssociationAddedEventArgs> AssociationAdded;
		public event EventHandler<AssociationRemovedEventArgs> AssociationRemoved;
		public event EventHandler<AssociationMovedEventArgs> AssociationMoved;
		public event EventHandler<AssociationPropertyChangedEventArgs> AssociationPropertyChanged;

		public event EventHandler<GuideAddedEventArgs> GuideAdded;
		public event EventHandler<GuideRemovedEventArgs> GuideRemoved;
		public event EventHandler<GuideMovedEventArgs> GuideMoved;
		public event EventHandler<GuidePropertyChangedEventArgs> GuidePropertyChanged;

		private Options _options;

		public bool OptionsResetting { get; private set; } = false;

		[ObservableProperty]
		private bool _showGuides;

		[ObservableProperty]
		private bool _stickToPage;

		[ObservableProperty]
		private bool _snapToPixels;

		[ObservableProperty]
		private ObservableCollection<FileTypesAssociationViewModel> _associationVms = [];

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(AddGuideCommand))]
		[NotifyCanExecuteChangedFor(nameof(RemoveFileTypesCommand))]
		[NotifyCanExecuteChangedFor(nameof(MoveFileTypesUpCommand))]
		[NotifyCanExecuteChangedFor(nameof(MoveFileTypesDownCommand))]
		private FileTypesAssociationViewModel _selectedAssociationVm;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RemoveGuideCommand))]
		[NotifyCanExecuteChangedFor(nameof(MoveGuideUpCommand))]
		[NotifyCanExecuteChangedFor(nameof(MoveGuideDownCommand))]
		[NotifyCanExecuteChangedFor(nameof(ChooseColorCommand))]
		private GuideViewModel _selectedGuideVm;

		[ObservableProperty]
		private int[] _customColors = [];

		public Options Options
		{
			get => _options;

			set
			{
				if (_options != value)
				{
					OptionsResetting = true;

					_options = value;

					SyncToModel();

					OptionsReset?.Invoke(this, EventArgs.Empty);

					OptionsResetting = false;
				}
			}
		}

		public OptionsViewModel(Options options) =>
			Options = options ?? throw new ArgumentNullException(nameof(options));

		partial void OnShowGuidesChanged(bool showGuides) => Options.ShowGuides = showGuides;

		partial void OnStickToPageChanged(bool stickToPage) => Options.StickToPage = stickToPage;

		partial void OnSnapToPixelsChanged(bool snapToPixels) => Options.SnapToPixels = snapToPixels;

		partial void OnSelectedAssociationVmChanged(FileTypesAssociationViewModel associationVm) =>
			SelectedGuideVm = ((associationVm is not null) && (associationVm.GuideVms.Count > 0)) ?
				associationVm.GuideVms[0] : null;

		partial void OnCustomColorsChanged(int[] customColors) => Options.CustomColors = customColors;

		private void OnAssociationPropertyChanged(object sender, PropertyChangedEventArgs e) =>
			AssociationPropertyChanged?.Invoke(this, new(sender as FileTypesAssociationViewModel, e?.PropertyName));

		private void OnGuidePropertyChanged(object sender, GuidePropertyChangedEventArgs e) =>
			GuidePropertyChanged?.Invoke(this, e);

		private void SyncToModel()
		{
			ShowGuides = Options.ShowGuides;
			StickToPage = Options.StickToPage;
			SnapToPixels = Options.SnapToPixels;

			foreach (var associationVm in AssociationVms)
			{
				associationVm.PropertyChanged -= OnAssociationPropertyChanged;
				associationVm.GuidePropertyChanged -= OnGuidePropertyChanged;
			}

			AssociationVms.Clear();

			var associationIndex = 0;

			foreach (var association in Options.Associations)
			{
				var associationVm = new FileTypesAssociationViewModel(associationIndex++, association);

				associationVm.PropertyChanged += OnAssociationPropertyChanged;
				associationVm.GuidePropertyChanged += OnGuidePropertyChanged;

				AssociationVms.Add(associationVm);
			}

			SelectedAssociationVm = (AssociationVms.Count > 0) ? AssociationVms[0] : null;

			CustomColors = Options.CustomColors;
		}

		[RelayCommand(CanExecute = nameof(CanExecuteAddFileTypes))]
		private void AddFileTypes()
		{
			var associationVm = new FileTypesAssociationViewModel(
				(SelectedAssociationVm is not null) ? (SelectedAssociationVm.Index + 1) : AssociationVms.Count,
				new FileTypesAssociation
				{
					Enabled = DefaultSettings.Instance.NewAssociationEnabled,
					FileTypes = DefaultSettings.Instance.NewAssociationFileTypes
				});

			associationVm.PropertyChanged += OnAssociationPropertyChanged;
			associationVm.GuidePropertyChanged += OnGuidePropertyChanged;

			Options.Associations.Insert(associationVm.Index, associationVm.Association);

			AssociationVms.Insert(associationVm.Index, associationVm);

			for (var associationIndex = associationVm.Index + 1;
				associationIndex < AssociationVms.Count;
				++associationIndex)
			{
				AssociationVms[associationIndex].Index = associationIndex;
			}

			AssociationAdded?.Invoke(this, new(associationVm));

			SelectedAssociationVm = associationVm;

			if (DefaultSettings.Instance.NewAssociationAddGuide)
			{
				AddGuide();
			}

			AddFileTypesCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteAddFileTypes() => AssociationVms.Count < DefaultSettings.Instance.MaxAssociationCount;

		[RelayCommand(CanExecute = nameof(CanExecuteRemoveFileTypes))]
		private void RemoveFileTypes()
		{
			Debug.Assert(CanExecuteRemoveFileTypes());

			SelectedAssociationVm.PropertyChanged -= OnAssociationPropertyChanged;
			SelectedAssociationVm.GuidePropertyChanged -= OnGuidePropertyChanged;

			var associationVm = SelectedAssociationVm;

			Options.Associations.Remove(associationVm.Association);

			AssociationVms.Remove(associationVm);

			for (var associationIndex = associationVm.Index;
				associationIndex < AssociationVms.Count;
				++associationIndex)
			{
				AssociationVms[associationIndex].Index = associationIndex;
			}

			AssociationRemoved?.Invoke(this, new(associationVm));

			SelectedAssociationVm = (AssociationVms.Count > 0) ?
				AssociationVms[Math.Min(associationVm.Index, AssociationVms.Count - 1)] : null;

			AddFileTypesCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteRemoveFileTypes() => SelectedAssociationVm is not null;

		[RelayCommand(CanExecute = nameof(CanExecuteMoveFileTypesUp))]
		private void MoveFileTypesUp()
		{
			Debug.Assert(CanExecuteMoveFileTypesUp());

			Options.Associations.RemoveAt(SelectedAssociationVm.Index);
			Options.Associations.Insert(SelectedAssociationVm.Index - 1, SelectedAssociationVm.Association);

			AssociationVms.Move(SelectedAssociationVm.Index, SelectedAssociationVm.Index - 1);

			AssociationVms[SelectedAssociationVm.Index].Index = SelectedAssociationVm.Index;

			--SelectedAssociationVm.Index;

			AssociationMoved?.Invoke(this, new(SelectedAssociationVm));

			MoveFileTypesUpCommand.NotifyCanExecuteChanged();
			MoveFileTypesDownCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteMoveFileTypesUp() =>
			(AssociationVms.Count > 1) && (SelectedAssociationVm is not null) && (SelectedAssociationVm.Index > 0);

		[RelayCommand(CanExecute = nameof(CanExecuteMoveFileTypesDown))]
		private void MoveFileTypesDown()
		{
			Debug.Assert(CanExecuteMoveFileTypesDown());

			Options.Associations.RemoveAt(SelectedAssociationVm.Index);
			Options.Associations.Insert(SelectedAssociationVm.Index + 1, SelectedAssociationVm.Association);

			AssociationVms.Move(SelectedAssociationVm.Index, SelectedAssociationVm.Index + 1);

			AssociationVms[SelectedAssociationVm.Index].Index = SelectedAssociationVm.Index;

			++SelectedAssociationVm.Index;

			AssociationMoved?.Invoke(this, new(SelectedAssociationVm));

			MoveFileTypesUpCommand.NotifyCanExecuteChanged();
			MoveFileTypesDownCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteMoveFileTypesDown() =>
			(AssociationVms.Count > 1) &&
			(SelectedAssociationVm is not null) &&
			(SelectedAssociationVm.Index < (AssociationVms.Count - 1));

		[RelayCommand(CanExecute = nameof(CanExecuteAddGuide))]
		private void AddGuide()
		{
			Debug.Assert(CanExecuteAddGuide());

			var guideVm = new GuideViewModel(
				(SelectedGuideVm is not null) ? (SelectedGuideVm.Index + 1) : SelectedAssociationVm.GuideVms.Count,
				new Guide
				{
					Visible = DefaultSettings.Instance.NewGuideVisible,
					Column = DefaultSettings.Instance.NewGuideColumn,
					Color = Color.FromArgb(
						DefaultSettings.Instance.NewGuideColor.A,
						DefaultSettings.Instance.NewGuideColor.R,
						DefaultSettings.Instance.NewGuideColor.G,
						DefaultSettings.Instance.NewGuideColor.B),
					Width = DefaultSettings.Instance.NewGuideWidth
				});

			SelectedAssociationVm.Association.Guides.Insert(guideVm.Index, guideVm.Guide);

			SelectedAssociationVm.GuideVms.Insert(guideVm.Index, guideVm);

			for (var guideIndex = guideVm.Index + 1;
				guideIndex < SelectedAssociationVm.GuideVms.Count;
				++guideIndex)
			{
				SelectedAssociationVm.GuideVms[guideIndex].Index = guideIndex;
			}

			SelectedAssociationVm.OnGuideVmAdded(guideVm);

			GuideAdded?.Invoke(this, new(SelectedAssociationVm, guideVm));

			SelectedGuideVm = guideVm;

			AddGuideCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteAddGuide() =>
			(SelectedAssociationVm is not null) &&
			(SelectedAssociationVm.GuideVms.Count < DefaultSettings.Instance.MaxAssociationGuideCount);

		[RelayCommand(CanExecute = nameof(CanExecuteRemoveGuide))]
		private void RemoveGuide()
		{
			Debug.Assert(CanExecuteRemoveGuide());

			var guideVm = SelectedGuideVm;

			SelectedAssociationVm.Association.Guides.Remove(guideVm.Guide);

			SelectedAssociationVm.GuideVms.Remove(guideVm);

			SelectedAssociationVm.OnGuideVmRemoved(guideVm);

			for (var guideIndex = guideVm.Index; guideIndex < SelectedAssociationVm.GuideVms.Count; ++guideIndex)
			{
				SelectedAssociationVm.GuideVms[guideIndex].Index = guideIndex;
			}

			GuideRemoved?.Invoke(this, new(SelectedAssociationVm, guideVm));

			SelectedGuideVm = (SelectedAssociationVm.GuideVms.Count > 0) ?
				SelectedAssociationVm.GuideVms[Math.Min(guideVm.Index, SelectedAssociationVm.GuideVms.Count - 1)] :
				null;

			AddGuideCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteRemoveGuide() => (SelectedAssociationVm is not null) && (SelectedGuideVm is not null);

		[RelayCommand(CanExecute = nameof(CanExecuteMoveGuideUp))]
		private void MoveGuideUp()
		{
			Debug.Assert(CanExecuteMoveGuideUp());

			Debug.Assert(SelectedGuideVm.Index > 0);

			SelectedAssociationVm.Association.Guides.RemoveAt(SelectedGuideVm.Index);
			SelectedAssociationVm.Association.Guides.Insert(SelectedGuideVm.Index - 1, SelectedGuideVm.Guide);

			SelectedAssociationVm.GuideVms.Move(SelectedGuideVm.Index, SelectedGuideVm.Index - 1);

			SelectedAssociationVm.GuideVms[SelectedGuideVm.Index].Index = SelectedGuideVm.Index;

			--SelectedGuideVm.Index;

			GuideMoved?.Invoke(this, new(SelectedAssociationVm, SelectedGuideVm));

			MoveGuideUpCommand.NotifyCanExecuteChanged();
			MoveGuideDownCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteMoveGuideUp() =>
			(SelectedAssociationVm is not null) &&
			(SelectedGuideVm is not null) &&
			(SelectedAssociationVm.GuideVms.Count > 1) &&
			(SelectedGuideVm.Index > 0);

		[RelayCommand(CanExecute = nameof(CanExecuteMoveGuideDown))]
		private void MoveGuideDown()
		{
			Debug.Assert(CanExecuteMoveGuideDown());

			Debug.Assert(SelectedGuideVm.Index < (SelectedAssociationVm.GuideVms.Count - 1));

			SelectedAssociationVm.Association.Guides.RemoveAt(SelectedGuideVm.Index);
			SelectedAssociationVm.Association.Guides.Insert(SelectedGuideVm.Index + 1, SelectedGuideVm.Guide);

			SelectedAssociationVm.GuideVms.Move(SelectedGuideVm.Index, SelectedGuideVm.Index + 1);

			SelectedAssociationVm.GuideVms[SelectedGuideVm.Index].Index = SelectedGuideVm.Index;

			++SelectedGuideVm.Index;

			GuideMoved?.Invoke(this, new(SelectedAssociationVm, SelectedGuideVm));

			MoveGuideUpCommand.NotifyCanExecuteChanged();
			MoveGuideDownCommand.NotifyCanExecuteChanged();
		}

		private bool CanExecuteMoveGuideDown() =>
			(SelectedAssociationVm is not null) &&
			(SelectedGuideVm is not null) &&
			(SelectedAssociationVm.GuideVms.Count > 1) &&
			(SelectedGuideVm.Index < (SelectedAssociationVm.GuideVms.Count - 1));

		[RelayCommand(CanExecute = nameof(CanExecuteChooseColor))]
		private void ChooseColor()
		{
			Debug.Assert(CanExecuteChooseColor());

			var colorDialog = new ColorDialog
			{
				AllowFullOpen = true,
				Color = SelectedGuideVm.GetRgbAsSystemDrawingColor(),
				CustomColors = CustomColors
			};

			if (colorDialog.ShowDialog() == DialogResult.OK)
			{
				SelectedGuideVm.SetRgbFromSystemDrawingColor(colorDialog.Color);

				if (!CustomColors.SequenceEqual(colorDialog.CustomColors))
				{
					CustomColors = colorDialog.CustomColors;
				}
			}
		}

		private bool CanExecuteChooseColor() => (SelectedAssociationVm is not null) && (SelectedGuideVm is not null);
	}

	public abstract class AssociationEventArgs(FileTypesAssociationViewModel associationVm) : EventArgs
	{
		public FileTypesAssociationViewModel AssociationVm { get; } =
			associationVm ?? throw new ArgumentNullException(nameof(associationVm));
	}

	public sealed class AssociationAddedEventArgs(FileTypesAssociationViewModel associationVm) :
		AssociationEventArgs(associationVm)
	{ }

	public sealed class AssociationRemovedEventArgs(FileTypesAssociationViewModel associationVm) :
		AssociationEventArgs(associationVm)
	{ }

	public sealed class AssociationMovedEventArgs(FileTypesAssociationViewModel associationVm) :
		AssociationEventArgs(associationVm)
	{ }

	public sealed class AssociationPropertyChangedEventArgs(
			FileTypesAssociationViewModel associationVm,
			string propertyName) : PropertyChangedEventArgs(propertyName)
	{
		public FileTypesAssociationViewModel AssociationVm { get; } =
			associationVm ?? throw new ArgumentNullException(nameof(associationVm));
	}

	public abstract class GuideEventArgs(
		FileTypesAssociationViewModel associationVm,
		GuideViewModel guideVm) : AssociationEventArgs(associationVm)
	{
		public GuideViewModel GuideVm { get; } = guideVm ?? throw new ArgumentNullException(nameof(guideVm));
	}

	public sealed class GuideAddedEventArgs(
		FileTypesAssociationViewModel associationVm,
		GuideViewModel guideVm) : GuideEventArgs(associationVm, guideVm)
	{ }

	public sealed class GuideRemovedEventArgs(
		FileTypesAssociationViewModel associationVm,
		GuideViewModel guideVm) : GuideEventArgs(associationVm, guideVm)
	{ }

	public sealed class GuideMovedEventArgs(
		FileTypesAssociationViewModel associationVm,
		GuideViewModel guideVm) : GuideEventArgs(associationVm, guideVm)
	{ }

	public sealed class GuidePropertyChangedEventArgs(
			FileTypesAssociationViewModel associationVm,
			GuideViewModel guideVm,
			string propertyName) : PropertyChangedEventArgs(propertyName)
	{
		public FileTypesAssociationViewModel AssociationVm { get; } =
			associationVm ?? throw new ArgumentNullException(nameof(associationVm));
		public GuideViewModel GuideVm { get; } = guideVm ?? throw new ArgumentNullException(nameof(guideVm));
	}
}
