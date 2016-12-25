using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenAdminLogsCollectionTool.Messages;
using RavenAdminLogsCollectionTool.Model;
using WebSocketSharp;
using LogLevel = RavenAdminLogsCollectionTool.Model.LogLevel;


namespace RavenAdminLogsCollectionTool.Services
{
    public class LogService : ILogService
    {
        private readonly IRavenDbCommunicationService _ravenDbCommunicationService;
        private readonly object _collectionLogsSyncObject = new object();
        private List<LogInfo> _filterLogs = new List<LogInfo>();
        private readonly List<LogInfo> _allLogs = new List<LogInfo>();

        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        public string Category { get; set; } = "Raven.";

        public LogService(IRavenDbCommunicationService ravenDbCommunicationService)
        {
            _ravenDbCommunicationService = ravenDbCommunicationService;
        }

        public async Task<string> ConnectAsync(string databaseUrl)
        {
            EventHandler onWebSocketOpen = (sender, args) =>
            {
                Messenger.Default.Send(new OpenWebSocketMessage());
            };
            EventHandler<CloseEventArgs> onWebSocketClose = (sender, args) => {
                Messenger.Default.Send(new CloseWebSocketMessage());
            };
            EventHandler<MessageEventArgs> onWebSocketReceiveMessage = (sender, args) => {
                OnWebSocketReceiveMessage(args);
            };
            EventHandler<ErrorEventArgs> onWebSocketError = (sender, args) =>
            {
                Messenger.Default.Send(new ErrorWebSocketMessage { ErrorMessage = args.Message });
            };
            await _ravenDbCommunicationService.ConnectAsync(databaseUrl, onWebSocketOpen,
                onWebSocketClose, onWebSocketReceiveMessage, onWebSocketError);
            return String.Empty;
        }

        public void Disconnect()
        {
            _ravenDbCommunicationService.Disconnect();
        }

        public void LogsClear()
        {
            lock (_collectionLogsSyncObject)
            {
                Messenger.Default.Send(new LogsMessage { FullLogText = String.Empty });
                _filterLogs.Clear();
                _allLogs.Clear();
            }
        }

        public bool IsAllLogsEmpty()
        {
            lock (_collectionLogsSyncObject)
            {
                return _allLogs.Count == 0;
            }
        }

        public bool IsFilterLogsEmpty()
        {
            lock (_collectionLogsSyncObject)
            {
                return _filterLogs.Count == 0;
            }
        }

        public void FilterLogs(LogLevel logLevel, string category)
        {
            lock (_collectionLogsSyncObject)
            {
                _filterLogs = new List<LogInfo>(_allLogs.Where(
                    logInfo => logInfo.Level >= logLevel && logInfo.LoggerName.Contains(category)));
                Messenger.Default.Send(new LogsMessage { FullLogText = LogsToString() });
            }
        }

        public string LogsToJsonString()
        {
            lock (_collectionLogsSyncObject)
            {
                return JsonConvert.SerializeObject(_filterLogs, Formatting.Indented);
            }
        }

        private void OnWebSocketReceiveMessage(MessageEventArgs args)
        {
            var jObject = JObject.Parse(args.Data);
            if (jObject == null || (jObject["Type"] != null && jObject["Type"].ToString() == "Heartbeat"))
            {
                return;
            }
            var logInfo = jObject.ToObject<LogInfo>();
            lock (_collectionLogsSyncObject)
            {
                if (logInfo.Level >= LogLevel && logInfo.LoggerName.Contains(Category))
                {
                    Messenger.Default.Send(new LogMessage { LogText = logInfo.ToString() });
                    _filterLogs.Add(logInfo);
                }
                _allLogs.Add(logInfo);
            }
            DispatcherHelper.CheckBeginInvokeOnUI(CommandManager.InvalidateRequerySuggested);
        }

        private string LogsToString()
        {
            if (_filterLogs.Count == 0)
            {
                return String.Empty;
            }
            var stringBuilder = new StringBuilder();
            foreach (var log in _filterLogs)
            {
                stringBuilder.Append(log);
            }
            return stringBuilder.ToString();
        }
    }
}
