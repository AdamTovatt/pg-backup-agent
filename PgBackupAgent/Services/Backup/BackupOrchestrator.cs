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
        private readonly ISubtenantStructureService _subtenantStructureService;
        private readonly RetentionPolicy _retentionPolicy;
        private readonly BackupSettings _backupSettings;
        private readonly ILogger<BackupOrchestrator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupOrchestrator"/> class.
        /// </summary>
        /// <param name="databaseBackupService">The database backup service.</param>
        /// <param name="shelfFileProvider">The ByteShelf file provider.</param>
        /// <param name="subtenantStructureService">The subtenant structure service.</param>
        /// <param name="retentionPolicy">The retention policy for cleanup.</param>
        /// <param name="backupSettings">The backup settings.</param>
        /// <param name="logger">The logger instance.</param>
        public BackupOrchestrator(
            IDatabaseBackupService databaseBackupService,
            IShelfFileProvider shelfFileProvider,
            ISubtenantStructureService subtenantStructureService,
            RetentionPolicy retentionPolicy,
            BackupSettings backupSettings,
            ILogger<BackupOrchestrator> logger)
        {
            ArgumentNullException.ThrowIfNull(databaseBackupService);
            ArgumentNullException.ThrowIfNull(shelfFileProvider);
            ArgumentNullException.ThrowIfNull(subtenantStructureService);
            ArgumentNullException.ThrowIfNull(retentionPolicy);
            ArgumentNullException.ThrowIfNull(backupSettings);
            ArgumentNullException.ThrowIfNull(logger);

            _databaseBackupService = databaseBackupService;
            _shelfFileProvider = shelfFileProvider;
            _subtenantStructureService = subtenantStructureService;
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

            // Get target subtenant ID for the current date
            string targetSubtenantId = await _subtenantStructureService.GetOrCreateDateBasedSubtenantAsync(currentDate, cancellationToken);

            // Get list of databases to backup
            List<string> databases = await _databaseBackupService.GetDatabasesToBackupAsync(cancellationToken);
            _logger.LogInformation("Found {DatabaseCount} databases to backup", databases.Count);

            // Process each database individually to minimize memory usage
            foreach (string databaseName in databases)
            {
                try
                {
                    // Create backup for this specific database
                    BackupFile backupFile = _databaseBackupService.CreateBackup(databaseName, cancellationToken);

                    await UploadBackupFileAsync(backupFile, targetSubtenantId, cancellationToken);
                    _logger.LogInformation("Successfully backed up database: {DatabaseName}", databaseName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to backup database: {DatabaseName}", databaseName);
                }
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
        /// <param name="targetTenantId">The tenant to upload the backup file to.</param>
        /// <returns>A task representing the upload operation.</returns>
        private async Task UploadBackupFileAsync(BackupFile backupFile, string targetTenantId, CancellationToken cancellationToken)
        {
            using Stream backupStream = await backupFile.BackupData.CreateStreamAsync(cancellationToken);

            try
            {
                // Upload to ByteShelf with appropriate content type
                Guid fileId = await _shelfFileProvider.WriteFileForTenantAsync(
                    targetTenantId: targetTenantId,
                    originalFilename: backupFile.Filename,
                    contentType: "application/sql",
                    content: backupStream,
                    cancellationToken: cancellationToken);

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
            // Get all subtenants to traverse the hierarchical structure
            Dictionary<string, TenantInfoResponse> allSubtenants = await _shelfFileProvider.GetSubTenantsAsync(cancellationToken);

            // Process each year subtenant
            foreach (KeyValuePair<string, TenantInfoResponse> yearSubtenant in allSubtenants)
            {
                await ProcessSubtenantForRetentionAsync(yearSubtenant.Key, yearSubtenant.Value.DisplayName, currentDate, cancellationToken);
            }
        }

        /// <summary>
        /// Recursively processes subtenants for retention policy cleanup.
        /// </summary>
        /// <param name="subtenantId">The subtenant ID to process.</param>
        /// <param name="subtenantName">The display name of the subtenant.</param>
        /// <param name="currentDate">The current date for retention calculations.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the cleanup operation.</returns>
        private async Task ProcessSubtenantForRetentionAsync(string subtenantId, string subtenantName, DateTime currentDate, CancellationToken cancellationToken)
        {
            // Get files in this subtenant
            IEnumerable<ShelfFileMetadata> files = await _shelfFileProvider.GetFilesForTenantAsync(subtenantId, cancellationToken);

            int deletedFilesCount = 0;

            // Check each file against retention policy
            foreach (ShelfFileMetadata file in files)
            {
                bool shouldKeep = _retentionPolicy.ShouldKeepFile(file.CreatedAt.DateTime, currentDate);

                if (!shouldKeep)
                {
                    await _shelfFileProvider.DeleteFileForTenantAsync(subtenantId, file.Id, cancellationToken);
                    _logger.LogInformation("Deleted old backup: {Filename} from subtenant {SubtenantName}", file.OriginalFilename, subtenantName);
                    deletedFilesCount++;
                }
            }

            // Get subtenants under this subtenant and process them recursively
            Dictionary<string, TenantInfoResponse> childSubtenants = await _shelfFileProvider.GetSubTenantsUnderSubTenantAsync(subtenantId, cancellationToken);

            foreach (KeyValuePair<string, TenantInfoResponse> childSubtenant in childSubtenants)
            {
                await ProcessSubtenantForRetentionAsync(childSubtenant.Key, childSubtenant.Value.DisplayName, currentDate, cancellationToken);
            }

            // After processing all child subtenants, check if this subtenant should be deleted
            // Only check if we deleted files or if there were no files to begin with
            if (deletedFilesCount > 0 || !files.Any())
            {
                await CheckAndDeleteEmptySubtenantAsync(subtenantId, subtenantName, cancellationToken);
            }
        }

        /// <summary>
        /// Checks if a subtenant is empty and deletes it if so.
        /// </summary>
        /// <param name="subtenantId">The subtenant ID to check.</param>
        /// <param name="subtenantName">The display name of the subtenant.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A task representing the cleanup operation.</returns>
        private async Task CheckAndDeleteEmptySubtenantAsync(string subtenantId, string subtenantName, CancellationToken cancellationToken)
        {
            // Get remaining files in this subtenant
            IEnumerable<ShelfFileMetadata> remainingFiles = await _shelfFileProvider.GetFilesForTenantAsync(subtenantId, cancellationToken);

            // If there are files remaining, we can't delete the subtenant
            if (remainingFiles.Any())
                return;

            // Get remaining child subtenants
            Dictionary<string, TenantInfoResponse> remainingChildSubtenants = await _shelfFileProvider.GetSubTenantsUnderSubTenantAsync(subtenantId, cancellationToken);

            // Delete subtenant if it has no child subtenants
            if (!remainingChildSubtenants.Any())
            {
                try
                {
                    await _shelfFileProvider.DeleteSubTenantAsync(subtenantId, cancellationToken);
                    _logger.LogInformation("Deleted empty subtenant: {SubtenantName} (ID: {SubtenantId})", subtenantName, subtenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Tried to delete empty subtenant {subtenantName} ({subtenantId}) but failed with exception: {ex.Message}");
                }
            }
        }
    }
}