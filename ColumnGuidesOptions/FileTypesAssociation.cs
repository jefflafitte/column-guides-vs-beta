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

using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Generic;
using System.Linq;

namespace ColumnGuidesOptions
{
	public sealed class FileTypesAssociation
	{
		private string _fileTypes;

		private List<Guide> _guides;

		private Matcher _matcher;

		public bool Enabled { get; set; } = false;

		public string FileTypes
		{
			get => _fileTypes ??= "";

			set
			{
				if (_fileTypes != value)
				{
					_fileTypes = value;

					_matcher = null;

					if (!string.IsNullOrEmpty(_fileTypes))
					{
						var fileTypes = _fileTypes.Split(';').
							Select(s => s.Trim()).
							Where(s => !string.IsNullOrWhiteSpace(s));

						if (fileTypes.Any())
						{
							_matcher = new();

							foreach (var fileType in fileTypes)
							{
								_matcher.AddInclude(fileType);
							}
						}
					}
				}
			}
		}

		public List<Guide> Guides
		{
			get => _guides ??= [];

			set => _guides = value;
		}

		public bool Matches(string fileName) => _matcher?.Match(fileName).HasMatches ?? false;
	}
}
