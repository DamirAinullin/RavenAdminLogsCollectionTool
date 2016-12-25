using System.Windows;
using System.Windows.Controls;

namespace RavenAdminLogsCollectionTool.Helpers
{
    public static class FocusExtension
    {
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached(
            "IsFocused", typeof(bool), typeof(FocusExtension), new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        private static void OnIsFocusedPropertyChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBox)dependencyObject;
            if ((bool)e.NewValue)
            {
                textBox.Focus();
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }
}
