using PgBackupAgent.Attributes;

namespace PgBackupAgent.Configuration
{
    /// <summary>
    /// Configuration class containing all environment variable name constants.
    /// This class is scanned during startup to validate that all required environment variables are properly configured.
    /// </summary>
    [EnvironmentVariableNameContainer]
    public static class EnvironmentVariableNames
    {
        /// <summary>
        /// Path to the backup configuration file environment variable name.
        /// </summary>
        [EnvironmentVariableName]
        public const string BackupConfigPath = "BACKUP_CONFIG_PATH";
    }
} 