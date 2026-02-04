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
using System.ComponentModel;

namespace ColumnGuidesOptions
{
	public partial class FileTypesAssociationViewModel : ObservableObject
	{
		public event EventHandler<GuidePropertyChangedEventArgs> GuidePropertyChanged;

		[ObservableProperty]
		private int _index;

		[ObservableProperty]
		private bool _enabled;

		[ObservableProperty]
		private string _fileTypes;

		[ObservableProperty]
		private ObservableCollection<GuideViewModel> _guideVms = [];

		public FileTypesAssociation Association { get; }

		public FileTypesAssociationViewModel(int index, FileTypesAssociation association)
		{
			Association = association ?? throw new ArgumentNullException(nameof(association));

			_index = index;
			_enabled = association.Enabled;
			_fileTypes = association.FileTypes;

			var guideIndex = 0;

			foreach (var guide in association.Guides)
			{
				var guideVm = new GuideViewModel(guideIndex++, guide);

				guideVm.PropertyChanged += OnGuidePropertyChanged;

				_guideVms.Add(guideVm);
			}
		}

		internal void OnGuideVmAdded(GuideViewModel guideVm) => guideVm.PropertyChanged += OnGuidePropertyChanged;

		internal void OnGuideVmRemoved(GuideViewModel guideVm) => guideVm.PropertyChanged -= OnGuidePropertyChanged;

		partial void OnEnabledChanged(bool enabled) => Association.Enabled = enabled;

		partial void OnFileTypesChanged(string fileTypes) => Association.FileTypes = fileTypes;

		private void OnGuidePropertyChanged(object sender, PropertyChangedEventArgs e) =>
			GuidePropertyChanged?.Invoke(this, new(this, sender as GuideViewModel, e?.PropertyName));
	}
}
