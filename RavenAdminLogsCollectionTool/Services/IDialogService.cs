namespace RavenAdminLogsCollectionTool.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message, string caption = "Information");
        void ShowErrorMessage(string message, string caption = "Error");
    }
}
