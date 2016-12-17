using System.Windows;

namespace RavenAdminLogsCollectionTool.Services
{
    public class DialogService : IDialogService
    {
        public void ShowMessage(string message, string caption = "Information")
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowErrorMessage(string message, string caption = "Error")
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
