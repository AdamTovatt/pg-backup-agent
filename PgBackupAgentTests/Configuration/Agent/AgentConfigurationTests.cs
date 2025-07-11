using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgentTests.Configuration.Agent
{
    [TestClass]
    public class AgentConfigurationTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            PostgresSettings postgresSettings = new("localhost", 5432, "user", "password", "database");
            ByteShelfSettings byteShelfSettings = new("https://example.com", "api-key-with-sufficient-length");
            BackupSettings backupSettings = new("/path/to/policy.json", 60);

            // Act
            AgentConfiguration configuration = new(postgresSettings, byteShelfSettings, backupSettings);

            // Assert
            Assert.IsNotNull(configuration.Postgres);
            Assert.IsNotNull(configuration.ByteShelf);
            Assert.IsNotNull(configuration.Backup);
            Assert.AreEqual(postgresSettings, configuration.Postgres);
            Assert.AreEqual(byteShelfSettings, configuration.ByteShelf);
            Assert.AreEqual(backupSettings, configuration.Backup);
        }

        [TestMethod]
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange
            PostgresSettings postgresSettings = new("localhost", 5432, "user", "password", "database");
            ByteShelfSettings byteShelfSettings = new("https://example.com", "api-key-with-sufficient-length");
            BackupSettings backupSettings = new("/path/to/policy.json", 60);

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new AgentConfiguration(null!, byteShelfSettings, backupSettings));
            Assert.ThrowsException<ArgumentNullException>(() => new AgentConfiguration(postgresSettings, null!, backupSettings));
            Assert.ThrowsException<ArgumentNullException>(() => new AgentConfiguration(postgresSettings, byteShelfSettings, null!));
        }
    }
} 