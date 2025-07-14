namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Service for managing hierarchical subtenant structures for organizing backup files.
    /// </summary>
    public interface ISubtenantStructureService
    {
        /// <summary>
        /// Gets or creates a subtenant structure based on a date, following the pattern: Year > Month > Day.
        /// </summary>
        /// <param name="date">The date to create the structure for.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The subtenant ID for the day level where files should be uploaded.</returns>
        Task<string> GetOrCreateDateBasedSubtenantAsync(DateTime date, CancellationToken cancellationToken = default);
    }
} 