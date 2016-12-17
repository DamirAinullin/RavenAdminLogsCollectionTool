using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using RavenAdminLogsCollectionTool.Helpers;
using RavenAdminLogsCollectionTool.Services;
using LogLevel = RavenAdminLogsCollectionTool.Model.LogLevel;

namespace RavenAdminLogsCollectionTool.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogService _logService;
        private readonly IDialogService _dialogService;
        private readonly IFileSystemService _fileSystemService;
        private readonly IConfigurationService _configurationService;
        private string _fullLogText = string.Empty;
        private string _databaseUrl;
        private string _category;
        private ICommand _logsClearCommand;
        private ICommand _saveToFileCommand;
        private ICommand _connectCommand;
        private ICommand _disconnectCommand;
        private ICommand _windowLoadedCommand;
        private ICommand _filterLogsCommand;
        private ICommand _keepDownCommand;
        private LogLevel _logLevel;
        private bool _connectIsEnabled = true;
        private bool _disconnectIsEnabled;
        private bool _isAutoScrollEnabled;

        public MainViewModel(ILogService logService, IDialogService dialogService, IFileSystemService fileSystemService, IConfigurationService configurationService)
        {
            _logService = logService;
            _dialogService = dialogService;
            _fileSystemService = fileSystemService;
            _configurationService = configurationService;

            Messenger.Default.Register<OpenWebSocketMessage>(this, message =>
            {
                DisconnectIsEnabled = true;
                ConnectIsEnabled = false;
            });
            Messenger.Default.Register<CloseWebSocketMessage>(this, message =>
            {
                DisconnectIsEnabled = false;
                ConnectIsEnabled = true;
            });
            Messenger.Default.Register<ErrorWebSocketMessage>(this, message =>
            {
                _dialogService.ShowErrorMessage(message.ErrorMessage);
            });
            Messenger.Default.Register<LogsMessage>(this, message =>
            {
                FullLogText = message.FullLogText;
            });
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

        public bool IsAutoScrollEnabled
        {
            get { return _isAutoScrollEnabled; }
            set { Set(ref _isAutoScrollEnabled, value); }
        }

        public ICommand LogsClearCommand
        {
            get
            {
                return _logsClearCommand ?? (_logsClearCommand = new RelayCommand(() =>
                    {
                        _logService.LogsClear();
                    }, () => !_logService.IsAllLogsEmpty()));
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
                        string message = await _logService.Connect(DatabaseUrl, Category, LogLevel);
                        if (!String.IsNullOrEmpty(message))
                        {
                            ConnectIsEnabled = true;
                            DisconnectIsEnabled = false;
                            _dialogService.ShowErrorMessage("Network error has occurred. " + message);
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
                        string message = _logService.Disconnect();
                        if (!String.IsNullOrEmpty(message))
                        {
                            _dialogService.ShowErrorMessage("Websocket error has occurred. " + message);
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
                        _logService.FilterLogs(selectedValue, Category);
                    }));
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return _saveToFileCommand ?? (_saveToFileCommand = new RelayCommand(() =>
                    {
                        string jsonString = _logService.LogsToJsonString();
                        string filePath = _fileSystemService.SaveLogFile(jsonString);
                        _dialogService.ShowMessage("Log file has been saved as " + filePath, "File saved");
                    }, () => !_logService.IsShowLogsEmpty()));
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
                        string isAutoScrollEnabledStr = _configurationService.GetValue("IsAutoScrollEnabled");
                        if (!String.IsNullOrEmpty(url))
                        {
                            DatabaseUrl = url;
                        }
                        if (!String.IsNullOrEmpty(category))
                        {
                            Category = category;
                        }
                        bool isAutoScrollEnabled;
                        if (Boolean.TryParse(isAutoScrollEnabledStr, out isAutoScrollEnabled))
                        {
                            IsAutoScrollEnabled = isAutoScrollEnabled;
                        }
                    }));
            }
        }

        public ICommand KeepDownCommand
        {
            get
            {
                return _keepDownCommand ?? (_keepDownCommand = new RelayCommand<bool>(isEnabled =>
                    {
                        IsAutoScrollEnabled = AutoScrollBehavior.IsEnabled = isEnabled;
                        _configurationService.SetValue("IsAutoScrollEnabled", IsAutoScrollEnabled.ToString());
                    }));
            }
        }

        private bool CheckDatabaseUrl()
        {
            Uri uriResult;
            return String.IsNullOrEmpty(DatabaseUrl) || (Uri.TryCreate(DatabaseUrl, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps));
        }
    }
}