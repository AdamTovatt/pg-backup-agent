using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgentTests.Configuration.FileRetention
{
    [TestClass]
    public class FileRetentionPolicyProviderTests
    {
        private string _tempPolicyPath = string.Empty;
        private string _tempConfigPath = string.Empty;
        private MockAgentConfigurationProvider _mockConfigProvider = null!;

        [TestInitialize]
        public void Setup()
        {
            _tempPolicyPath = Path.GetTempFileName();
            _tempConfigPath = Path.GetTempFileName();
            _mockConfigProvider = new MockAgentConfigurationProvider(_tempPolicyPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempPolicyPath))
            {
                File.Delete(_tempPolicyPath);
            }
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }
        }

        [TestMethod]
        public void GetRetentionPolicy_WithValidJson_ReturnsPolicy()
        {
            // Arrange
            string validJson = @"{
                ""rules"": [
                    {
                        ""keepEvery"": ""1.00:00:00"",
                        ""duration"": ""14.00:00:00""
                    },
                    {
                        ""keepEvery"": ""7.00:00:00"",
                        ""duration"": null
                    }
                ]
            }";
            File.WriteAllText(_tempPolicyPath, validJson);

            // Act
            FileRetentionPolicyProvider provider = new(_mockConfigProvider);
            RetentionPolicy policy = provider.GetRetentionPolicy();

            // Assert
            Assert.IsNotNull(policy);
            Assert.IsNotNull(policy.Rules);
            Assert.AreEqual(2, policy.Rules.Count);
            Assert.AreEqual("1.00:00:00", policy.Rules[0].KeepEvery);
            Assert.AreEqual("14.00:00:00", policy.Rules[0].Duration);
            Assert.AreEqual("7.00:00:00", policy.Rules[1].KeepEvery);
            Assert.IsNull(policy.Rules[1].Duration);
        }

        [TestMethod]
        public void GetRetentionPolicy_WithMissingFile_ThrowsInvalidOperationException()
        {
            // Arrange
            string nonExistentPath = "/path/that/does/not/exist.json";
            _mockConfigProvider.SetRetentionPolicyPath(nonExistentPath);

            // Act & Assert
            FileRetentionPolicyProvider provider = new(_mockConfigProvider);
            Assert.ThrowsException<InvalidOperationException>(() => provider.GetRetentionPolicy());
        }

        [TestMethod]
        public void GetRetentionPolicy_WithInvalidJson_ThrowsInvalidOperationException()
        {
            // Arrange
            string invalidJson = "{ invalid json content }";
            File.WriteAllText(_tempPolicyPath, invalidJson);

            // Act & Assert
            FileRetentionPolicyProvider provider = new(_mockConfigProvider);
            Assert.ThrowsException<InvalidOperationException>(() => provider.GetRetentionPolicy());
        }

        [TestMethod]
        public void GetRetentionPolicy_WithEmptyFile_ThrowsInvalidOperationException()
        {
            // Arrange
            File.WriteAllText(_tempPolicyPath, "");

            // Act & Assert
            FileRetentionPolicyProvider provider = new(_mockConfigProvider);
            Assert.ThrowsException<InvalidOperationException>(() => provider.GetRetentionPolicy());
        }

        [TestMethod]
        public void GetRetentionPolicy_WithEmptyRules_ThrowsInvalidOperationException()
        {
            // Arrange
            string jsonWithEmptyRules = @"{
                ""rules"": []
            }";
            File.WriteAllText(_tempPolicyPath, jsonWithEmptyRules);

            // Act & Assert
            FileRetentionPolicyProvider provider = new(_mockConfigProvider);
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => provider.GetRetentionPolicy());
            Assert.IsTrue(exception.Message.Contains("---> Retention policy must contain at least one rule"));
        }

        [TestMethod]
        public void GetRetentionPolicy_WithInvalidTimeSpanFormat_ThrowsInvalidOperationException()
        {
            // Arrange
            string jsonWithInvalidTimeSpan = @"{
                ""rules"": [
                    {
                        ""keepEvery"": ""invalid-format"",
                        ""duration"": ""14.00:00:00""
                    }
                ]
            }";
            File.WriteAllText(_tempPolicyPath, jsonWithInvalidTimeSpan);

            // Act & Assert
            FileRetentionPolicyProvider provider = new(_mockConfigProvider);
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => provider.GetRetentionPolicy());
            Assert.IsTrue(exception.Message.Contains("---> Rule 1: KeepEvery must be a valid TimeSpan format"));
        }

        [TestMethod]
        public void GetRetentionPolicy_WithInvalidDurationFormat_ThrowsInvalidOperationException()
        {
            // Arrange
            string jsonWithInvalidDuration = @"{
                ""rules"": [
                    {
                        ""keepEvery"": ""1.00:00:00"",
                        ""duration"": ""invalid-format""
                    }
                ]
            }";
            File.WriteAllText(_tempPolicyPath, jsonWithInvalidDuration);

            // Act & Assert
            FileRetentionPolicyProvider provider = new(_mockConfigProvider);
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => provider.GetRetentionPolicy());
            Assert.IsTrue(exception.Message.Contains("---> Rule 1: Duration must be a valid TimeSpan format"));
        }

        private class MockAgentConfigurationProvider : IAgentConfigurationProvider
        {
            private string _retentionPolicyPath;

            public MockAgentConfigurationProvider(string retentionPolicyPath)
            {
                _retentionPolicyPath = retentionPolicyPath;
            }

            public void SetRetentionPolicyPath(string path)
            {
                _retentionPolicyPath = path;
            }

            public AgentConfiguration GetConfiguration()
            {
                PostgresSettings postgres = new("localhost", 5432, "user", "pass", "db");
                ByteShelfSettings byteShelf = new("https://example.com", "api-key-with-sufficient-length");
                BackupSettings backup = new(_retentionPolicyPath, 60);
                return new AgentConfiguration(postgres, byteShelf, backup);
            }
        }
    }
} 