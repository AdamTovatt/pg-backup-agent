namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Interface for backup data streams that can create streams on-demand.
    /// </summary>
    public interface IBackupDataStream
    {
        /// <summary>
        /// Gets the name of the database being backed up.
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// Gets the filename for the backup file.
        /// </summary>
        string Filename { get; }

        /// <summary>
        /// Gets the creation timestamp of the backup.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the estimated size of the backup in bytes.
        /// </summary>
        long EstimatedSizeBytes { get; }

        /// <summary>
        /// Creates a stream containing the backup data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A stream containing the backup data.</returns>
        Task<Stream> CreateStreamAsync(CancellationToken cancellationToken = default);
    }
}