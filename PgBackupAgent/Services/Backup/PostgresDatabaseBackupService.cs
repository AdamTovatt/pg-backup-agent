using PgDump;
using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Implementation of IDatabaseBackupService for PostgreSQL databases using PgClient.
    /// </summary>
    public class PostgresDatabaseBackupService : IDatabaseBackupService
    {
        private readonly PostgresSettings _postgresSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresDatabaseBackupService"/> class.
        /// </summary>
        /// <param name="postgresSettings">The PostgreSQL connection settings.</param>
        public PostgresDatabaseBackupService(PostgresSettings postgresSettings)
        {
            ArgumentNullException.ThrowIfNull(postgresSettings);

            _postgresSettings = postgresSettings;
        }

        /// <summary>
        /// Creates backups for all databases on the server.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An enumerable of backup files.</returns>
        public async Task<IEnumerable<BackupFile>> CreateBackupsAsync(CancellationToken cancellationToken = default)
        {
            List<BackupFile> backupFiles = new List<BackupFile>();
            DateTime createdAt = DateTime.UtcNow;

            // Create connection options for listing databases
            ConnectionOptions connectionOptions = new ConnectionOptions(
                _postgresSettings.Host,
                _postgresSettings.Port,
                _postgresSettings.Username,
                _postgresSettings.Password,
                "postgres" // Use postgres database for listing other databases
            );

            PgClient pgClient = new PgClient(connectionOptions);

            // List all databases
            List<string> databases = await pgClient.ListDatabasesAsync(TimeSpan.FromSeconds(30), cancellationToken);

            // Create backup for each database
            foreach (string databaseName in databases)
            {
                // Skip system databases
                if (IsSystemDatabase(databaseName))
                    continue;

                // Create connection options for the specific database
                ConnectionOptions databaseConnectionOptions = new ConnectionOptions(
                    _postgresSettings.Host,
                    _postgresSettings.Port,
                    _postgresSettings.Username,
                    _postgresSettings.Password,
                    databaseName
                );

                PgClient databasePgClient = new PgClient(databaseConnectionOptions);
                PostgresBackupDataStream backupDataStream = new PostgresBackupDataStream(databasePgClient, databaseName, createdAt);

                BackupFile backupFile = new BackupFile(
                    databaseName,
                    backupDataStream,
                    backupDataStream.Filename,
                    createdAt,
                    backupDataStream.EstimatedSizeBytes
                );

                backupFiles.Add(backupFile);
            }

            return backupFiles;
        }

        /// <summary>
        /// Determines if a database is a system database that should be skipped.
        /// </summary>
        /// <param name="databaseName">The name of the database to check.</param>
        /// <returns>True if the database is a system database, false otherwise.</returns>
        private static bool IsSystemDatabase(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                return true;

            // Skip PostgreSQL system databases
            string[] systemDatabases = { "template0", "template1", "postgres" };
            return systemDatabases.Contains(databaseName, StringComparer.OrdinalIgnoreCase);
        }
    }
}