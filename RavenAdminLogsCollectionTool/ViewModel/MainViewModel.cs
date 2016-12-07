using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenAdminLogsCollectionTool.Model;
using RavenAdminLogsCollectionTool.Services;
using WebSocketSharp;
using LogLevel = RavenAdminLogsCollectionTool.Model.LogLevel;

namespace RavenAdminLogsCollectionTool.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IRavenDbCommunicationService _ravenDbCommunicationService;
        private readonly IDialogService _dialogService;
        private readonly IFileSystemService _fileSystemService;
        private readonly IConfigurationService _configurationService;
        private readonly object _collectionLogsSyncObject = new object();
        private readonly List<LogInfo> _allLogs = new List<LogInfo>();
        private ObservableCollection<LogInfo> _logs = new ObservableCollection<LogInfo>();
        private string _fullLogText = string.Empty;
        private string _databaseUrl;
        private string _category;
        private ICommand _logsClearCommand;
        private ICommand _saveToFileCommand;
        private ICommand _connectCommand;
        private ICommand _disconnectCommand;
        private ICommand _windowLoadedCommand;
        private ICommand _filterLogsCommand;
        private LogLevel _logLevel;
        private bool _connectIsEnabled = true;
        private bool _disconnectIsEnabled;

        private readonly EventHandler _onWebSocketOpen;
        private readonly EventHandler<CloseEventArgs> _onWebSocketClose;
        private readonly EventHandler<MessageEventArgs> _onWebSocketReceiveMessage;
        private readonly EventHandler<ErrorEventArgs> _onWebSocketError;


        public MainViewModel(IRavenDbCommunicationService ravenDbCommunicationService, IDialogService dialogService,
            IFileSystemService fileSystemService, IConfigurationService configurationService)
        {
            _ravenDbCommunicationService = ravenDbCommunicationService;
            _dialogService = dialogService;
            _fileSystemService = fileSystemService;
            _configurationService = configurationService;
            _onWebSocketReceiveMessage = (sender, args) => {
                var jObject = JObject.Parse(args.Data);
                if (jObject == null || (jObject["Type"] != null && jObject["Type"].ToString() == "Heartbeat"))
                {
                    return;
                }
                var logInfo = jObject.ToObject<LogInfo>();
                lock (_collectionLogsSyncObject)
                {
                    if (logInfo.LogLevel >= LogLevel && logInfo.LoggerName.Contains(Category))
                    {
                        Logs.Add(logInfo);
                    }
                    _allLogs.Add(logInfo);
                    Logs = Logs;
                }
            };
            _onWebSocketClose = (sender, args) => {
                DisconnectIsEnabled = false;
                ConnectIsEnabled = true;
            };
            _onWebSocketError = (sender, args) => { _dialogService.ShowErrorMessage(args.Message); };
            _onWebSocketOpen = (sender, args) => {
                DisconnectIsEnabled = true;
                ConnectIsEnabled = false;
            };
        }

        public string DatabaseUrl
        {
            get { return _databaseUrl; }
            set
            {
                Set(ref _databaseUrl, value);
                if (!CheckDatabaseUrl())
                {
                    throw new ValidationException("Invalid URL");
                }
            }
        }

        public string Category
        {
            get { return _category; }
            set
            {
                Set(ref _category, value);
                FilterLogsCommand.Execute(null);
            }
        }

        public bool ConnectIsEnabled
        {
            get { return _connectIsEnabled; }
            set { Set(ref _connectIsEnabled, value); }
        }

        public bool DisconnectIsEnabled
        {
            get { return _disconnectIsEnabled; }
            set { Set(ref _disconnectIsEnabled, value); }
        }

        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set { Set(ref _logLevel, value); }
        }

        public string FullLogText
        {
            get { return _fullLogText; }
            set { Set(ref _fullLogText, value); }
        }

        public ObservableCollection<LogInfo> Logs
        {
            get { return _logs; }
            set
            {
                Set(ref _logs, value);
                FullLogText = LogsToString();
            }
        }

        public ICommand LogsClearCommand
        {
            get
            {
                return _logsClearCommand ?? (_logsClearCommand = new RelayCommand(() =>
                    {
                        lock (_collectionLogsSyncObject)
                        {
                            Logs.Clear();
                            Logs = Logs;
                            _allLogs.Clear();
                        }
                    }, () =>
                    {
                        lock (_collectionLogsSyncObject)
                        {
                            return _allLogs.Count != 0;
                        }
                    }));
            }
        }

        public ICommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new RelayCommand(async () =>
                    {
                        ConnectIsEnabled = false;
                        _configurationService.SetValue("DatabaseUrl", DatabaseUrl);
                        _configurationService.SetValue("Category", Category);
                        string message = await _ravenDbCommunicationService.ConfigureAdminLogsAsync(DatabaseUrl);
                        if (String.IsNullOrEmpty(message))
                        {
                            var webSocketMessage = _ravenDbCommunicationService.OpenWebSocket(DatabaseUrl,
                                _onWebSocketOpen, _onWebSocketClose, _onWebSocketReceiveMessage, _onWebSocketError);
                            if (!String.IsNullOrEmpty(webSocketMessage))
                            {
                                _dialogService.ShowErrorMessage(webSocketMessage);
                                ConnectIsEnabled = true;
                                DisconnectIsEnabled = false;
                            }
                        }
                        else
                        {
                            _dialogService.ShowErrorMessage("Network error has occurred. " + message);
                            ConnectIsEnabled = true;
                            DisconnectIsEnabled = false;
                        }
                    }, () => !String.IsNullOrEmpty(DatabaseUrl) && CheckDatabaseUrl()));
            }
        }

        public ICommand DisconnectCommand
        {
            get
            {
                return _disconnectCommand ?? (_disconnectCommand = new RelayCommand(() =>
                    {
                        DisconnectIsEnabled = false;
                        string message = _ravenDbCommunicationService.CloseWebSocket();
                        if (!String.IsNullOrEmpty(message))
                        {
                            _dialogService.ShowErrorMessage(message);
                        }
                    }));
            }
        }

        public ICommand FilterLogsCommand
        {
            get
            {
                return _filterLogsCommand ?? (_filterLogsCommand = new RelayCommand<LogLevel>(selectedValue =>
                    {
                        lock (_collectionLogsSyncObject)
                        {
                            Logs = new ObservableCollection<LogInfo>(_allLogs.Where(
                                logInfo => logInfo.LogLevel >= selectedValue && logInfo.LoggerName.Contains(Category)));
                        }
                    }));
            }
        }

        public ICommand SaveToFileCommand
        {
            get
            {
                return _saveToFileCommand ?? (_saveToFileCommand = new RelayCommand(() =>
                    {
                        string fileName;
                        if (_dialogService.ShowSaveFileDialog(out fileName) == true)
                        {
                            _fileSystemService.SaveFile(fileName, JsonConvert.SerializeObject(Logs, Formatting.Indented));
                        }
                    }, () =>
                    {
                        lock (_collectionLogsSyncObject)
                        {
                            return Logs.Count != 0;
                        }
                    }));
            }
        }

        public ICommand WindowLoadedCommand
        {
            get
            {
                return _windowLoadedCommand ?? (_windowLoadedCommand = new RelayCommand(() =>
                    {
                        string url = _configurationService.GetValue("DatabaseUrl");
                        string category = _configurationService.GetValue("Category");
                        if (!String.IsNullOrEmpty(url))
                        {
                            DatabaseUrl = url;
                        }
                        if (!String.IsNullOrEmpty(category))
                        {
                            Category = category;
                        }
                    }));
            }
        }

        private bool CheckDatabaseUrl()
        {
            Uri uriResult;
            return String.IsNullOrEmpty(DatabaseUrl) || (Uri.TryCreate(DatabaseUrl, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps));
        }

        private string LogsToString()
        {
            lock (_collectionLogsSyncObject)
            {
                if (Logs.Count == 0)
                {
                    return String.Empty;
                }
                var stringBuilder = new StringBuilder();
                foreach (var log in Logs)
                {
                    stringBuilder.Append(log);
                }
                return stringBuilder.ToString();
            }
        }
    }
}