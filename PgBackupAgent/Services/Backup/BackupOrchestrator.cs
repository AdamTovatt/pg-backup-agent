using ByteShelfClient;
using ByteShelfCommon;
using Microsoft.Extensions.Logging;
using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Implementation of IBackupOrchestrator that coordinates database backups and ByteShelf storage.
    /// </summary>
    public class BackupOrchestrator : IBackupOrchestrator
    {
        private readonly IDatabaseBackupService _databaseBackupService;
        private readonly IShelfFileProvider _shelfFileProvider;
        private readonly RetentionPolicy _retentionPolicy;
        private readonly BackupSettings _backupSettings;
        private readonly ILogger<BackupOrchestrator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupOrchestrator"/> class.
        /// </summary>
        /// <param name="databaseBackupService">The database backup service.</param>
        /// <param name="shelfFileProvider">The ByteShelf file provider.</param>
        /// <param name="retentionPolicy">The retention policy for cleanup.</param>
        /// <param name="backupSettings">The backup settings.</param>
        /// <param name="logger">The logger instance.</param>
        public BackupOrchestrator(
            IDatabaseBackupService databaseBackupService,
            IShelfFileProvider shelfFileProvider,
            RetentionPolicy retentionPolicy,
            BackupSettings backupSettings,
            ILogger<BackupOrchestrator> logger)
        {
            if (databaseBackupService is null)
                throw new ArgumentNullException(nameof(databaseBackupService));

            if (shelfFileProvider is null)
                throw new ArgumentNullException(nameof(shelfFileProvider));

            if (retentionPolicy is null)
                throw new ArgumentNullException(nameof(retentionPolicy));

            if (backupSettings is null)
                throw new ArgumentNullException(nameof(backupSettings));

            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

            _databaseBackupService = databaseBackupService;
            _shelfFileProvider = shelfFileProvider;
            _retentionPolicy = retentionPolicy;
            _backupSettings = backupSettings;
            _logger = logger;
        }

        /// <summary>
        /// Performs a complete backup operation including database backup and storage upload.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the backup operation.</returns>
        public async Task PerformBackupAsync(CancellationToken cancellationToken = default)
        {
            DateTime currentDate = DateTime.UtcNow;
            _logger.LogInformation("Starting backup operation at {CurrentDate}", currentDate);

            // Create backups for all databases
            IEnumerable<BackupFile> backupFiles = await _databaseBackupService.CreateBackupsAsync(cancellationToken);
            _logger.LogInformation("Created {BackupCount} database backups", backupFiles.Count());

            // Upload each backup to ByteShelf
            foreach (BackupFile backupFile in backupFiles)
            {
                await UploadBackupFileAsync(backupFile, cancellationToken);
            }

            // Apply retention policy to clean up old backups
            await ApplyRetentionPolicyAsync(currentDate, cancellationToken);

            _logger.LogInformation("Completed backup operation");
        }

        /// <summary>
        /// Uploads a backup file to ByteShelf storage.
        /// </summary>
        /// <param name="backupFile">The backup file to upload.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the upload operation.</returns>
        private async Task UploadBackupFileAsync(BackupFile backupFile, CancellationToken cancellationToken)
        {
            using Stream backupStream = await backupFile.BackupData.CreateStreamAsync(cancellationToken);

            try
            {
                // Upload to ByteShelf with appropriate content type
                Guid fileId = await _shelfFileProvider.WriteFileAsync(
                    backupFile.Filename,
                    "application/sql",
                    backupStream,
                    cancellationToken);

                _logger.LogInformation("Uploaded backup {Filename} with ID: {FileId}", backupFile.Filename, fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when uploading backup {backupFile.Filename} with {_shelfFileProvider.GetType().Name}");
            }
        }

        /// <summary>
        /// Applies the retention policy to clean up old backups.
        /// </summary>
        /// <param name="currentDate">The current date for retention calculations.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the cleanup operation.</returns>
        private async Task ApplyRetentionPolicyAsync(DateTime currentDate, CancellationToken cancellationToken)
        {
            // Get all files from ByteShelf
            IEnumerable<ShelfFileMetadata> allFiles = await _shelfFileProvider.GetFilesAsync(cancellationToken);

            // Check each file against retention policy
            foreach (ShelfFileMetadata file in allFiles)
            {
                // Extract date from filename (assuming format: database_yyyy-MM-dd_HH-mm-ss.sql)
                if (TryExtractDateFromFilename(file.OriginalFilename, out DateTime fileDate))
                {
                    bool shouldKeep = _retentionPolicy.ShouldKeepFile(fileDate, currentDate);

                    if (!shouldKeep)
                    {
                        await _shelfFileProvider.DeleteFileAsync(file.Id, cancellationToken);
                        _logger.LogInformation("Deleted old backup: {Filename}", file.OriginalFilename);
                    }
                }
            }
        }

        /// <summary>
        /// Tries to extract a date from a backup filename.
        /// </summary>
        /// <param name="filename">The filename to parse.</param>
        /// <param name="fileDate">The extracted date, if successful.</param>
        /// <returns>True if a date was successfully extracted, false otherwise.</returns>
        private static bool TryExtractDateFromFilename(string filename, out DateTime fileDate)
        {
            fileDate = DateTime.MinValue;

            if (string.IsNullOrEmpty(filename))
                return false;

            // Look for date pattern: yyyy-MM-dd_HH-mm-ss
            string[] parts = filename.Split('_');
            if (parts.Length < 3)
                return false;

            try
            {
                // Combine date and time parts
                string datePart = parts[parts.Length - 3]; // yyyy-MM-dd
                string timePart = parts[parts.Length - 2]; // HH-mm-ss

                string dateTimeString = $"{datePart}_{timePart}";
                fileDate = DateTime.ParseExact(dateTimeString, "yyyy-MM-dd_HH-mm-ss", null);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}