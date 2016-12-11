using System;
using System.Linq;
using NUnit.Framework;
using RavenAdminLogsCollectionTool.Helpers;

namespace RavenAdminLogsCollectionToolTests.Helpers
{
    [TestFixture]
    public class RandomIdGeneratorTests
    {
        [Test]
        public void GenerateIdTest()
        {
            string id = RandomIdGenerator.GenerateId();

            Assert.AreEqual(5, id.Length);
            Assert.IsTrue(id.All(Char.IsLetterOrDigit));

            id = RandomIdGenerator.GenerateId(10);

            Assert.AreEqual(10, id.Length);
            Assert.IsTrue(id.All(Char.IsLetterOrDigit));
        }
    }
}
