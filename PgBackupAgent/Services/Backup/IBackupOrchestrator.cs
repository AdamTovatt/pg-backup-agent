namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Interface for backup orchestration services that coordinate database backups and storage.
    /// </summary>
    public interface IBackupOrchestrator
    {
        /// <summary>
        /// Backs up every database to the shelf, then prunes the source's backups unless a database failed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>What the run stored, what failed, and how pruning went.</returns>
        Task<BackupReport> PerformBackupAsync(CancellationToken cancellationToken = default);
    }
}