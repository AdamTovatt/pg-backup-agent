using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgentTests.Configuration.Agent
{
    [TestClass]
    public class BackupSettingsTests
    {
        private const string Source = "primary";
        private const string PolicyPath = "/path/to/retention-policy.json";

        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            BackupSettings settings = new(Source, PolicyPath, 60);

            Assert.AreEqual(Source, settings.Source);
            Assert.AreEqual(PolicyPath, settings.RetentionPolicyPath);
            Assert.AreEqual(60, settings.TimeoutMinutes);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow("server/a")]
        [DataRow("..")]
        public void Constructor_WithSourceThatCannotNameAFolder_ThrowsArgumentException(string? source)
        {
            Assert.ThrowsException<ArgumentException>(() => new BackupSettings(source!, PolicyPath, 60));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Constructor_WithoutRetentionPolicyPath_ThrowsArgumentException(string? retentionPolicyPath)
        {
            Assert.ThrowsException<ArgumentException>(() => new BackupSettings(Source, retentionPolicyPath!, 60));
        }

        [TestMethod]
        public void Constructor_WithInvalidTimeout_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new BackupSettings(Source, PolicyPath, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new BackupSettings(Source, PolicyPath, -1));
        }
    }
}
