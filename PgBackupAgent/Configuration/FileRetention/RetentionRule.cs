namespace PgBackupAgent.Configuration.FileRetention
{
    /// <summary>
    /// Individual retention rule defining backup retention for a specific time period.
    /// </summary>
    public class RetentionRule
    {
        /// <summary>
        /// Minimum spacing between backups within the range (TimeSpan format).
        /// </summary>
        public string KeepEvery { get; set; }

        /// <summary>
        /// How far back from now the rule applies (TimeSpan format).
        /// Null means this rule applies indefinitely.
        /// </summary>
        public string? Duration { get; set; }

        /// <summary>
        /// Gets the KeepEvery value as a TimeSpan.
        /// </summary>
        public TimeSpan KeepEveryTimeSpan => TimeSpan.Parse(KeepEvery);

        /// <summary>
        /// Gets the Duration value as a TimeSpan, or null if Duration is null.
        /// </summary>
        public TimeSpan? DurationTimeSpan => Duration != null ? TimeSpan.Parse(Duration) : null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetentionRule"/> class.
        /// </summary>
        /// <param name="keepEvery">Minimum spacing between backups within the range (TimeSpan format).</param>
        /// <param name="duration">How far back from now the rule applies (TimeSpan format). Null means this rule applies indefinitely.</param>
        public RetentionRule(string keepEvery, string? duration)
        {
            if (keepEvery is null)
                throw new ArgumentNullException(nameof(keepEvery));

            if (string.IsNullOrEmpty(keepEvery))
                throw new ArgumentException(nameof(keepEvery));

            KeepEvery = keepEvery;
            Duration = duration;
        }

        /// <summary>
        /// Returns a string representation of the retention rule.
        /// </summary>
        /// <returns>A string describing the retention rule.</returns>
        public override string ToString()
        {
            string durationText = Duration != null ? $" for {Duration}" : " indefinitely";
            return $"Keep every {KeepEvery}{durationText}";
        }
    }
}