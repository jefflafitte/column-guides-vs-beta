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

using System.Collections.Generic;

namespace ColumnGuidesOptions
{
	public sealed class Options
	{
		private List<FileTypesAssociation> _associations;

		private int[] _customColors;

		public bool ShowGuides { get; set; }

		public bool StickToPage { get; set; }

		public bool SnapToPixels { get; set; }

		public List<FileTypesAssociation> Associations
		{
			get => _associations ??= [];

			set => _associations = value;
		}

		public int[] CustomColors
		{
			get => _customColors ??= [];

			set => _customColors = value;
		}
	}
}
