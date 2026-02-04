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
using System;
using System.ComponentModel.Composition;
using System.Windows;

namespace ColumnGuides
{
	internal interface IOptionsViewModelService
	{
		OptionsViewModel OptionsViewModel { get; }
	}

	[Export(typeof(IOptionsViewModelService))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	internal sealed class OptionsViewModelService : IOptionsViewModelService
	{
		private readonly IOptionsService _optionsService;

		public OptionsViewModel OptionsViewModel { get; }

		[ImportingConstructor]
		public OptionsViewModelService(IOptionsService optionsService)
		{
			_optionsService = optionsService ?? throw new ArgumentNullException(nameof(optionsService));

			OptionsViewModel = new(optionsService.Options);

			WeakEventManager<IOptionsService, EventArgs>.AddHandler(
				_optionsService,
				nameof(IOptionsService.OptionsLoaded),
				OnOptionsLoaded);
		}

		private void OnOptionsLoaded(object sender, EventArgs e) => OptionsViewModel.Options = _optionsService.Options;
	}
}
