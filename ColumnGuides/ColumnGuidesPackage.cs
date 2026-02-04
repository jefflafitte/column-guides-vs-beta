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

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ColumnGuides
{
	[Guid("234ec81a-6741-4710-a0b7-e8a17aa52558")]
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideOptionPage(typeof(OptionsDialogPage), "Column Guides", "General", 0, 0, true)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	internal sealed class ColumnGuidesPackage : AsyncPackage
	{
		private static readonly Guid guidColumnGuidesCmdSet = new("dce540bc-103c-442d-a277-974a3368e660");

		private const int cmdidShowGuides = 0x0100;
		private const int cmdidHideGuides = 0x0101;
		private const int cmdidOptions = 0x0102;

		protected override async Task InitializeAsync(
			CancellationToken cancellationToken,
			IProgress<ServiceProgressData> progress)
		{
			await base.InitializeAsync(cancellationToken, progress);

			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

			Assumes.Present(commandService);

			var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;

			Assumes.Present(componentModel);

			var optionsService = componentModel.GetService<IOptionsService>();

			Assumes.Present(optionsService);

			var optionsViewModelService = componentModel.GetService<IOptionsViewModelService>();

			Assumes.Present(optionsViewModelService);

			optionsService.LoadOptionsFromStorage();

			new ToggleShowGuidesCommand(
				guidColumnGuidesCmdSet,
				cmdidShowGuides,
				commandService,
				optionsService,
				optionsViewModelService.OptionsViewModel,
				true);

			new ToggleShowGuidesCommand(
				guidColumnGuidesCmdSet,
				cmdidHideGuides,
				commandService,
				optionsService,
				optionsViewModelService.OptionsViewModel,
				false);

			new OptionsCommand(
				guidColumnGuidesCmdSet,
				cmdidOptions,
				this,
				commandService);
		}
	}
}
