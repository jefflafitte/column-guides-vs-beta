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
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Windows;

namespace ColumnGuides
{
	internal sealed class ToggleShowGuidesCommand
	{
		private readonly IOptionsService _optionsService;
		private readonly OptionsViewModel _optionsViewModel;
		private readonly bool _showGuides;
		private readonly OleMenuCommand _menuItem;

		public ToggleShowGuidesCommand(
			Guid commandSet,
			int commandId,
			OleMenuCommandService commandService,
			IOptionsService optionsService,
			OptionsViewModel optionsViewModel,
			bool showGuides)
		{
			_ = commandService ?? throw new ArgumentNullException(nameof(commandService));

			_optionsService = optionsService;
			_optionsViewModel = optionsViewModel;
			_showGuides = showGuides;

			_menuItem = new OleMenuCommand(Execute, new CommandID(commandSet, commandId));

			SetMenuItemVisibility();

			commandService.AddCommand(_menuItem);

			WeakEventManager<OleMenuCommand, EventArgs>.AddHandler(
				_menuItem,
				nameof(OleMenuCommand.BeforeQueryStatus),
				OnBeforeQueryStatus);
		}

		private void Execute(object sender, EventArgs e)
		{
			_optionsViewModel.ShowGuides = _showGuides;

			_optionsService.SaveOptionsToStorage();
		}

		private void OnBeforeQueryStatus(object sender, EventArgs e) => SetMenuItemVisibility();

		private void SetMenuItemVisibility() => _menuItem.Visible = _showGuides ^ _optionsViewModel.ShowGuides;
	}
}
