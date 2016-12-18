using System.Threading.Tasks;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionTool.Services
{
    public interface ILogService
    {
        Task<string> Connect(string databaseUrl);
        string Disconnect();
        void LogsClear();
        bool IsAllLogsEmpty();
        bool IsShowLogsEmpty();
        void FilterLogs(LogLevel logLevel, string category);
        string LogsToJsonString();

        LogLevel LogLevel { get; set; }
        string Category { get; set; }
    }
}