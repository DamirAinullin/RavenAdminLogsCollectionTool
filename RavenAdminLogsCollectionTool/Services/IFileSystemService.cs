namespace RavenAdminLogsCollectionTool.Services
{
    public interface IFileSystemService
    {
        void SaveFile(string path, string content);
    }
}
