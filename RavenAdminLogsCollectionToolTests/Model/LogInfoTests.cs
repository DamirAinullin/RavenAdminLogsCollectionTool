using System;
using NUnit.Framework;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionToolTests.Model
{
    [TestFixture]
    public class LogInfoTests
    {
        [Test]
        public void LogInfoToStringTest()
        {
            var logInfo = new LogInfo
            {
                Level = LogLevel.Debug,
                Database = "Database",
                Exception = "Exception",
                LoggerName = "LoggerName",
                Message = "Message",
                StackTrace = "StackTrace",
                TimeStamp = DateTime.Parse("2016-12-18T19:27:33.7743417Z").ToUniversalTime()
            };
            Assert.AreEqual("2016-12-18T19:27:33.7743417Z;DEBUG;Database;LoggerName;MessageException\nStackTrace\n\n", logInfo.ToString());
        }
    }
}
