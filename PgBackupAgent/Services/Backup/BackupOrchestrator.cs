using ByteShelfClient;
using ByteShelfCommon;
using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Dumps every database to the source's folder on the shelf, <c>/&lt;source&gt;/YYYY/MM/DD/</c>, then
    /// prunes that folder by the retention policy. Nothing outside the source's folder is touched, so
    /// agents for different servers can share a shelf.
    /// </summary>
    public class BackupOrchestrator : IBackupOrchestrator
    {
        private readonly IDatabaseBackupService _databaseBackupService;
        private readonly IShelfClient _shelf;
        private readonly RetentionPolicy _retentionPolicy;
        private readonly BackupSettings _backupSettings;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<BackupOrchestrator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupOrchestrator"/> class.
        /// </summary>
        /// <param name="databaseBackupService">The database backup service.</param>
        /// <param name="shelf">The shelf the backups are stored on.</param>
        /// <param name="retentionPolicy">The retention policy for pruning.</param>
        /// <param name="backupSettings">The backup settings, naming the source.</param>
        /// <param name="timeProvider">The clock pruning judges each file's age by.</param>
        /// <param name="logger">The logger instance.</param>
        public BackupOrchestrator(
            IDatabaseBackupService databaseBackupService,
            IShelfClient shelf,
            RetentionPolicy retentionPolicy,
            BackupSettings backupSettings,
            TimeProvider timeProvider,
            ILogger<BackupOrchestrator> logger)
        {
            ArgumentNullException.ThrowIfNull(databaseBackupService);
            ArgumentNullException.ThrowIfNull(shelf);
            ArgumentNullException.ThrowIfNull(retentionPolicy);
            ArgumentNullException.ThrowIfNull(backupSettings);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _databaseBackupService = databaseBackupService;
            _shelf = shelf;
            _retentionPolicy = retentionPolicy;
            _backupSettings = backupSettings;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        /// <summary>
        /// The source's folder on the shelf, <c>/&lt;source&gt;</c>.
        /// </summary>
        private string SourceFolder => ShelfPath.Combine(ShelfPath.Root, _backupSettings.Source);

        /// <summary>
        /// Backs up every database, then prunes the source's folder unless a database failed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>What the run stored, what failed, and how pruning went.</returns>
        public async Task<BackupReport> PerformBackupAsync(CancellationToken cancellationToken = default)
        {
            List<string> databases = await _databaseBackupService.GetDatabasesToBackupAsync(cancellationToken);
            _logger.LogInformation("Found {DatabaseCount} databases to back up to {SourceFolder} on shelf {Shelf}", databases.Count, SourceFolder, _shelf.Name);

            List<string> storedPaths = new List<string>();
            List<string> failedDatabases = new List<string>();

            // One database at a time, so only one dump is held in memory.
            foreach (string databaseName in databases)
            {
                try
                {
                    BackupFile backupFile = _databaseBackupService.CreateBackup(databaseName, cancellationToken);
                    ShelfFile stored = await StoreAsync(backupFile, cancellationToken);
                    storedPaths.Add(stored.Path);
                    _logger.LogInformation("Stored {DatabaseName} at {Path} ({SizeBytes} bytes)", databaseName, stored.Path, stored.SizeBytes);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    failedDatabases.Add(databaseName);
                    _logger.LogError(ex, "Failed to back up database {DatabaseName}", databaseName);
                }
            }

            PruningOutcome pruning;
            if (failedDatabases.Count > 0)
            {
                // A failing backup must not also thin out the backups that still work.
                pruning = PruningOutcome.SkippedAfterFailedBackup;
                _logger.LogWarning("Not pruning, since {FailedCount} of {DatabaseCount} databases failed: {FailedDatabases}",
                    failedDatabases.Count, databases.Count, string.Join(", ", failedDatabases));
            }
            else
            {
                pruning = await PruneAsync(cancellationToken);
            }

            return new BackupReport(storedPaths, failedDatabases, pruning);
        }

        /// <summary>
        /// Dumps a database and stores the dump at its path under the source's folder, typed by its
        /// <c>.tar</c> extension.
        /// </summary>
        private async Task<ShelfFile> StoreAsync(BackupFile backupFile, CancellationToken cancellationToken)
        {
            string path = backupFile.ShelfPathUnder(SourceFolder);
            await using Stream content = await backupFile.BackupData.CreateStreamAsync(cancellationToken);
            return await _shelf.UploadAsync(path, content, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes every file in the source's folder the retention policy does not keep, judged by when
        /// the file was stored, then every directory left with no file under it.
        /// </summary>
        private async Task<PruningOutcome> PruneAsync(CancellationToken cancellationToken)
        {
            try
            {
                DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
                (List<ShelfDirectory> directories, List<ShelfFile> files) = await ListSourceFolderAsync(cancellationToken);

                List<ShelfFile> kept = new List<ShelfFile>();
                int deletedFiles = 0;
                foreach (ShelfFile file in files)
                {
                    if (_retentionPolicy.ShouldKeepFile(file.CreatedAt.UtcDateTime, now))
                    {
                        kept.Add(file);
                        continue;
                    }

                    await _shelf.DeleteFileAsync(file.Id, cancellationToken);
                    deletedFiles++;
                    _logger.LogInformation("Pruned {Path}, stored {CreatedAt:u}", file.Path, file.CreatedAt);
                }

                int deletedDirectories = 0;
                foreach (ShelfDirectory directory in TopmostEmptyDirectories(directories, kept))
                {
                    // Deleting the topmost empty directory takes the empty ones below it in the same deletion.
                    await _shelf.DeleteDirectoryAsync(directory.Id, cancellationToken);
                    deletedDirectories++;
                    _logger.LogInformation("Removed empty directory {Path}", directory.Path);
                }

                _logger.LogInformation("Pruned {DeletedFiles} of {FileCount} files and {DeletedDirectories} empty directories in {SourceFolder}",
                    deletedFiles, files.Count, deletedDirectories, SourceFolder);
                return PruningOutcome.Pruned;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Pruning {SourceFolder} failed", SourceFolder);
                return PruningOutcome.Failed;
            }
        }

        /// <summary>
        /// Every live directory and file under the source's folder, at any depth, by path.
        /// </summary>
        private async Task<(List<ShelfDirectory> Directories, List<ShelfFile> Files)> ListSourceFolderAsync(CancellationToken cancellationToken)
        {
            List<ShelfDirectory> directories = new List<ShelfDirectory>();
            List<ShelfFile> files = new List<ShelfFile>();
            string? cursor = null;

            do
            {
                DirectoryTree page = await _shelf.ListTreeAsync(new TreeQuery { Under = SourceFolder, Limit = TreeQuery.MaxLimit, After = cursor }, cancellationToken);
                directories.AddRange(page.Directories);
                files.AddRange(page.Files);
                cursor = page.NextCursor;
            }
            while (cursor != null);

            return (directories, files);
        }

        /// <summary>
        /// The directories with no kept file under them, leaving out those inside another such directory.
        /// </summary>
        private static List<ShelfDirectory> TopmostEmptyDirectories(List<ShelfDirectory> directories, List<ShelfFile> keptFiles)
        {
            HashSet<string> occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (ShelfFile file in keptFiles)
            {
                for (string parent = ShelfPath.GetParent(file.Path); parent != ShelfPath.Root; parent = ShelfPath.GetParent(parent))
                {
                    if (!occupied.Add(parent))
                    {
                        break;
                    }
                }
            }

            HashSet<string> empty = directories.Where(directory => !occupied.Contains(directory.Path)).Select(directory => directory.Path).ToHashSet(StringComparer.Ordinal);
            return directories
                .Where(directory => empty.Contains(directory.Path) && !empty.Contains(ShelfPath.GetParent(directory.Path)))
                .ToList();
        }
    }
}
