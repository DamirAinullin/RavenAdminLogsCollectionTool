using System.Threading.Tasks;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionTool.Services
{
    public interface ILogService
    {
        Task<string> Connect(string databaseUrl, string category, LogLevel logLevel);
        string Disconnect();
        void LogsClear();
        bool IsAllLogsEmpty();
        bool IsShowLogsEmpty();
        void FilterLogs(LogLevel logLevel, string category);
        string LogsToJsonString();
    }
}