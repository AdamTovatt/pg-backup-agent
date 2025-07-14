using System.Text.Json;
using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgent.Configuration.FileRetention
{
    /// <summary>
    /// File-based retention policy provider that reads from a JSON configuration file.
    /// </summary>
    public class FileRetentionPolicyProvider : IRetentionPolicyProvider
    {
        private readonly IAgentConfigurationProvider _configurationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileRetentionPolicyProvider"/> class.
        /// </summary>
        /// <param name="configurationProvider">The configuration provider to get the retention policy path from.</param>
        public FileRetentionPolicyProvider(IAgentConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        /// <summary>
        /// Gets the retention policy from the JSON file specified in the configuration.
        /// </summary>
        /// <returns>The retention policy.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the retention policy file cannot be read or is invalid.</exception>
        public RetentionPolicy GetRetentionPolicy()
        {
            AgentConfiguration configuration = _configurationProvider.GetConfiguration();
            string filePath = configuration.Backup.RetentionPolicyPath;

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Retention policy file not found: {filePath}");
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                RetentionPolicy? policy = JsonSerializer.Deserialize<RetentionPolicy>(jsonContent, JsonOptions.DefaultSerializerOptions);

                if (policy == null)
                {
                    throw new InvalidOperationException("Retention policy file is empty or contains invalid JSON.");
                }

                ValidateRetentionPolicy(policy);
                return policy;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse retention policy file: {ex.Message}", ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Failed to read retention policy file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates the retention policy to ensure it is properly configured.
        /// </summary>
        /// <param name="policy">The retention policy to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when retention policy is invalid.</exception>
        private static void ValidateRetentionPolicy(RetentionPolicy policy)
        {
            List<string> errors = new();

            if (policy.Rules == null || policy.Rules.Count == 0)
            {
                errors.Add("Retention policy must contain at least one rule");
            }
            else
            {
                for (int i = 0; i < policy.Rules.Count; i++)
                {
                    RetentionRule rule = policy.Rules[i];

                    if (string.IsNullOrWhiteSpace(rule.KeepEvery))
                    {
                        errors.Add($"Rule {i + 1}: KeepEvery is required");
                    }
                    else
                    {
                        try
                        {
                            TimeSpan keepEvery = rule.KeepEveryTimeSpan;
                            if (keepEvery <= TimeSpan.Zero)
                            {
                                errors.Add($"Rule {i + 1}: KeepEvery must be greater than zero");
                            }
                        }
                        catch (FormatException)
                        {
                            errors.Add($"Rule {i + 1}: KeepEvery must be a valid TimeSpan format (e.g., '1.00:00:00')");
                        }
                    }

                    if (rule.Duration != null)
                    {
                        try
                        {
                            TimeSpan duration = rule.DurationTimeSpan!.Value;
                            if (duration <= TimeSpan.Zero)
                            {
                                errors.Add($"Rule {i + 1}: Duration must be greater than zero");
                            }
                        }
                        catch (FormatException)
                        {
                            errors.Add($"Rule {i + 1}: Duration must be a valid TimeSpan format (e.g., '14.00:00:00') or null");
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                string errorMessage = $"Retention policy validation failed:\n{string.Join("\n", errors.Select(e => $"---> {e}"))}";
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}