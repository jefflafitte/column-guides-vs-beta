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
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace ColumnGuidesOptions
{
	public sealed class OptionsConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			=> (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
			=> (destinationType == typeof(string)) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			object result = null;

			if (value is string s && !string.IsNullOrEmpty(s))
			{
				try
				{
					result = JsonSerializer.Deserialize<Options>(s);
				}
				catch (Exception ex)
				{
					ActivityLog.TryLogWarning("ColumnGuides", $"Failed to read options from JSON: {ex.Message}");
				}
			}
			else
			{
				result = base.ConvertFrom(context, culture, value);
			}

			return result;
		}

		public override object ConvertTo(
			ITypeDescriptorContext context,
			CultureInfo culture,
			object value,
			Type destinationType) =>
			((value is Options options) && (destinationType == typeof(string))) ?
				JsonSerializer.Serialize(options) : base.ConvertTo(context, culture, value, destinationType);
	}
}
