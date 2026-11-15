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

                return configuration;
            }
            catch (ArgumentException ex)
            {
                // Each settings class checks its own values when it is created.
                throw new InvalidOperationException($"Invalid configuration: {ex.Message}", ex);
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
    }
}