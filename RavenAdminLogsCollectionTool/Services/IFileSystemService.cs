using System.Collections.Generic;
using System.Threading.Tasks;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionTool.Services
{
    public interface IFileSystemService
    {
        string SaveLogFile(string content);
        bool LogFileExists();
        Task<List<LogInfo>> LoadLogsFromFileAsync();
        void SaveLogMessageToFile(LogInfo logInfo);
    }
}
