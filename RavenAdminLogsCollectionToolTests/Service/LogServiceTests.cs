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
        public void LogServiceConnectAsyncTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<ErrorEventArgs>>())).ReturnsAsync(String.Empty);

            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);

            string errorMessage = logService.ConnectAsync("http://localhost:8080/").Result;

            Assert.IsEmpty(errorMessage);
            _ravenDbCommunicationServiceMock.Verify(
                m => m.ConnectAsync("http://localhost:8080/", It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Once);
        }

        [Test]
        public void LogServiceConnectAsyncFailedTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<ErrorEventArgs>>()))
                .Throws(new Exception("Error"));

            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);
            Assert.That(() => logService.ConnectAsync("http://localhost:8080/"),
               Throws.TypeOf<Exception>().With.Message.EquivalentTo("Error"));
            _ravenDbCommunicationServiceMock.Verify(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<EventHandler>(),
                It.IsAny<EventHandler<CloseEventArgs>>(), It.IsAny<EventHandler<MessageEventArgs>>(),
                It.IsAny<EventHandler<ErrorEventArgs>>()), Times.Once);
        }
        
        [Test]
        public void LogServiceDisconnectTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.Disconnect());
            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);

            logService.Disconnect();

            _ravenDbCommunicationServiceMock.Verify(m => m.Disconnect(), Times.Once);
        }

        [Test]
        public void LogServiceDisconnectFailedTest()
        {
            _ravenDbCommunicationServiceMock.Setup(m => m.Disconnect()).Throws(new Exception("Error"));
            var logService = new LogService(_ravenDbCommunicationServiceMock.Object);

            Assert.That(() => logService.Disconnect(),
               Throws.TypeOf<Exception>().With.Message.EquivalentTo("Error"));

            _ravenDbCommunicationServiceMock.Verify(m => m.Disconnect(), Times.Once);
        }
    }
}
