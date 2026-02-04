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
using System.ComponentModel.Design;

namespace ColumnGuides
{
	internal sealed class OptionsCommand
	{
		private readonly AsyncPackage _package;

		public OptionsCommand(
			Guid commandSet,
			int commandId,
			AsyncPackage package,
			OleMenuCommandService commandService)
		{
			_package = package ?? throw new ArgumentNullException(nameof(package));

			_ = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuItem = new MenuCommand(Execute, new CommandID(commandSet, commandId));

			commandService.AddCommand(menuItem);
		}

		private void Execute(object sender, EventArgs e) => _package.ShowOptionPage(typeof(OptionsDialogPage));
	}
}
