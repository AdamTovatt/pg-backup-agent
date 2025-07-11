using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgentTests.Configuration.Agent
{
    [TestClass]
    public class ByteShelfSettingsTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            string baseUrl = "https://byteshelf.example.com";
            string apiKey = "api-key-with-sufficient-length";

            // Act
            ByteShelfSettings settings = new(baseUrl, apiKey);

            // Assert
            Assert.AreEqual(baseUrl, settings.BaseUrl);
            Assert.AreEqual(apiKey, settings.ApiKey);
        }

        [TestMethod]
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new ByteShelfSettings(null!, "api-key"));
            Assert.ThrowsException<ArgumentNullException>(() => new ByteShelfSettings("https://example.com", null!));
        }

        [TestMethod]
        public void Constructor_WithEmptyStringParameters_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => new ByteShelfSettings("", "api-key"));
            Assert.ThrowsException<ArgumentException>(() => new ByteShelfSettings("https://example.com", ""));
        }

        [TestMethod]
        public void Constructor_WithShortApiKey_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => new ByteShelfSettings("https://example.com", "short"));
        }
    }
} 