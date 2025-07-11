namespace PgBackupAgent.Configuration.Agent
{
    /// <summary>
    /// Backup operation settings.
    /// </summary>
    public class BackupSettings
    {
        /// <summary>
        /// Path to the retention policy configuration file.
        /// </summary>
        public string RetentionPolicyPath { get; set; }

        /// <summary>
        /// Backup timeout in minutes.
        /// </summary>
        public int TimeoutMinutes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupSettings"/> class.
        /// </summary>
        /// <param name="retentionPolicyPath">Path to the retention policy configuration file.</param>
        /// <param name="timeoutMinutes">Backup timeout in minutes.</param>
        public BackupSettings(string retentionPolicyPath, int timeoutMinutes)
        {
            if (retentionPolicyPath is null)
                throw new ArgumentNullException(nameof(retentionPolicyPath));
            if (string.IsNullOrEmpty(retentionPolicyPath))
                throw new ArgumentException(nameof(retentionPolicyPath));
            if (timeoutMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMinutes));

            RetentionPolicyPath = retentionPolicyPath;
            TimeoutMinutes = timeoutMinutes;
        }
    }
} 