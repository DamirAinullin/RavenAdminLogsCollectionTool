using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using RavenAdminLogsCollectionTool.Helpers;
using RavenAdminLogsCollectionTool.Messages;
using RavenAdminLogsCollectionTool.Model;
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
        private string _messageText;
        private string _databaseUrl;
        private string _category;
        private bool _connectIsEnabled = true;
        private bool _disconnectIsEnabled;
        private bool _autoScrollEnabled = true;
        private bool _databaseUrlIsFocused;
        private bool _streamToFileIsEnabled;
        private LogLevel _logLevel;
        private ICommand _logsClearCommand;
        private ICommand _saveToFileCommand;
        private ICommand _connectCommand;
        private ICommand _disconnectCommand;
        private ICommand _windowLoadedCommand;
        private ICommand _filterLogsCommand;
        private ICommand _keepDownCommand;
        private ICommand _openLogFileCommand;
        private ICommand _streamToFileCommand;

        public MainViewModel(ILogService logService, IDialogService dialogService, IFileSystemService fileSystemService,
            IConfigurationService configurationService)
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
            Messenger.Default.Register<LogInfo>(this, message =>
            {
                MessageText = message.ToString();
                if (StreamToFileIsEnabled)
                {
                    Task.Factory.StartNew(() => {
                         _fileSystemService.SaveLogMessageToFile(message);
                    });
                }
                _fullLogText += message.ToString();
            });
        }

        public string MessageText
        {
            get { return _messageText; }
            set { Set(ref _messageText, value); }
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
                _logService.Category = _category;
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

        public bool DatabaseUrlIsFocused
        {
            get { return _databaseUrlIsFocused; }
            set { Set(ref _databaseUrlIsFocused, value); }
        }

        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set
            {
                Set(ref _logLevel, value);
                _logService.LogLevel = _logLevel;
            }
        }

        public string FullLogText
        {
            get { return _fullLogText; }
            set { Set(ref _fullLogText, value); }
        }

        public bool AutoScrollEnabled
        {
            get { return _autoScrollEnabled; }
            set { Set(ref _autoScrollEnabled, value); }
        }



        public bool StreamToFileIsEnabled
        {
            get { return _streamToFileIsEnabled; }
            set { Set(ref _streamToFileIsEnabled, value); }
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
                        try
                        {
                            await _logService.ConnectAsync(DatabaseUrl);
                        }
                        catch (Exception ex)
                        {
                            ConnectIsEnabled = true;
                            DisconnectIsEnabled = false;
                            _dialogService.ShowErrorMessage("Network error has occurred. " + ex.Message);
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
                        try
                        {
                            _logService.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            _dialogService.ShowErrorMessage("Websocket error has occurred. " + ex.Message);
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
                    }, () => !StreamToFileIsEnabled && !_logService.IsFilterLogsEmpty()));
            }
        }

        public ICommand OpenLogFileCommand
        {
            get
            {
                return _openLogFileCommand ?? (_openLogFileCommand = new RelayCommand(async () =>
                {
                    var logs = await _fileSystemService.LoadLogsFromFileAsync();
                    _logService.LoadLogs(logs, LogLevel, Category);
                }, () => !StreamToFileIsEnabled && _fileSystemService.LogFileExists()));
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
                        string autoScrollEnabledStr = _configurationService.GetValue("AutoScrollEnabled");
                        if (!String.IsNullOrEmpty(url))
                        {
                            DatabaseUrl = url;
                        }
                        if (!String.IsNullOrEmpty(category))
                        {
                            Category = category;
                        }
                        bool autoScrollEnabled;
                        if (Boolean.TryParse(autoScrollEnabledStr, out autoScrollEnabled))
                        {
                            AutoScrollEnabled = AutoScrollBehavior.IsEnabled = autoScrollEnabled;
                        }
                        DatabaseUrlIsFocused = true;
                    }));
            }
        }

        public ICommand KeepDownCommand
        {
            get
            {
                return _keepDownCommand ?? (_keepDownCommand = new RelayCommand<bool>(isEnabled =>
                    {
                        AutoScrollEnabled = AutoScrollBehavior.IsEnabled = isEnabled;
                        _configurationService.SetValue("AutoScrollEnabled", AutoScrollEnabled.ToString());
                    }));
            }
        }

        public ICommand StreamToFileCommand
        {
            get
            {
                return _streamToFileCommand ?? (_streamToFileCommand = new RelayCommand<bool>(isEnabled =>
                {
                    StreamToFileIsEnabled = isEnabled;
                }));
            }
        }

        private bool CheckDatabaseUrl()
        {
            Uri uriResult;
            return String.IsNullOrEmpty(DatabaseUrl) || Uri.TryCreate(DatabaseUrl, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}