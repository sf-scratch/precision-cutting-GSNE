using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace 精密切割系统.Behaviors
{
    public static class TextBoxFocusBehavior
    {
        public static readonly DependencyProperty GotFocusCommandProperty =
            DependencyProperty.RegisterAttached("GotFocusCommand", typeof(ICommand), typeof(TextBoxFocusBehavior),
                new PropertyMetadata(null, OnGotFocusCommandChanged));

        public static ICommand GetGotFocusCommand(TextBox textBox)
        {
            return (ICommand)textBox.GetValue(GotFocusCommandProperty);
        }

        public static void SetGotFocusCommand(TextBox textBox, ICommand value)
        {
            textBox.SetValue(GotFocusCommandProperty, value);
        }

        private static void OnGotFocusCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.GotFocus -= TextBox_GotFocus;

                if (e.NewValue is ICommand command)
                {
                    textBox.GotFocus += TextBox_GotFocus;
                }
            }
        }

        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            var command = GetGotFocusCommand(textBox);

            if (command?.CanExecute(textBox?.Name) == true)
            {
                command.Execute(textBox?.Name);
            }
        }
    }
}