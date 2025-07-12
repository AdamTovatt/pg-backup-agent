namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Represents a backup file with metadata and data stream.
    /// </summary>
    public class BackupFile
    {
        /// <summary>
        /// Gets the name of the database being backed up.
        /// </summary>
        public string DatabaseName { get; }

        /// <summary>
        /// Gets the backup data stream provider.
        /// </summary>
        public IBackupDataStream BackupData { get; }

        /// <summary>
        /// Gets the filename for the backup file.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Gets the creation timestamp of the backup.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the estimated size of the backup in bytes.
        /// </summary>
        public long EstimatedSizeBytes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupFile"/> class.
        /// </summary>
        /// <param name="databaseName">The name of the database being backed up.</param>
        /// <param name="backupData">The backup data stream provider.</param>
        /// <param name="filename">The filename for the backup file.</param>
        /// <param name="createdAt">The creation timestamp of the backup.</param>
        /// <param name="estimatedSizeBytes">The estimated size of the backup in bytes.</param>
        public BackupFile(string databaseName, IBackupDataStream backupData, string filename, DateTime createdAt, long estimatedSizeBytes)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentException("Database name cannot be null or empty.", nameof(databaseName));

            if (backupData is null)
                throw new ArgumentNullException(nameof(backupData));

            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));

            DatabaseName = databaseName;
            BackupData = backupData;
            Filename = filename;
            CreatedAt = createdAt;
            EstimatedSizeBytes = estimatedSizeBytes;
        }
    }
}