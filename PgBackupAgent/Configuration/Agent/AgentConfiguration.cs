namespace PgBackupAgent.Configuration.Agent
{
    /// <summary>
    /// Configuration model for the PostgreSQL backup agent.
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        /// PostgreSQL connection settings.
        /// </summary>
        public PostgresSettings Postgres { get; set; }

        /// <summary>
        /// ByteShelf storage settings.
        /// </summary>
        public ByteShelfSettings ByteShelf { get; set; }

        /// <summary>
        /// Backup operation settings.
        /// </summary>
        public BackupSettings Backup { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentConfiguration"/> class.
        /// </summary>
        /// <param name="postgres">PostgreSQL connection settings.</param>
        /// <param name="byteShelf">ByteShelf storage settings.</param>
        /// <param name="backup">Backup operation settings.</param>
        public AgentConfiguration(PostgresSettings postgres, ByteShelfSettings byteShelf, BackupSettings backup)
        {
            if (postgres is null)
                throw new ArgumentNullException(nameof(postgres));
            if (byteShelf is null)
                throw new ArgumentNullException(nameof(byteShelf));
            if (backup is null)
                throw new ArgumentNullException(nameof(backup));

            Postgres = postgres;
            ByteShelf = byteShelf;
            Backup = backup;
        }
    }
}