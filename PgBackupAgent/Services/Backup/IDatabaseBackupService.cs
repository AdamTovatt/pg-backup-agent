namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Interface for database backup services that can create backups of multiple databases.
    /// </summary>
    public interface IDatabaseBackupService
    {
        /// <summary>
        /// Creates backups for all databases on the server.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An enumerable of backup files.</returns>
        Task<IEnumerable<BackupFile>> CreateBackupsAsync(CancellationToken cancellationToken = default);
    }
}