using System;
using System.Windows;
using Microsoft.Win32;

namespace RavenAdminLogsCollectionTool.Services
{
    public class DialogService : IDialogService
    {
        public void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool? ShowSaveFileDialog(out string fileName)
        {
            var dlg = new SaveFileDialog
            {
                FileName = "logs",
                DefaultExt = ".json",
                Filter = "Json documents (.json)|*.json"
            };
            if (dlg.ShowDialog() == true)
            {
                fileName = dlg.FileName;
                return true;
            }
            fileName = String.Empty;
            return false;
        }
    }
}
