using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Newtonsoft.Json;
using RavenAdminLogsCollectionTool.Model;
using RavenAdminLogsCollectionTool.Services;

namespace RavenAdminLogsCollectionTool.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IRavenDbCommunicationService _ravenDbCommunicationService;
        private readonly IDialogService _dialogService;
        private readonly IFileSystemService _fileSystemService;
        private readonly IConfigurationService _configurationService;
        private readonly Timer _logsTimer = new Timer();
        private readonly object _collectionLogsSyncObject = new object();
        private readonly List<LogInfo> _allLogs = new List<LogInfo>();
        private ObservableCollection<LogInfo> _logs = new ObservableCollection<LogInfo>();
        private string _fullLogText = string.Empty;
        private string _databaseUrl = string.Empty;
        private ICommand _logsClearCommand;
        private ICommand _saveToFileCommand;
        private ICommand _connectCommand;
        private LogLevel _logLevel;
        private bool _connectIsEnabled = true;
        private bool _disconnectIsEnabled;
        private ICommand _filterLogsCommand;
        private RelayCommand _disconnectCommand;
        private RelayCommand _windowLoadedCommand;

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
                            return Logs.Count != 0;
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
                        DisconnectIsEnabled = true;
                        _configurationService.SetDatabaseUrl(DatabaseUrl);
                        string message = await _ravenDbCommunicationService.ConfigureAdminLogsAsync(DatabaseUrl);
                        if (String.IsNullOrEmpty(message))
                        {
                            var webSocketMessage = await _ravenDbCommunicationService.OpenWebSocketAsync(DatabaseUrl);
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
                return _disconnectCommand ?? (_disconnectCommand = new RelayCommand(async () =>
                    {
                        string message = await _ravenDbCommunicationService.CloseWebSocketAsync();
                        if (!String.IsNullOrEmpty(message))
                        {
                            _dialogService.ShowErrorMessage(message);
                        }
                        DisconnectIsEnabled = false;
                        ConnectIsEnabled = true;
                    }, () => DisconnectIsEnabled));
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
                            Logs = new ObservableCollection<LogInfo>(_allLogs.Where(logInfo => logInfo.LogLevel >= selectedValue));
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
                        string url = _configurationService.GetDatabaseUrl();
                        if (!String.IsNullOrEmpty(url))
                        {
                            DatabaseUrl = url;
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

        private void LogTimerCallback(object obj, ElapsedEventArgs args)
        {
            var newLogs = _ravenDbCommunicationService.GetNewLogsAndClear();
            if (newLogs.Count == 0)
            {
                return;
            }
            lock (_collectionLogsSyncObject)
            {
                foreach (var log in newLogs)
                {
                    if (log.LogLevel >= LogLevel)
                    {
                        Logs.Add(log);
                    }
                    _allLogs.Add(log);
                }
                Logs = Logs;
            }
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

        public MainViewModel(IRavenDbCommunicationService ravenDbCommunicationService,
            IDialogService dialogService, IFileSystemService fileSystemService, IConfigurationService configurationService)
        {
            _ravenDbCommunicationService = ravenDbCommunicationService;
            _dialogService = dialogService;
            _fileSystemService = fileSystemService;
            _configurationService = configurationService;

            _logsTimer.Interval = 5000;
            _logsTimer.Elapsed += LogTimerCallback;
            _logsTimer.AutoReset = true;
            _logsTimer.Enabled = true;
            _logsTimer.Start();
        }

        public override void Cleanup()
        {
            ((IDisposable) _ravenDbCommunicationService)?.Dispose();
            _logsTimer?.Dispose();
            base.Cleanup();
        }
    }
}