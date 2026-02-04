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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ColumnGuides
{
	internal static class SelectAllOnFocusBehavior
	{
		public static readonly DependencyProperty SelectAllOnFocusProperty =
			DependencyProperty.RegisterAttached(
				"SelectAllOnFocus",
				typeof(bool),
				typeof(SelectAllOnFocusBehavior),
				new(false, OnPropertyChanged));

		public static bool GetSelectAllOnFocus(DependencyObject d) =>
			(bool)d.GetValue(SelectAllOnFocusProperty);

		public static void SetSelectAllOnFocus(DependencyObject d, bool value) =>
			d.SetValue(SelectAllOnFocusProperty, value);

		private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not TextBox textBox)
			{
				return;
			}

			if ((bool)e.NewValue)
			{
				textBox.GotKeyboardFocus += OnGotKeyboardFocus;
				textBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
			}
			else
			{
				textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
				textBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
			}
		}

		private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
			(sender as TextBox)?.SelectAll();

		private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if ((sender is TextBox textBox) && !textBox.IsFocused)
			{
				textBox.Focus();

				e.Handled = true;
			}
		}
	}
}
