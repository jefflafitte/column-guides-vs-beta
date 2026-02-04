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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace ColumnGuides
{
	[ContentType("text")]
	[Export(typeof(IWpfTextViewCreationListener))]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class ColumnGuidesAdornmentTextViewCreationListener : IWpfTextViewCreationListener
	{
#pragma warning disable 169
		[Export]
		[Name(ColumnGuidesAdornment.Name)]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Caret)]
		private readonly AdornmentLayerDefinition columnGuidesAdornmentLayer;
#pragma warning restore 169

		[Import]
		private readonly IClassificationFormatMapService _formatMapService = null!;

		[Import]
		private readonly IOptionsViewModelService _optionsViewModelService = null!;

		public void TextViewCreated(IWpfTextView textView)
		{
			Debug.Assert(_formatMapService is not null);
			Debug.Assert(_optionsViewModelService is not null);
			Debug.Assert(textView is not null);

			textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(
				typeof(ITextDocument),
				out ITextDocument document);

			var formatMap = _formatMapService.GetClassificationFormatMap(textView);

			if ((document is not null) && (formatMap is not null))
			{
				textView.Properties.GetOrCreateSingletonProperty(() => new ColumnGuidesAdornment(
					textView,
					document,
					formatMap,
					_optionsViewModelService.OptionsViewModel));
			}
		}
	}
}
