using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgentTests.Configuration.Agent
{
    [TestClass]
    public class PostgresSettingsTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            string host = "localhost";
            int port = 5432;
            string username = "postgres";
            string password = "password123";
            string database = "mydb";

            // Act
            PostgresSettings settings = new(host, port, username, password, database);

            // Assert
            Assert.AreEqual(host, settings.Host);
            Assert.AreEqual(port, settings.Port);
            Assert.AreEqual(username, settings.Username);
            Assert.AreEqual(password, settings.Password);
            Assert.AreEqual(database, settings.Database);
        }

        [TestMethod]
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresSettings(null!, 5432, "user", "pass", "db"));
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresSettings("host", 5432, null!, "pass", "db"));
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresSettings("host", 5432, "user", null!, "db"));
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresSettings("host", 5432, "user", "pass", null!));
        }

        [TestMethod]
        public void Constructor_WithEmptyStringParameters_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => new PostgresSettings("", 5432, "user", "pass", "db"));
            Assert.ThrowsException<ArgumentException>(() => new PostgresSettings("host", 5432, "", "pass", "db"));
            Assert.ThrowsException<ArgumentException>(() => new PostgresSettings("host", 5432, "user", "", "db"));
            Assert.ThrowsException<ArgumentException>(() => new PostgresSettings("host", 5432, "user", "pass", ""));
        }

        [TestMethod]
        public void Constructor_WithInvalidPort_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new PostgresSettings("host", 0, "user", "pass", "db"));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new PostgresSettings("host", -1, "user", "pass", "db"));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new PostgresSettings("host", 65536, "user", "pass", "db"));
        }
    }
} 