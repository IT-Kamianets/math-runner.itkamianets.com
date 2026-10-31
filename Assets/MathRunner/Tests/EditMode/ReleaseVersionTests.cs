using MathRunner.Core.Update;
using NUnit.Framework;

namespace MathRunner.Tests
{
    public class ReleaseVersionTests
    {
        [TestCase("0.1.0", "v0.2.0", true)]
        [TestCase("0.1.0", "0.1.1", true)]
        [TestCase("0.9.0", "v0.10.0", true)]
        [TestCase("0.2.0", "v0.2.0", false)]
        [TestCase("0.2.0", "v0.1.9", false)]
        [TestCase("0.2", "v0.2.0", false)]
        [TestCase("0.2.0", "v0.3.0-beta", false)]
        [TestCase("0.2.0", "latest", false)]
        [TestCase("0.2.0", "", false)]
        [TestCase("", "v0.3.0", false)]
        public void IsNewer_ComparesNumerically(string current, string latest, bool expected)
        {
            Assert.AreEqual(expected, ReleaseVersion.IsNewer(current, latest));
        }
    }
}
