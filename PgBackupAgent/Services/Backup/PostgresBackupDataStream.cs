using PgDump;

namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Implementation of IBackupDataStream for PostgreSQL databases using PgClient.
    /// </summary>
    public class PostgresBackupDataStream : IBackupDataStream
    {
        private readonly PgClient _pgClient;
        private readonly string _databaseName;
        private readonly DateTime _createdAt;
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Gets the name of the database being backed up.
        /// </summary>
        public string DatabaseName => _databaseName;

        /// <summary>
        /// Gets the creation timestamp of the backup.
        /// </summary>
        public DateTime CreatedAt => _createdAt;

        /// <summary>
        /// Gets the estimated size of the backup in bytes.
        /// </summary>
        public long EstimatedSizeBytes => 0; // Could estimate based on database size in the future

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresBackupDataStream"/> class.
        /// </summary>
        /// <param name="pgClient">The PgClient instance for database operations.</param>
        /// <param name="databaseName">The name of the database to backup.</param>
        /// <param name="createdAt">The creation timestamp of the backup.</param>
        /// <param name="timeout">How long the dump may take.</param>
        public PostgresBackupDataStream(PgClient pgClient, string databaseName, DateTime createdAt, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(pgClient);

            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentException("Database name cannot be null or empty.", nameof(databaseName));

            _pgClient = pgClient;
            _databaseName = databaseName;
            _createdAt = createdAt;
            _timeout = timeout;
        }

        /// <summary>
        /// Creates a stream containing the backup data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A stream containing the backup data.</returns>
        public async Task<Stream> CreateStreamAsync(CancellationToken cancellationToken = default)
        {
            // Use MemoryStream but ensure it's properly disposed
            MemoryStream memoryStream = new MemoryStream();
            
            try
            {
                StreamOutputProvider outputProvider = new StreamOutputProvider(memoryStream);
                await _pgClient.DumpAsync(outputProvider, _timeout, DumpFormat.Tar, cancellationToken);
                
                memoryStream.Position = 0; // Reset position for reading
                return memoryStream;
            }
            catch
            {
                // If anything goes wrong, make sure we clean up the memory stream
                memoryStream.Dispose();
                throw;
            }
        }
    }
}