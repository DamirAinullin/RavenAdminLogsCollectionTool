using System;
using Moq;
using NUnit.Framework;
using RavenAdminLogsCollectionTool.Model;
using RavenAdminLogsCollectionTool.Services;
using RavenAdminLogsCollectionTool.ViewModel;
using WebSocketSharp;
using LogLevel = RavenAdminLogsCollectionTool.Model.LogLevel;
using System.ComponentModel.DataAnnotations;

namespace RavenAdminLogsCollectionToolTests
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
        public void ConnectCommandTestShowErrorMessageIfConfigureAdminLogsMessageIsNotEmpty()
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
        public void ConnectCommandTestShowErrorMessageIfOpenWebSocketMessageIsNotEmpty()
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


    }
}