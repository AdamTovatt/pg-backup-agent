using System.Text.Json;
using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgentTests.Configuration.Agent
{
    [TestClass]
    public class FileAgentConfigurationProviderTests
    {
        private string _tempConfigPath = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempConfigPath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }
        }

        [TestMethod]
        public void GetConfiguration_WithValidJson_ReturnsConfiguration()
        {
            // Arrange
            string validJson = @"{
                ""postgres"": {
                    ""host"": ""localhost"",
                    ""port"": 5432,
                    ""username"": ""postgres"",
                    ""password"": ""password123"",
                    ""database"": ""mydb""
                },
                ""byteShelf"": {
                    ""baseUrl"": ""https://byteshelf.example.com"",
                    ""apiKey"": ""api-key-with-sufficient-length""
                },
                ""backup"": {
                    ""retentionPolicyPath"": ""/path/to/policy.json"",
                    ""timeoutMinutes"": 60
                }
            }";
            File.WriteAllText(_tempConfigPath, validJson);

            // Act
            FileAgentConfigurationProvider provider = new(_tempConfigPath);
            AgentConfiguration configuration = provider.GetConfiguration();

            // Assert
            Assert.IsNotNull(configuration);
            Assert.IsNotNull(configuration.Postgres);
            Assert.IsNotNull(configuration.ByteShelf);
            Assert.IsNotNull(configuration.Backup);
            Assert.AreEqual("localhost", configuration.Postgres.Host);
            Assert.AreEqual(5432, configuration.Postgres.Port);
            Assert.AreEqual("https://byteshelf.example.com", configuration.ByteShelf.BaseUrl);
            Assert.AreEqual("/path/to/policy.json", configuration.Backup.RetentionPolicyPath);
        }

        [TestMethod]
        public void GetConfiguration_WithMissingFile_ThrowsInvalidOperationException()
        {
            // Arrange
            string nonExistentPath = "/path/that/does/not/exist.json";

            // Act & Assert
            FileAgentConfigurationProvider provider = new(nonExistentPath);
            Assert.ThrowsException<InvalidOperationException>(() => provider.GetConfiguration());
        }

        [TestMethod]
        public void GetConfiguration_WithInvalidJson_ThrowsInvalidOperationException()
        {
            // Arrange
            string invalidJson = "{ invalid json content }";
            File.WriteAllText(_tempConfigPath, invalidJson);

            // Act & Assert
            FileAgentConfigurationProvider provider = new(_tempConfigPath);
            Assert.ThrowsException<InvalidOperationException>(() => provider.GetConfiguration());
        }

        [TestMethod]
        public void GetConfiguration_WithEmptyFile_ThrowsInvalidOperationException()
        {
            // Arrange
            File.WriteAllText(_tempConfigPath, "");

            // Act & Assert
            FileAgentConfigurationProvider provider = new(_tempConfigPath);
            Assert.ThrowsException<InvalidOperationException>(() => provider.GetConfiguration());
        }

        [TestMethod]
        public void GetConfiguration_WithMissingPostgresHost_ThrowsInvalidOperationException()
        {
            // Arrange
            string jsonWithMissingHost = @"{
                ""postgres"": {
                    ""port"": 5432,
                    ""username"": ""postgres"",
                    ""password"": ""password123"",
                    ""database"": ""mydb""
                },
                ""byteShelf"": {
                    ""baseUrl"": ""https://byteshelf.example.com"",
                    ""apiKey"": ""api-key-with-sufficient-length""
                },
                ""backup"": {
                    ""retentionPolicyPath"": ""/path/to/policy.json"",
                    ""timeoutMinutes"": 60
                }
            }";
            File.WriteAllText(_tempConfigPath, jsonWithMissingHost);

            // Act & Assert
            FileAgentConfigurationProvider provider = new(_tempConfigPath);
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => provider.GetConfiguration());

            Console.WriteLine(exception.Message);

            Assert.IsTrue(exception.Message.Contains("Failed to read configuration file: Value cannot be null. (Parameter 'host')"));
        }

        [TestMethod]
        public void GetConfiguration_WithInvalidPort_ThrowsInvalidOperationException()
        {
            // Arrange
            string jsonWithInvalidPort = @"{
                ""postgres"": {
                    ""host"": ""localhost"",
                    ""port"": 0,
                    ""username"": ""postgres"",
                    ""password"": ""password123"",
                    ""database"": ""mydb""
                },
                ""byteShelf"": {
                    ""baseUrl"": ""https://byteshelf.example.com"",
                    ""apiKey"": ""api-key-with-sufficient-length""
                },
                ""backup"": {
                    ""retentionPolicyPath"": ""/path/to/policy.json"",
                    ""timeoutMinutes"": 60
                }
            }";
            File.WriteAllText(_tempConfigPath, jsonWithInvalidPort);

            // Act & Assert
            FileAgentConfigurationProvider provider = new(_tempConfigPath);
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => provider.GetConfiguration());
            Assert.IsTrue(exception.Message.Contains("Failed to read configuration file: Specified argument was out of the range of valid values. (Parameter 'port')"));
        }

        [TestMethod]
        public void GetConfiguration_WithShortApiKey_ThrowsInvalidOperationException()
        {
            // Arrange
            string jsonWithShortApiKey = @"{
                ""postgres"": {
                    ""host"": ""localhost"",
                    ""port"": 5432,
                    ""username"": ""postgres"",
                    ""password"": ""password123"",
                    ""database"": ""mydb""
                },
                ""byteShelf"": {
                    ""baseUrl"": ""https://byteshelf.example.com"",
                    ""apiKey"": ""short""
                },
                ""backup"": {
                    ""retentionPolicyPath"": ""/path/to/policy.json"",
                    ""timeoutMinutes"": 60
                }
            }";
            File.WriteAllText(_tempConfigPath, jsonWithShortApiKey);

            // Act & Assert
            FileAgentConfigurationProvider provider = new(_tempConfigPath);
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => provider.GetConfiguration());

            Console.WriteLine(exception.Message);

            Assert.IsTrue(exception.Message.Contains("Failed to read configuration file: API key must be at least 16 characters long"));
        }
    }
} 