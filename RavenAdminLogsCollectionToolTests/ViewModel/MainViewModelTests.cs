using System;
using System.ComponentModel.DataAnnotations;
using Moq;
using NUnit.Framework;
using RavenAdminLogsCollectionTool.Helpers;
using RavenAdminLogsCollectionTool.Services;
using RavenAdminLogsCollectionTool.ViewModel;
using LogLevel = RavenAdminLogsCollectionTool.Model.LogLevel;

namespace RavenAdminLogsCollectionToolTests.ViewModel
{
    [TestFixture]
    public class MainViewModelTests
    {
        private Mock<ILogService> _logServiceMock;
        private Mock<IDialogService> _dialogServiceMock;
        private Mock<IFileSystemService> _fileSystemServiceMock;
        private Mock<IConfigurationService> _configurationServiceMock;

        [SetUp]
        public void TestInitialize()
        {
            _logServiceMock = new Mock<ILogService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _fileSystemServiceMock = new Mock<IFileSystemService>();
            _configurationServiceMock = new Mock<IConfigurationService>();
        }

        [Test]
        public void LogsClearCommandTest()
        {
            _logServiceMock.Setup(m => m.LogsClear());
            _logServiceMock.Setup(m => m.IsAllLogsEmpty()).Returns(true);
            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsFalse(mainViewModel.LogsClearCommand.CanExecute(null));

            _logServiceMock.Setup(m => m.IsAllLogsEmpty()).Returns(false);
            Assert.IsTrue(mainViewModel.LogsClearCommand.CanExecute(null));

            mainViewModel.LogsClearCommand.Execute(null);

            _logServiceMock.Verify(m => m.LogsClear(), Times.Once);
        }

        [Test]
        public void ConnectCommandTest()
        {
            _configurationServiceMock.Setup(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()));
            _logServiceMock.Setup(m => m.ConnectAsync(It.IsAny<string>()))
                .ReturnsAsync(String.Empty);

            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object)
            {
                LogLevel = LogLevel.Debug,
                Category = "Category"
            };

            Assert.IsFalse(mainViewModel.ConnectCommand.CanExecute(null));

            Assert.Catch<ValidationException>(() => { mainViewModel.DatabaseUrl = "IncorrectDatabaseUrl"; });

            Assert.IsFalse(mainViewModel.ConnectCommand.CanExecute(null));

            mainViewModel.DatabaseUrl = "http://localhost:8080";

            Assert.IsTrue(mainViewModel.ConnectCommand.CanExecute(null));

            mainViewModel.ConnectCommand.Execute(null);

            Assert.IsFalse(mainViewModel.ConnectIsEnabled);
            _configurationServiceMock.Verify(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _logServiceMock.Verify(m => m.ConnectAsync("http://localhost:8080"), Times.Once());
            _dialogServiceMock.Verify(m => m.ShowErrorMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        
        [Test]
        public void ConnectCommandShowErrorMessageIfConnectMessageIsNotEmptyTest()
        {
            _configurationServiceMock.Setup(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()));
            _logServiceMock.Setup(m => m.ConnectAsync(It.IsAny<string>()))
                .Throws(new Exception("Something wrong happened"));

            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object)
            {
                DatabaseUrl = "http://localhost:8080",
                LogLevel = LogLevel.Debug,
                Category = "Category"
            };

            mainViewModel.ConnectCommand.Execute(null);

            _configurationServiceMock.Verify(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _logServiceMock.Verify(m => m.ConnectAsync("http://localhost:8080"), Times.Once);

            _dialogServiceMock.Verify(m => m.ShowErrorMessage("Network error has occurred. Something wrong happened", "Error"), Times.Once);
            Assert.IsTrue(mainViewModel.ConnectIsEnabled);
            Assert.IsFalse(mainViewModel.DisconnectIsEnabled);
        }
                
        [Test]
        public void DiconnectCommandTest()
        {
            _logServiceMock.Setup(m => m.Disconnect());
            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsTrue(mainViewModel.DisconnectCommand.CanExecute(null));

            mainViewModel.DisconnectCommand.Execute(null);

            _logServiceMock.Verify(m => m.Disconnect(), Times.Once);
            _dialogServiceMock.Verify(m => m.ShowErrorMessage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.IsFalse(mainViewModel.DisconnectIsEnabled);
        }
        
        [Test]
        public void DiconnectCommandShowErrorMessageIfOpenWebSocketMessageIsNotEmptyTest()
        {
            _logServiceMock.Setup(m => m.Disconnect()).Throws(new Exception("Something wrong happened"));
            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            mainViewModel.DisconnectCommand.Execute(null);

            _logServiceMock.Verify(m => m.Disconnect(), Times.Once);
            _dialogServiceMock.Verify(m => m.ShowErrorMessage("Websocket error has occurred. Something wrong happened", "Error"), Times.Once);
            Assert.IsFalse(mainViewModel.DisconnectIsEnabled);
        }

        [Test]
        public void FilterLogsCommandTest()
        {
            _logServiceMock.Setup(m => m.FilterLogs(It.IsAny<LogLevel>(), It.IsAny<string>()));
            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object) {Category = "LoggerName"};

            Assert.IsTrue(mainViewModel.FilterLogsCommand.CanExecute(null));

            mainViewModel.FilterLogsCommand.Execute(LogLevel.Warn);

            _logServiceMock.Verify(m => m.FilterLogs(LogLevel.Warn, "LoggerName"), Times.Once);
        }
        
        [Test]
        public void SaveToFileCommandTest()
        {
            _fileSystemServiceMock.Setup(m => m.SaveLogFile( It.IsAny<string>()));
            _logServiceMock.Setup(m => m.LogsToJsonString()).Returns("test");
            _logServiceMock.Setup(m => m.IsFilterLogsEmpty()).Returns(true);
            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);
            Assert.IsFalse(mainViewModel.ExportCommand.CanExecute(null));

            _logServiceMock.Setup(m => m.IsFilterLogsEmpty()).Returns(false);
            Assert.IsTrue(mainViewModel.ExportCommand.CanExecute(null));

            mainViewModel.ExportCommand.Execute(null);

            _logServiceMock.Verify(m => m.LogsToJsonString(), Times.Once);
            _fileSystemServiceMock.Verify(m => m.SaveLogFile("test"), Times.Once);
        }

        [TestCase("http://localhost:8080", "LoggerName", "false", "http://localhost:8080", "LoggerName", false, TestName = "WindowLoadedCommandTest")]
        [TestCase("", "", "wrong data", null, null, true, TestName = "WindowLoadedCommandWrongDataTest")]
        public void WindowLoadedCommandTest(string databaseUrl, string loggerName, string autoScrollEnabledStr,
            string checkDatabaseUrl, string checkLoggerName, bool checkIsAutoScrollEnabled)
        {
            _configurationServiceMock.Setup(m => m.GetValue("DatabaseUrl")).Returns(databaseUrl);
            _configurationServiceMock.Setup(m => m.GetValue("Category")).Returns(loggerName);
            _configurationServiceMock.Setup(m => m.GetValue("AutoScrollEnabled")).Returns(autoScrollEnabledStr);

            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsTrue(mainViewModel.WindowLoadedCommand.CanExecute(null));

            mainViewModel.WindowLoadedCommand.Execute(null);

            Assert.AreEqual(checkDatabaseUrl, mainViewModel.DatabaseUrl);
            Assert.AreEqual(checkLoggerName, mainViewModel.Category);
            Assert.AreEqual(checkIsAutoScrollEnabled, mainViewModel.AutoScrollEnabled);
        }

        [Test]
        public void KeepDownCommandTest()
        {
            _configurationServiceMock.Setup(m => m.SetValue(It.IsAny<string>(), It.IsAny<string>()));

            var mainViewModel = new MainViewModel(_logServiceMock.Object, _dialogServiceMock.Object,
                _fileSystemServiceMock.Object, _configurationServiceMock.Object);

            Assert.IsTrue(mainViewModel.WindowLoadedCommand.CanExecute(null));

            mainViewModel.KeepDownCommand.Execute(true);

            Assert.IsTrue(mainViewModel.AutoScrollEnabled);
            Assert.IsTrue(AutoScrollBehavior.IsEnabled);

            _configurationServiceMock.Setup(m => m.SetValue("AutoScrollIsEnabled", "true"));
        }
    }
}