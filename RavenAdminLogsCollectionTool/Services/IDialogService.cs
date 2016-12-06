namespace RavenAdminLogsCollectionTool.Services
{
    public interface IDialogService
    {
        void ShowErrorMessage(string message);
        bool? ShowSaveFileDialog(out string fileName);
    }
}
