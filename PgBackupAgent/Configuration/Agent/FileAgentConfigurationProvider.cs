using System.Text.Json;
using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgent.Configuration
{
    /// <summary>
    /// File-based configuration provider that reads from a JSON configuration file.
    /// </summary>
    public class FileAgentConfigurationProvider : IAgentConfigurationProvider
    {
        private readonly string _configFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileAgentConfigurationProvider"/> class.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file.</param>
        public FileAgentConfigurationProvider(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        /// <summary>
        /// Gets the backup configuration from the JSON file.
        /// </summary>
        /// <returns>The backup configuration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the configuration file cannot be read or is invalid.</exception>
        public AgentConfiguration GetConfiguration()
        {
            if (!File.Exists(_configFilePath))
            {
                throw new InvalidOperationException($"Configuration file not found: {_configFilePath}");
            }

            try
            {
                string jsonContent = File.ReadAllText(_configFilePath);
                AgentConfiguration? configuration = JsonSerializer.Deserialize<AgentConfiguration>(jsonContent, JsonOptions.DefaultSerializerOptions);

                if (configuration == null)
                {
                    throw new InvalidOperationException("Configuration file is empty or contains invalid JSON.");
                }

                ValidateConfiguration(configuration);
                return configuration;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse configuration file: {ex.Message}", ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Failed to read configuration file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates the configuration to ensure all required fields are set.
        /// </summary>
        /// <param name="configuration">The configuration to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        private static void ValidateConfiguration(AgentConfiguration configuration)
        {
            List<string> errors = new();

            // Validate PostgreSQL settings
            if (string.IsNullOrWhiteSpace(configuration.Postgres.Host))
            {
                errors.Add("Postgres.Host is required");
            }

            if (configuration.Postgres.Port <= 0 || configuration.Postgres.Port > 65535)
            {
                errors.Add("Postgres.Port must be between 1 and 65535");
            }

            if (string.IsNullOrWhiteSpace(configuration.Postgres.Username))
            {
                errors.Add("Postgres.Username is required");
            }

            if (string.IsNullOrWhiteSpace(configuration.Postgres.Password))
            {
                errors.Add("Postgres.Password is required");
            }

            if (string.IsNullOrWhiteSpace(configuration.Postgres.Database))
            {
                errors.Add("Postgres.Database is required");
            }

            // Validate ByteShelf settings
            if (string.IsNullOrWhiteSpace(configuration.ByteShelf.BaseUrl))
            {
                errors.Add("ByteShelf.BaseUrl is required");
            }

            if (string.IsNullOrWhiteSpace(configuration.ByteShelf.ApiKey))
            {
                errors.Add("ByteShelf.ApiKey is required");
            }

            if (configuration.ByteShelf.ApiKey.Length < 16)
            {
                errors.Add("ByteShelf.ApiKey must be at least 16 characters long");
            }

            // Validate backup settings
            if (string.IsNullOrWhiteSpace(configuration.Backup.RetentionPolicyPath))
            {
                errors.Add("Backup.RetentionPolicyPath is required");
            }

            if (configuration.Backup.TimeoutMinutes <= 0)
            {
                errors.Add("Backup.TimeoutMinutes must be greater than 0");
            }

            if (errors.Count > 0)
            {
                string errorMessage = $"Configuration validation failed:\n{string.Join("\n", errors.Select(e => $"---> {e}"))}";
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}