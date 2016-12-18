using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<LogInfo> _logs = new ObservableCollection<LogInfo>();
        private readonly List<LogInfo> _allLogs = new List<LogInfo>();

        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        public string Category { get; set; } = "Raven.";

        public LogService(IRavenDbCommunicationService ravenDbCommunicationService)
        {
            _ravenDbCommunicationService = ravenDbCommunicationService;
            _logs.CollectionChanged += (sender, args) =>
            {
                _logs = (ObservableCollection<LogInfo>)sender;
                Messenger.Default.Send(new LogsMessage{ FullLogText = LogsToString() });
            };
        }

        public async Task<string> Connect(string databaseUrl)
        {
            string errorMessage = await _ravenDbCommunicationService.ConfigureAdminLogsAsync(databaseUrl);
            if (String.IsNullOrEmpty(errorMessage))
            {
                EventHandler<MessageEventArgs> onWebSocketReceiveMessage = (sender, args) => {
                    OnWebSocketReceiveMessage(args);
                };
                EventHandler<CloseEventArgs> onWebSocketClose = (sender, args) => {
                    Messenger.Default.Send(new CloseWebSocketMessage());
                };
                EventHandler<ErrorEventArgs> onWebSocketError = (sender, args) =>
                {
                    Messenger.Default.Send(new ErrorWebSocketMessage { ErrorMessage = args.Message });
                };
                EventHandler onWebSocketOpen = (sender, args) =>
                {
                    Messenger.Default.Send(new OpenWebSocketMessage());
                };
                errorMessage = _ravenDbCommunicationService.OpenWebSocket(databaseUrl,
                    onWebSocketOpen, onWebSocketClose, onWebSocketReceiveMessage, onWebSocketError);
            }
            return errorMessage;
        }

        public string Disconnect()
        {
            return _ravenDbCommunicationService.CloseWebSocket();
        }

        public void LogsClear()
        {
            lock (_collectionLogsSyncObject)
            {
                _logs.Clear();
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

        public bool IsShowLogsEmpty()
        {
            lock (_collectionLogsSyncObject)
            {
                return _logs.Count == 0;
            }
        }

        public void FilterLogs(LogLevel logLevel, string category)
        {
            lock (_collectionLogsSyncObject)
            {
                var logs = new ObservableCollection<LogInfo>(_allLogs.Where(
                    logInfo => logInfo.Level >= logLevel && logInfo.LoggerName.Contains(category)));
                _logs.Clear();
                foreach (var log in logs)
                {
                    _logs.Add(log);
                }
            }
        }

        public string LogsToJsonString()
        {
            return JsonConvert.SerializeObject(_logs, Formatting.Indented);
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
                    _logs.Add(logInfo);
                }
                _allLogs.Add(logInfo);
            }
            DispatcherHelper.CheckBeginInvokeOnUI(CommandManager.InvalidateRequerySuggested);
        }

        private string LogsToString()
        {
            lock (_collectionLogsSyncObject)
            {
                if (_logs.Count == 0)
                {
                    return String.Empty;
                }
                var stringBuilder = new StringBuilder();
                foreach (var log in _logs)
                {
                    stringBuilder.Append(log);
                }
                return stringBuilder.ToString();
            }
        }
    }
}
