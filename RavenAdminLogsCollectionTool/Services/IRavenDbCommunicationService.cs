using System;
using System.Threading.Tasks;
using WebSocketSharp;

namespace RavenAdminLogsCollectionTool.Services
{
    public interface IRavenDbCommunicationService
    {
        Task<string> ConfigureAdminLogsAsync(string databaseUrl);

        string OpenWebSocket(string databaseUrl, EventHandler onOpen,
            EventHandler<CloseEventArgs> onClose, EventHandler<MessageEventArgs> onMessage,
            EventHandler<ErrorEventArgs> onError);

        string CloseWebSocket();
    }
}
