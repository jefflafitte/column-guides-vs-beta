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

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ColumnGuides
{
	public partial class OptionsUserControl : UserControl
	{
		private const int _minColumnValue = 0;

		private const int _minColorValue = 0;
		private const int _maxColorValue = 255;

		private static readonly char[] _invalidFileTypesCharacters = [ '<', '>', ':', '"', '/', '\\', '|' ];

		internal OptionsUserControl() => InitializeComponent();

		private static bool ValidateNumericInput(TextBox textBox, string input, int minValue, int maxValue)
		{
			if (textBox == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(input))
			{
				return true;
			}

			maxValue = Math.Max(maxValue, minValue);

			if ((minValue >= 0) && !input.All(char.IsDigit))
			{
				return false;
			}

			var newText = textBox.Text.Substring(0, textBox.SelectionStart) +
				input +
				textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

			if ((newText.Length > 1) && (newText[0] == '0'))
			{
				return false;
			}

			if (!int.TryParse(newText, out var newValue))
			{
				return false;
			}

			return (newValue >= minValue) && (newValue <= maxValue);
		}

		private void Color_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string)))
			{
				if (!ValidateNumericInput(
					sender as TextBox,
					e.DataObject.GetData(typeof(string)) as string,
					_minColorValue, _maxColorValue))
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}

		private void Color_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = !ValidateNumericInput(sender as TextBox, e.Text, _minColorValue, _maxColorValue);
		}

		private void Column_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string)))
			{
				if (!ValidateNumericInput(
					sender as TextBox,
					e.DataObject.GetData(typeof(string)) as string,
					_minColumnValue,
					ColumnGuidesOptions.DefaultSettings.Instance.MaxGuideColumn))
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}

		private void Column_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = !ValidateNumericInput(
				sender as TextBox,
				e.Text,
				_minColumnValue,
				ColumnGuidesOptions.DefaultSettings.Instance.MaxGuideColumn);
		}

		private void FileTypes_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string)))
			{
				var s = e.DataObject.GetData(typeof(string)) as string;

				if (!string.IsNullOrEmpty(s) && s.Any(c => _invalidFileTypesCharacters.Contains(c)))
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}

		private void FileTypes_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = e.Text.Any(c => _invalidFileTypesCharacters.Contains(c));
		}

		private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var listBox = sender as ListBox;

			if ((listBox is not null) && (listBox.Items.Count > 0) && (listBox.SelectedValue is null))
			{
				listBox.SelectedValue = (e.RemovedItems.Count > 0) ? e.RemovedItems[0] : listBox.Items[0];
			}
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if ((sender is TextBox textBox) &&
				string.IsNullOrEmpty(textBox.Text) &&
				(textBox.GetBindingExpression(TextBox.TextProperty) is BindingExpression binding))
			{
				binding.UpdateTarget();
			}
        }
    }
}
