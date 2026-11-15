namespace PgBackupAgent.Services.Backup
{
    /// <summary>
    /// What one backup run did: which databases it stored, which failed, and whether it pruned.
    /// </summary>
    /// <param name="StoredPaths">The shelf paths of the dumps stored.</param>
    /// <param name="FailedDatabases">The databases whose dump or upload failed.</param>
    /// <param name="Pruning">How pruning went.</param>
    public sealed record BackupReport(IReadOnlyList<string> StoredPaths, IReadOnlyList<string> FailedDatabases, PruningOutcome Pruning)
    {
        /// <summary>
        /// Whether every database was stored and pruning ran without failing: the run exits with 0 only then.
        /// </summary>
        public bool Succeeded => FailedDatabases.Count == 0 && Pruning == PruningOutcome.Pruned;
    }

    /// <summary>
    /// How pruning went in a backup run.
    /// </summary>
    public enum PruningOutcome
    {
        /// <summary>The retention policy was applied to the source's folder.</summary>
        Pruned,

        /// <summary>Not applied, since a database failed: old dumps stay until a run stores every database.</summary>
        SkippedAfterFailedBackup,

        /// <summary>Applying it failed partway; what it had deleted stays deleted.</summary>
        Failed,
    }
}
