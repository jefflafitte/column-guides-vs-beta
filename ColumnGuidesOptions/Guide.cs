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

using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;
using System.Windows.Media;

namespace ColumnGuidesOptions
{
	public sealed class Guide
	{
		private DoubleCollection _dashes;

		public bool Visible { get; set; } = false;

		public int Column { get; set; } = 0;

		public Color Color { get; set; } = System.Windows.Media.Colors.Black;

		public int Width { get; set; } = 1;

		public DoubleCollection Dashes
		{
			get => _dashes ??= [];

			set => _dashes = value;
		}

		public Guide Clone() => new Guide
		{
			Visible = Visible,
			Column = Column,
			Color = Color,
			Width = Width,
			Dashes = Dashes.Clone()
		};
	}
}
