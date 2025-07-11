using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgentTests.Configuration.Agent
{
    [TestClass]
    public class BackupSettingsTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            string retentionPolicyPath = "/path/to/retention-policy.json";
            int timeoutMinutes = 60;

            // Act
            BackupSettings settings = new(retentionPolicyPath, timeoutMinutes);

            // Assert
            Assert.AreEqual(retentionPolicyPath, settings.RetentionPolicyPath);
            Assert.AreEqual(timeoutMinutes, settings.TimeoutMinutes);
        }

        [TestMethod]
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new BackupSettings(null!, 60));
        }

        [TestMethod]
        public void Constructor_WithEmptyStringParameters_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => new BackupSettings("", 60));
        }

        [TestMethod]
        public void Constructor_WithInvalidTimeout_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new BackupSettings("/path/to/policy.json", 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new BackupSettings("/path/to/policy.json", -1));
        }
    }
} 