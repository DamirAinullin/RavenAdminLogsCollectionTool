using System.Collections.Generic;
using System.Threading.Tasks;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionTool.Services
{
    public interface ILogService
    {
        Task<string> ConnectAsync(string databaseUrl);
        void Disconnect();
        void LogsClear();
        bool IsAllLogsEmpty();
        bool IsFilterLogsEmpty();
        void FilterLogs(LogLevel logLevel, string category);
        string LogsToJsonString();
        void LoadLogs(List<LogInfo> logs, LogLevel logLevel, string category);

        LogLevel LogLevel { get; set; }
        string Category { get; set; }

    }
}