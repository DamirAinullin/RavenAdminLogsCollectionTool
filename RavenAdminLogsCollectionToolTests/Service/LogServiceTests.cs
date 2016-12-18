using System;
using Moq;
using NUnit.Framework;
using RavenAdminLogsCollectionTool.Services;
using WebSocketSharp;

namespace RavenAdminLogsCollectionToolTests.Service
{
    [TestFixture]
    public class LogServiceTests
    {
        private Mock<IRavenDbCommunicationService> _ravenDbCommunicationServiceMock;

        [SetUp]
        public void TestInitialize()
        {
            _ravenDbCommunicationServiceMock = new Mock<IRavenDbCommunicationService>();
        }

        [Test]
        public void LogServiceConnectTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(String.Empty);
            _ravenDbCommunicationServiceMock.Setup(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<ErrorEventArgs>>()))
                .Returns(String.Empty);

            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);

            string errorMessage = logService.Connect("http://localhost:8080/").Result;

            Assert.IsEmpty(errorMessage);
            _ravenDbCommunicationServiceMock.Verify(m => m.ConfigureAdminLogsAsync("http://localhost:8080/"), Times.Once);
            _ravenDbCommunicationServiceMock.Verify(
                m => m.OpenWebSocket("http://localhost:8080/", It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Once);
        }

        [Test]
        public void LogServiceConnectFailedOpenWebSocketIsNotEmptyTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()))
                .ReturnsAsync(String.Empty);
            _ravenDbCommunicationServiceMock.Setup(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<ErrorEventArgs>>()))
                .Returns("Error");

            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);

            string errorMessage = logService.Connect("http://localhost:8080/").Result;

            Assert.AreEqual("Error", errorMessage);
            _ravenDbCommunicationServiceMock.Verify(m => m.ConfigureAdminLogsAsync("http://localhost:8080/"), Times.Once);
            _ravenDbCommunicationServiceMock.Verify(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Once);
        }

        [Test]
        public void LogServiceConnectFailedConfigureAdminLogsAsyncIsNotEmptyTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.ConfigureAdminLogsAsync(It.IsAny<string>()))
                .ReturnsAsync("Error");
            _ravenDbCommunicationServiceMock.Setup(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<ErrorEventArgs>>()))
                .Returns(String.Empty);

            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);

            string errorMessage = logService.Connect("http://localhost:8080/").Result;

            Assert.AreEqual("Error", errorMessage);
            _ravenDbCommunicationServiceMock.Verify(m => m.ConfigureAdminLogsAsync("http://localhost:8080/"), Times.Once);
            _ravenDbCommunicationServiceMock.Verify(m => m.OpenWebSocket(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Never);
        }

        [Test]
        public void LogServiceDisconnectTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.CloseWebSocket()).Returns("");
            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);

            string errorMessage = logService.Disconnect();

            Assert.IsEmpty(errorMessage);
            _ravenDbCommunicationServiceMock.Verify(m => m.CloseWebSocket(), Times.Once);
        }
    }
}
