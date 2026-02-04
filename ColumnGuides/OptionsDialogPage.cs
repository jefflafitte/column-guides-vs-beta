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

using Microsoft.VisualStudio.ComponentModelHost;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ColumnGuides
{
	[Guid("6869AC17-142E-4836-A92F-F0ED8F917014")]
	internal sealed class OptionsDialogPage : DialogPageBase
	{
		private IOptionsService _optionsService;
		private IOptionsViewModelService _optionsViewModeService;
		private OptionsUserControl _userControl;

		private IOptionsService OptionsService =>
			_optionsService ??= ((IComponentModel)GetService(
				typeof(SComponentModel))).GetService<IOptionsService>();

		private IOptionsViewModelService OptionsViewModelService =>
			_optionsViewModeService ??= ((IComponentModel)GetService(
				typeof(SComponentModel))).GetService<IOptionsViewModelService>();

		private OptionsUserControl UserControl => _userControl ??= new();

		protected override System.Windows.UIElement Child => UserControl;

		protected override void OnActivate(CancelEventArgs e)
		{
			base.OnActivate(e);

			UserControl.DataContext = OptionsViewModelService.OptionsViewModel;
		}

		public override void LoadSettingsFromStorage() => OptionsService.LoadOptionsFromStorage();

		public override void SaveSettingsToStorage()
		{
			UpdateBindings(UserControl);

			OptionsService.SaveOptionsToStorage();
		}

		private void UpdateBindings(DependencyObject parent)
		{
			int childCount = VisualTreeHelper.GetChildrenCount(parent);

			for (var childIndex = 0; childIndex < childCount; ++childIndex)
			{
				var child = VisualTreeHelper.GetChild(parent, childIndex);

				if (child is TextBox textBox)
				{
					textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
				}

				UpdateBindings(child);
			}
		}
	}
}
