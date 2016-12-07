using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RavenAdminLogsCollectionTool.Helpers;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionTool.Services
{
    public class RavenDbCommunicationService : IRavenDbCommunicationService, IDisposable
    {
        private static readonly UTF8Encoding Encoder = new UTF8Encoding();
        private static readonly object CollectionLogsSyncObject = new object();
        private const int ReceiveChunkSize = 16384;
        private readonly IList<LogInfo> _logs = new List<LogInfo>();
        private readonly string _eventId = RandomIdGenerator.GenerateId();
        private ClientWebSocket _webSocket;

        public async Task<string> ConfigureAdminLogsAsync(string databaseUrl)
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

        public async Task<string> OpenWebSocketAsync(string databaseUrl)
        {
            string webSocketUrl = BuildWebSocketUrl(databaseUrl);
            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);
                await ReceiveData(_webSocket);
                return String.Empty;
            }
            catch (Exception ex)
            {
                return $"{ ex.Message} Websocket close status: {_webSocket.CloseStatus}";
            }
            finally
            {
                _webSocket?.Dispose();
            }
        }

        public async Task<string> CloseWebSocketAsync()
        {
            try
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, CancellationToken.None);
                }
                return String.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            finally
            {
                _webSocket?.Dispose();
            }
        }

        public IList<LogInfo> GetNewLogsAndClear()
        {
            lock (CollectionLogsSyncObject)
            {
                if (_logs.Count == 0)
                {
                    return new List<LogInfo>();
                }
                var logs = _logs.Select(loginInfo => new LogInfo(loginInfo.LogLevel, loginInfo.Database, loginInfo.TimeStamp,
                    loginInfo.Message, loginInfo.LoggerName, loginInfo.Exception, loginInfo.StackTrace)).ToList();
                _logs.Clear();
                return logs;
            }
        }

        private string BuildWebSocketUrl(string databaseUrl)
        {
            var uri = new Uri(databaseUrl);
            return $"ws://{uri.Host}:{uri.Port}/admin/logs/events?id={_eventId}";
        }

        private async Task ReceiveData(ClientWebSocket webSocket)
        {
            byte[] buffer = new byte[ReceiveChunkSize];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, String.Empty, CancellationToken.None);
                }
                else
                {
                    AddToLog(buffer, result.Count);
                }
            }
        }

        private void AddToLog(byte[] buffer, int length)
        {
            string jsonString = Encoder.GetString(buffer, 0, length);
            var jObject = JObject.Parse(jsonString);
            if (jObject == null || (jObject["Type"] != null && jObject["Type"].ToString() == "Heartbeat"))
            {
                return;
            }
            var logInfo = jObject.ToObject<LogInfo>();
            lock (CollectionLogsSyncObject)
            {
                _logs.Add(logInfo);
            }
        }

        public void Dispose()
        {
            if (_webSocket == null)
            {
                return;
            }
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
            {
                _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, CancellationToken.None).Wait();
            }
            _webSocket.Dispose();
            _webSocket = null;
        }
    }
}
