using System.IO;

namespace RavenAdminLogsCollectionTool.Services
{
    public class FileSystemService : IFileSystemService
    {
        public void SaveFile(string path, string content)
        {
            File.WriteAllText(path, content);
        }
    }
}
