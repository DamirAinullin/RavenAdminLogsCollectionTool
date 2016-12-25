using System;
using System.Windows;
using System.Windows.Controls;

namespace RavenAdminLogsCollectionTool.Helpers
{
    public class TextBoxExtension
    {
        public static string GetMessageText(DependencyObject obj)
        {
            return (string)obj.GetValue(MessageTextProperty);
        }

        public static void SetMessageText(DependencyObject obj, string value)
        {
            obj.SetValue(MessageTextProperty, value);
        }

        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.RegisterAttached("MessageText", typeof(string), typeof(TextBoxExtension),
            new UIPropertyMetadata(String.Empty, OnTextBoxTextChanged));

        private static void OnTextBoxTextChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBox)dependencyObject;
            textBox.AppendText((string)e.NewValue);
        }
    }
}
