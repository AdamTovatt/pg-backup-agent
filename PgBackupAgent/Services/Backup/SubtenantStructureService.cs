using ByteShelfClient;
using ByteShelfCommon;
using Microsoft.Extensions.Logging;

namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// Implementation of ISubtenantStructureService that creates hierarchical subtenant structures for organizing backup files by date.
    /// </summary>
    public class SubtenantStructureService : ISubtenantStructureService
    {
        private readonly IShelfFileProvider _shelfFileProvider;
        private readonly ILogger<SubtenantStructureService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtenantStructureService"/> class.
        /// </summary>
        /// <param name="shelfFileProvider">The ByteShelf file provider.</param>
        /// <param name="logger">The logger instance.</param>
        public SubtenantStructureService(IShelfFileProvider shelfFileProvider, ILogger<SubtenantStructureService> logger)
        {
            ArgumentNullException.ThrowIfNull(shelfFileProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _shelfFileProvider = shelfFileProvider;
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates a subtenant structure based on a date, following the pattern: Year > Month > Day.
        /// </summary>
        /// <param name="date">The date to create the structure for.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The subtenant ID for the day level where files should be uploaded.</returns>
        public async Task<string> GetOrCreateDateBasedSubtenantAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            string yearDisplayName = date.Year.ToString();
            string monthDisplayName = $"{date.Month:D2} {date:MMMM}";
            string dayDisplayName = date.Day.ToString("D2");

            _logger.LogInformation("Creating subtenant structure for date {Date}: {Year} > {Month} > {Day}",
                date.ToString("yyyy-MM-dd"), yearDisplayName, monthDisplayName, dayDisplayName);

            // Get or create year subtenant
            string yearSubtenantId = await GetOrCreateSubtenantAsync(yearDisplayName, cancellationToken);

            // Get or create month subtenant under year
            string monthSubtenantId = await GetOrCreateSubtenantUnderSubtenantAsync(yearSubtenantId, monthDisplayName, cancellationToken);

            // Get or create day subtenant under month
            string daySubtenantId = await GetOrCreateSubtenantUnderSubtenantAsync(monthSubtenantId, dayDisplayName, cancellationToken);

            _logger.LogInformation("Subtenant structure created successfully. Day subtenant ID: {DaySubtenantId}", daySubtenantId);
            return daySubtenantId;
        }

        /// <summary>
        /// Gets or creates a subtenant at the top level.
        /// </summary>
        /// <param name="displayName">The display name for the subtenant.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The subtenant ID.</returns>
        private async Task<string> GetOrCreateSubtenantAsync(string displayName, CancellationToken cancellationToken)
        {
            Dictionary<string, TenantInfoResponse> subtenants = await _shelfFileProvider.GetSubTenantsAsync(cancellationToken);

            // Look for existing subtenant with matching display name
            KeyValuePair<string, TenantInfoResponse>? existingSubtenant = subtenants
                .FirstOrDefault(kvp => kvp.Value.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));

            if (IsNonEmptyValue(existingSubtenant))
            {
                _logger.LogDebug("Found existing subtenant '{DisplayName}' with ID: {SubtenantId}", displayName, existingSubtenant.Value.Key);
                return existingSubtenant.Value.Key;
            }

            // Create new subtenant
            string newSubtenantId = await _shelfFileProvider.CreateSubTenantAsync(displayName, cancellationToken);
            _logger.LogInformation("Created new subtenant '{DisplayName}' with ID: {SubtenantId}", displayName, newSubtenantId);
            return newSubtenantId;
        }

        /// <summary>
        /// Gets or creates a subtenant under a parent subtenant.
        /// </summary>
        /// <param name="parentSubtenantId">The parent subtenant ID.</param>
        /// <param name="displayName">The display name for the subtenant.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The subtenant ID.</returns>
        private async Task<string> GetOrCreateSubtenantUnderSubtenantAsync(string parentSubtenantId, string displayName, CancellationToken cancellationToken)
        {
            Dictionary<string, TenantInfoResponse> subtenants = await _shelfFileProvider.GetSubTenantsUnderSubTenantAsync(parentSubtenantId, cancellationToken);

            // Look for existing subtenant with matching display name
            KeyValuePair<string, TenantInfoResponse>? existingSubtenant = subtenants
                .FirstOrDefault(kvp => kvp.Value.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));

            if (IsNonEmptyValue(existingSubtenant))
            {
                _logger.LogDebug("Found existing subtenant '{DisplayName}' under parent {ParentId} with ID: {SubtenantId}",
                    displayName, parentSubtenantId, existingSubtenant.Value.Key);
                return existingSubtenant.Value.Key;
            }

            // Create new subtenant under parent
            string newSubtenantId = await _shelfFileProvider.CreateSubTenantUnderSubTenantAsync(parentSubtenantId, displayName, cancellationToken);
            _logger.LogInformation("Created new subtenant '{DisplayName}' under parent {ParentId} with ID: {SubtenantId}",
                displayName, parentSubtenantId, newSubtenantId);
            return newSubtenantId;
        }

        private bool IsNonEmptyValue(KeyValuePair<string, TenantInfoResponse>? keyValuePair)
        {
            return keyValuePair.HasValue && keyValuePair.Value.Key != null && keyValuePair.Value.Value != null;
        }
    }
}