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
                TimeStamp = "TimeStamp"
            };
            Assert.AreEqual("TimeStamp;DEBUG;Database;LoggerName;MessageException\nStackTrace\n\n", logInfo.ToString());
        }
    }
}
