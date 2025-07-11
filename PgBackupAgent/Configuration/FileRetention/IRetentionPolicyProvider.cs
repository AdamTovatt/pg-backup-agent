namespace PgBackupAgent.Configuration.FileRetention
{
    /// <summary>
    /// Interface for retention policy providers.
    /// </summary>
    public interface IRetentionPolicyProvider
    {
        /// <summary>
        /// Gets the retention policy.
        /// </summary>
        /// <returns>The retention policy.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the retention policy cannot be loaded or is invalid.</exception>
        RetentionPolicy GetRetentionPolicy();
    }
}