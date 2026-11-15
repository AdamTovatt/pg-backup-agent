using System.Globalization;

namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// One database's dump, waiting to be stored: what it is called and how to produce its bytes.
    /// </summary>
    public class BackupFile
    {
        /// <summary>
        /// The extension of a dump: pg_dump's tar format, which <c>pg_restore</c> reads.
        /// </summary>
        public const string Extension = ".tar";

        /// <summary>
        /// Gets the name of the database being backed up.
        /// </summary>
        public string DatabaseName { get; }

        /// <summary>
        /// Gets the backup data stream provider.
        /// </summary>
        public IBackupDataStream BackupData { get; }

        /// <summary>
        /// Gets the filename for the backup file: <c>&lt;database&gt;_&lt;yyyy-MM-dd_HH-mm-ss&gt;.tar</c>, in UTC.
        /// </summary>
        public string Filename => string.Create(CultureInfo.InvariantCulture, $"{DatabaseName}_{CreatedAt:yyyy-MM-dd_HH-mm-ss}{Extension}");

        /// <summary>
        /// Gets when the backup was taken, in UTC.
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
        /// <param name="createdAt">When the backup was taken, in UTC.</param>
        /// <param name="estimatedSizeBytes">The estimated size of the backup in bytes.</param>
        public BackupFile(string databaseName, IBackupDataStream backupData, DateTime createdAt, long estimatedSizeBytes)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentException("Database name cannot be null or empty.", nameof(databaseName));

            if (backupData is null)
                throw new ArgumentNullException(nameof(backupData));

            DatabaseName = databaseName;
            BackupData = backupData;
            CreatedAt = createdAt;
            EstimatedSizeBytes = estimatedSizeBytes;
        }

        /// <summary>
        /// Where the dump is stored on the shelf: <c>/&lt;source&gt;/YYYY/MM/DD/&lt;filename&gt;</c>, dated by
        /// <see cref="CreatedAt"/>.
        /// </summary>
        /// <param name="sourceFolder">The normalized path of the source's folder, <c>/&lt;source&gt;</c>.</param>
        /// <exception cref="ArgumentException">The database's name cannot name a file on a shelf.</exception>
        public string ShelfPathUnder(string sourceFolder)
        {
            string dayFolder = string.Create(CultureInfo.InvariantCulture, $"{sourceFolder}/{CreatedAt:yyyy}/{CreatedAt:MM}/{CreatedAt:dd}");
            return ByteShelfCommon.ShelfPath.Combine(dayFolder, Filename);
        }
    }
}
