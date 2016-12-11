using System;
using System.ComponentModel.DataAnnotations;
using Moq;
using NUnit.Framework;
using RavenAdminLogsCollectionTool.Model;
using RavenAdminLogsCollectionTool.Services;
using RavenAdminLogsCollectionTool.ViewModel;
using WebSocketSharp;
using LogLevel = RavenAdminLogsCollectionTool.Model.LogLevel;

namespace RavenAdminLogsCollectionToolTests.ViewModel
{
    [TestFixture]
    public class MainViewModelTests
    {
        private Mock<IRavenDbCommunicationService> _ravenDbCommunicationServiceMock;
        private Mock<IDialogService> _dialogServiceMock;
        private Mock<IFileSystemService> _fileSystemServiceMock;
        private Mock<IConfigurationService> _configurationServiceMock;

        [SetUp]
        public void TestInitialize()
        {
            _ravenDbCommunicationServiceMock = new Mock<IRavenDbCommunicationService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _fileSystemServiceMock = new Mock<IFileSystemService>();
            _configurationServiceMock = new Mock<IConfigurationService>();
        }

        [Test]
        public void LogsClearCommandTest()
        {
            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsFalse(mainViewModel.LogsClearCommand.CanExecute(null));

            var logInfo = new LogInfo
            {
                LogLevel = LogLevel.Debug,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "LoggerName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = "TimeStamp"
            };
            mainViewModel.AllLogs.Add(logInfo);
            mainViewModel.Logs.Add(logInfo);

            Assert.IsTrue(mainViewModel.LogsClearCommand.CanExecute(null));

            mainViewModel.LogsClearCommand.Execute(null);

            Assert.IsEmpty(mainViewModel.Logs);
            Assert.IsEmpty(mainViewModel.AllLogs);
        }

        [Test]
        public void ConnectCommandTest()
        {
            _configurationServiceMock.Setup(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()));
            _ravenDbCommunicationServiceMock.Setup(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(String.Empty);
            _ravenDbCommunicationServiceMock.Setup(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>())).Returns(String.Empty);

            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsFalse(mainViewModel.ConnectCommand.CanExecute(null));

            Assert.Catch<ValidationException>(() => { mainViewModel.DatabaseUrl = "IncorrectDatabaseUrl"; });

            Assert.IsFalse(mainViewModel.ConnectCommand.CanExecute(null));

            mainViewModel.DatabaseUrl = "http://localhost:8080";

            Assert.IsTrue(mainViewModel.ConnectCommand.CanExecute(null));

            mainViewModel.ConnectCommand.Execute(null);

            Assert.IsFalse(mainViewModel.ConnectIsEnabled);
            _configurationServiceMock.Verify(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _ravenDbCommunicationServiceMock.Verify(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()), Times.Once);
            _ravenDbCommunicationServiceMock.Verify(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Once);
            _dialogServiceMock.Verify(m => m.ShowErrorMessage(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ConnectCommandShowErrorMessageIfConfigureAdminLogsMessageIsNotEmptyTest()
        {
            _configurationServiceMock.Setup(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()));
            _ravenDbCommunicationServiceMock.Setup(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()))
                .ReturnsAsync("Something wrong happened");
            _ravenDbCommunicationServiceMock.Setup(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>())).Returns(String.Empty);

            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object) {DatabaseUrl = "http://localhost:8080"};


            mainViewModel.ConnectCommand.Execute(null);

            _configurationServiceMock.Verify(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _ravenDbCommunicationServiceMock.Verify(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()), Times.Once);
            _ravenDbCommunicationServiceMock.Verify(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Never);
            _dialogServiceMock.Verify(m => m.ShowErrorMessage("Network error has occurred. Something wrong happened"), Times.Once);
            Assert.IsTrue(mainViewModel.ConnectIsEnabled);
            Assert.IsFalse(mainViewModel.DisconnectIsEnabled);
        }

        [Test]
        public void ConnectCommandShowErrorMessageIfOpenWebSocketMessageIsNotEmptyTest()
        {
            _configurationServiceMock.Setup(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()));
            _ravenDbCommunicationServiceMock.Setup(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(String.Empty);
            _ravenDbCommunicationServiceMock.Setup(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>())).Returns("Something wrong happened");

            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object) {DatabaseUrl = "http://localhost:8080"};

            mainViewModel.ConnectCommand.Execute(null);

            _configurationServiceMock.Verify(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _ravenDbCommunicationServiceMock.Verify(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()), Times.Once);
            _ravenDbCommunicationServiceMock.Verify(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Once);
            _dialogServiceMock.Verify(m => m.ShowErrorMessage("Websocket error has occurred. Something wrong happened"), Times.Once);
            Assert.IsTrue(mainViewModel.ConnectIsEnabled);
            Assert.IsFalse(mainViewModel.DisconnectIsEnabled);
        }

        [Test]
        public void DiconnectCommandTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.CloseWebSocket()).Returns(String.Empty);
            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsTrue(mainViewModel.DisconnectCommand.CanExecute(null));

            mainViewModel.DisconnectCommand.Execute(null);

            _ravenDbCommunicationServiceMock.Verify(m => m.CloseWebSocket(), Times.Once);
            _dialogServiceMock.Verify(m => m.ShowErrorMessage(It.IsAny<string>()), Times.Never);
            Assert.IsFalse(mainViewModel.DisconnectIsEnabled);
        }

        [Test]
        public void DiconnectCommandShowErrorMessageIfOpenWebSocketMessageIsNotEmptyTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.CloseWebSocket()).Returns("Something wrong happened");
            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            mainViewModel.DisconnectCommand.Execute(null);

            _ravenDbCommunicationServiceMock.Verify(m => m.CloseWebSocket(), Times.Once);
            _dialogServiceMock.Verify(m => m.ShowErrorMessage("Websocket error has occurred. Something wrong happened"), Times.Once);
            Assert.IsFalse(mainViewModel.DisconnectIsEnabled);
        }

        [Test]
        public void FilterLogsCommandTest()
        {
            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsTrue(mainViewModel.FilterLogsCommand.CanExecute(null));

            var logInfo1 = new LogInfo
            {
                LogLevel = LogLevel.Debug,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "LoggerName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = "TimeStamp"
            };
            var logInfo2 = new LogInfo
            {
                LogLevel = LogLevel.Error,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "LoggerName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = "TimeStamp"
            };
            var logInfo3 = new LogInfo
            {
                LogLevel = LogLevel.Warning,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "LoggerName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = "TimeStamp"
            };
            var logInfo4 = new LogInfo
            {
                LogLevel = LogLevel.Warning,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "OtherName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = "TimeStamp"
            };

            mainViewModel.AllLogs.Add(logInfo1);
            mainViewModel.AllLogs.Add(logInfo2);
            mainViewModel.AllLogs.Add(logInfo3);
            mainViewModel.AllLogs.Add(logInfo4);

            mainViewModel.Category = "LoggerName";
            mainViewModel.FilterLogsCommand.Execute(LogLevel.Warning);

            Assert.AreEqual(4, mainViewModel.AllLogs.Count);
            Assert.AreEqual(2, mainViewModel.Logs.Count);
        }

        [Test]
        public void SaveToFileCommandTest()
        {
            // ReSharper disable once RedundantAssignment
            string fileName = "fileName";
            _dialogServiceMock.Setup(m => m.ShowSaveFileDialog(out fileName)).Returns(true);
            _fileSystemServiceMock.Setup(m => m.SaveFile("fileName", It.IsAny<string>()));
            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);
            Assert.IsFalse(mainViewModel.SaveToFileCommand.CanExecute(null));

            var logInfo = new LogInfo
            {
                LogLevel = LogLevel.Debug,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "LoggerName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = "TimeStamp"
            };
            mainViewModel.Logs.Add(logInfo);

            Assert.IsTrue(mainViewModel.SaveToFileCommand.CanExecute(null));

            mainViewModel.SaveToFileCommand.Execute(null);

            _dialogServiceMock.Verify(m => m.ShowSaveFileDialog(out fileName), Times.Once);
            _fileSystemServiceMock.Verify(m => m.SaveFile("fileName",
                "[\r\n  {\r\n    \"LogLevel\": 0,\r\n    \"Database\": \"Database\",\r\n    \"TimeStamp\": \"TimeStamp\",\r\n    \"Message\": \"Message\",\r\n    \"LoggerName\": \"LoggerName\",\r\n    \"Exception\": \"Exception\",\r\n    \"StackTrace\": \"StackTrace\"\r\n  }\r\n]"),
                Times.Once);
        }

        [Test]
        public void SaveToFileCommandCancelTest()
        {
            // ReSharper disable once RedundantAssignment
            string fileName = "fileName";
            _dialogServiceMock.Setup(m => m.ShowSaveFileDialog(out fileName)).Returns(false);

            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            var logInfo = new LogInfo
            {
                LogLevel = LogLevel.Debug,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "LoggerName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = "TimeStamp"
            };
            mainViewModel.Logs.Add(logInfo);

            Assert.IsTrue(mainViewModel.SaveToFileCommand.CanExecute(null));

            mainViewModel.SaveToFileCommand.Execute(null);

            _dialogServiceMock.Verify(m => m.ShowSaveFileDialog(out fileName), Times.Once);
            _fileSystemServiceMock.Verify(m => m.SaveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void WindowLoadedCommandTest()
        {
            _configurationServiceMock.Setup(m => m.GetValue("DatabaseUrl")).Returns("http://localhost:8080");
            _configurationServiceMock.Setup(m => m.GetValue("Category")).Returns("LoggerName");

            var mainViewModel = new MainViewModel(_ravenDbCommunicationServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsTrue(mainViewModel.WindowLoadedCommand.CanExecute(null));

            mainViewModel.WindowLoadedCommand.Execute(null);

            Assert.AreEqual("http://localhost:8080", mainViewModel.DatabaseUrl);
            Assert.AreEqual("LoggerName", mainViewModel.Category);
        }
    }
}