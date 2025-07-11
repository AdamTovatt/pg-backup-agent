using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgentTests.Integration
{
    [TestClass]
    public class ConfigurationIntegrationTests
    {
        private string _tempConfigPath = string.Empty;
        private string _tempPolicyPath = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempConfigPath = Path.GetTempFileName();
            _tempPolicyPath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempConfigPath))
            {
                File.Delete(_tempConfigPath);
            }
            if (File.Exists(_tempPolicyPath))
            {
                File.Delete(_tempPolicyPath);
            }
        }

        [TestMethod]
        public void FullConfigurationPipeline_WithValidFiles_LoadsSuccessfully()
        {
            // Arrange
            string configJson = @"{
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
                    ""retentionPolicyPath"": """ + _tempPolicyPath.Replace("\\", "\\\\") + @""",
                    ""timeoutMinutes"": 60
                }
            }";
            File.WriteAllText(_tempConfigPath, configJson);

            string policyJson = @"{
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
            File.WriteAllText(_tempPolicyPath, policyJson);

            // Act
            FileAgentConfigurationProvider configProvider = new(_tempConfigPath);
            AgentConfiguration agentConfig = configProvider.GetConfiguration();

            FileRetentionPolicyProvider policyProvider = new(configProvider);
            RetentionPolicy retentionPolicy = policyProvider.GetRetentionPolicy();

            // Assert
            Assert.IsNotNull(agentConfig);
            Assert.IsNotNull(agentConfig.Postgres);
            Assert.IsNotNull(agentConfig.ByteShelf);
            Assert.IsNotNull(agentConfig.Backup);
            Assert.AreEqual("localhost", agentConfig.Postgres.Host);
            Assert.AreEqual(5432, agentConfig.Postgres.Port);
            Assert.AreEqual("https://byteshelf.example.com", agentConfig.ByteShelf.BaseUrl);
            Assert.AreEqual(_tempPolicyPath, agentConfig.Backup.RetentionPolicyPath);
            Assert.AreEqual(60, agentConfig.Backup.TimeoutMinutes);

            Assert.IsNotNull(retentionPolicy);
            Assert.IsNotNull(retentionPolicy.Rules);
            Assert.AreEqual(2, retentionPolicy.Rules.Count);
            Assert.AreEqual("1.00:00:00", retentionPolicy.Rules[0].KeepEvery);
            Assert.AreEqual("14.00:00:00", retentionPolicy.Rules[0].Duration);
            Assert.AreEqual("7.00:00:00", retentionPolicy.Rules[1].KeepEvery);
            Assert.IsNull(retentionPolicy.Rules[1].Duration);
        }

        [TestMethod]
        public void ConfigurationValidation_WithInvalidData_ThrowsAppropriateExceptions()
        {
            // Arrange
            string invalidConfigJson = @"{
                ""postgres"": {
                    ""host"": """",
                    ""port"": 0,
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
                    ""timeoutMinutes"": -1
                }
            }";
            File.WriteAllText(_tempConfigPath, invalidConfigJson);

            // Act & Assert
            FileAgentConfigurationProvider configProvider = new(_tempConfigPath);
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => configProvider.GetConfiguration());
            
            Console.WriteLine(exception.Message);
            
            // The validation happens in constructors during deserialization, so we only get the first error
            // In this case, it's likely the empty host validation
            Assert.IsTrue(exception.Message.Contains("Failed to read configuration file:"));
        }
    }
} 