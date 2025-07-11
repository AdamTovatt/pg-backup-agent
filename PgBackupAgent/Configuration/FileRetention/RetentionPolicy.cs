namespace PgBackupAgent.Configuration.FileRetention
{
    /// <summary>
    /// Retention policy configuration for backup pruning.
    /// </summary>
    public class RetentionPolicy
    {
        /// <summary>
        /// List of retention rules, evaluated in order.
        /// </summary>
        public List<RetentionRule> Rules { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetentionPolicy"/> class.
        /// </summary>
        /// <param name="rules">List of retention rules.</param>
        public RetentionPolicy(List<RetentionRule> rules)
        {
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));

            Rules = rules;
        }

        /// <summary>
        /// Fixed reference date for retention calculations.
        /// </summary>
        private static readonly DateTime ReferenceDate = new DateTime(2024, 1, 1, 0, 0, 0);

        /// <summary>
        /// Determines if a file should be kept based on the retention policy.
        /// </summary>
        /// <param name="fileDate">The date of the file to check.</param>
        /// <param name="currentDate">The current date for duration calculations.</param>
        /// <returns>True if the file should be kept, false otherwise.</returns>
        public bool ShouldKeepFile(DateTime fileDate, DateTime currentDate)
        {
            foreach (RetentionRule rule in Rules)
            {
                TimeSpan keepEveryTimeSpan = rule.KeepEveryTimeSpan;
                TimeSpan? durationTimeSpan = rule.DurationTimeSpan;

                bool withinDuration = durationTimeSpan == null ||
                                    fileDate >= currentDate - durationTimeSpan.Value;

                if (!withinDuration)
                    continue;

                double daysFromReference = (fileDate - ReferenceDate).TotalDays;
                int sectionNumber = (int)Math.Floor(daysFromReference / keepEveryTimeSpan.TotalDays);

                DateTime sectionStart = ReferenceDate.AddDays(sectionNumber * keepEveryTimeSpan.TotalDays);
                DateTime sectionEnd = sectionStart.Add(keepEveryTimeSpan);

                bool inKeepSection = fileDate >= sectionStart && fileDate < sectionEnd;

                if (inKeepSection && withinDuration)
                {
                    return true;
                }
            }

            return false;
        }
    }
}