using ByteShelfCommon;

namespace PgBackupAgent.Configuration.Agent
{
    /// <summary>
    /// Backup operation settings.
    /// </summary>
    public class BackupSettings
    {
        /// <summary>
        /// Names the Postgres server being backed up: its dumps are stored under <c>/&lt;source&gt;/</c> on the
        /// shelf, and pruning touches nothing outside that folder. Agents that back up different servers
        /// to the same shelf each need their own.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Path to the retention policy configuration file.
        /// </summary>
        public string RetentionPolicyPath { get; }

        /// <summary>
        /// How long one database's dump may take, in minutes.
        /// </summary>
        public int TimeoutMinutes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupSettings"/> class.
        /// </summary>
        /// <param name="source">Names the Postgres server being backed up; one name on a shelf path.</param>
        /// <param name="retentionPolicyPath">Path to the retention policy configuration file.</param>
        /// <param name="timeoutMinutes">How long one database's dump may take, in minutes.</param>
        /// <exception cref="ArgumentException">A value is missing or not allowed.</exception>
        public BackupSettings(string source, string retentionPolicyPath, int timeoutMinutes)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Backup.Source is required.", nameof(source));
            if (!ShelfPath.TryValidateName(source, out string? sourceError))
                throw new ArgumentException($"Backup.Source '{source}' cannot name a folder on a shelf: {sourceError}", nameof(source));
            if (string.IsNullOrWhiteSpace(retentionPolicyPath))
                throw new ArgumentException("Backup.RetentionPolicyPath is required.", nameof(retentionPolicyPath));
            if (timeoutMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMinutes), timeoutMinutes, "Backup.TimeoutMinutes must be greater than 0.");

            Source = source;
            RetentionPolicyPath = retentionPolicyPath;
            TimeoutMinutes = timeoutMinutes;
        }
    }
}
