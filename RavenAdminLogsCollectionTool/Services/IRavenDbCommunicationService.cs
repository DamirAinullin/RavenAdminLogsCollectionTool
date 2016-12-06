using System.Collections.Generic;
using System.Threading.Tasks;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionTool.Services
{
    public interface IRavenDbCommunicationService
    {
        Task<string> ConfigureAdminLogsAsync(string databaseUrl);
        Task<string> OpenWebSocketAsync(string databaseUrl);
        Task<string> CloseWebSocketAsync();
        IList<LogInfo> GetNewLogsAndClear();
    }
}
