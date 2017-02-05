using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RavenAdminLogsCollectionTool.Helpers;
using WebSocketSharp;

namespace RavenAdminLogsCollectionTool.Services
{
    public class RavenDbCommunicationService : IRavenDbCommunicationService, IDisposable
    {
        private readonly string _eventId = RandomIdGenerator.GenerateId();
        private WebSocket _webSocket;

        public async Task<string> ConnectAsync(string databaseUrl, EventHandler onOpen,
            EventHandler<CloseEventArgs> onClose, EventHandler<MessageEventArgs> onMessage, EventHandler<ErrorEventArgs> onError)
        {
            await ConfigureAdminLogsAsync(databaseUrl);
            return OpenWebSocket(databaseUrl, onOpen, onClose, onMessage, onError);
        }

        public void Disconnect()
        {
            _webSocket?.CloseAsync();
        }

        private async Task<string> ConfigureAdminLogsAsync(string databaseUrl)
        {
            string message = String.Empty;
            try
            {
                string url = $"{databaseUrl}/admin/logs/configure?watch-category=Raven.:Debug:no-watch-stack&id={_eventId}";
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(url, CancellationToken.None);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        message = response.ReasonPhrase;
                    }
                }
            }
            catch (WebException ex)
            {
                message = ex.Message;
            }
            return message;
        }

        private string OpenWebSocket(string databaseUrl, EventHandler onOpen,
            EventHandler<CloseEventArgs> onClose, EventHandler<MessageEventArgs> onMessage, EventHandler<ErrorEventArgs> onError)
        {
            try
            {
                _webSocket = new WebSocket(BuildWebSocketUrl(databaseUrl));
                _webSocket.OnOpen += onOpen;
                _webSocket.OnClose += onClose;
                _webSocket.OnMessage += onMessage;
                _webSocket.OnError += onError;
                _webSocket.ConnectAsync();

                return String.Empty;
            }
            catch (Exception ex)
            {
                return $"{ ex.Message} Websocket ReadyState: {_webSocket.ReadyState}";
            }
        }

        private string BuildWebSocketUrl(string databaseUrl)
        {
            var uri = new Uri(databaseUrl);
            return $"ws://{uri.Host}:{uri.Port}/admin/logs/events?id={_eventId}";
        }

        protected virtual void Dispose(bool disposing)
        {
            Dispose();
        }

        public void Dispose()
        {
            ((IDisposable) _webSocket).Dispose();
        }
    }
}
