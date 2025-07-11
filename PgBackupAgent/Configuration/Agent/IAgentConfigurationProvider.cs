namespace PgBackupAgent.Configuration.Agent
{
    /// <summary>
    /// Interface for configuration providers.
    /// </summary>
    public interface IAgentConfigurationProvider
    {
        /// <summary>
        /// Gets the backup configuration.
        /// </summary>
        /// <returns>The backup configuration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration cannot be loaded or is invalid.</exception>
        AgentConfiguration GetConfiguration();
    }
}