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
        /// Gets a list of databases that should be backed up.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of database names to backup.</returns>
        public async Task<List<string>> GetDatabasesToBackupAsync(CancellationToken cancellationToken = default)
        {
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

            // Filter out system databases
            return databases.Where(db => !IsSystemDatabase(db)).ToList();
        }

        /// <summary>
        /// Creates a backup for a specific database.
        /// </summary>
        /// <param name="databaseName">The name of the database to backup.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A backup file for the specified database.</returns>
        public BackupFile CreateBackup(string databaseName, CancellationToken cancellationToken = default)
        {
            DateTime createdAt = DateTime.UtcNow;

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

            return new BackupFile(
                databaseName,
                backupDataStream,
                backupDataStream.Filename,
                createdAt,
                backupDataStream.EstimatedSizeBytes
            );
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

            // Get list of databases to backup
            List<string> databases = await GetDatabasesToBackupAsync(cancellationToken);

            // Create backup for each database
            foreach (string databaseName in databases)
            {
                BackupFile backupFile = CreateBackup(databaseName, cancellationToken);
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