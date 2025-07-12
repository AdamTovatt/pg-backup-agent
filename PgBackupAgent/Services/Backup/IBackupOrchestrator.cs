namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Interface for backup orchestration services that coordinate database backups and storage.
    /// </summary>
    public interface IBackupOrchestrator
    {
        /// <summary>
        /// Performs a complete backup operation including database backup and storage upload.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the backup operation.</returns>
        Task PerformBackupAsync(CancellationToken cancellationToken = default);
    }
}