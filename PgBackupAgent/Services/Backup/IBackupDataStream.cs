namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Produces a backup's bytes when they are needed. What the backup is, <see cref="BackupFile"/> holds.
    /// </summary>
    public interface IBackupDataStream
    {
        /// <summary>
        /// Creates a stream containing the backup data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A stream containing the backup data.</returns>
        Task<Stream> CreateStreamAsync(CancellationToken cancellationToken = default);
    }
}
