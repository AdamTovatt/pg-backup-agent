namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Interface for database backup services that can create backups of multiple databases.
    /// </summary>
    public interface IDatabaseBackupService
    {
        /// <summary>
        /// Gets a list of databases that should be backed up.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of database names to backup.</returns>
        Task<List<string>> GetDatabasesToBackupAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a backup for a specific database.
        /// </summary>
        /// <param name="databaseName">The name of the database to backup.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A backup file for the specified database.</returns>
        BackupFile CreateBackup(string databaseName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates backups for all databases on the server.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An enumerable of backup files.</returns>
        Task<IEnumerable<BackupFile>> CreateBackupsAsync(CancellationToken cancellationToken = default);
    }
}