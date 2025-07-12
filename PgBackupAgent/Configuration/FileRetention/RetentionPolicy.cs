using System.Linq;

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

            ValidateRuleSequence(rules);
            Rules = rules;
        }

        /// <summary>
        /// Validates that the retention rules form a proper sequence where each rule's interval
        /// is a multiple of the previous rule's interval.
        /// </summary>
        /// <param name="rules">The rules to validate.</param>
        /// <exception cref="ArgumentException">Thrown when rules do not form a proper sequence.</exception>
        private static void ValidateRuleSequence(List<RetentionRule> rules)
        {
            if (rules.Count <= 1)
                return;

            TimeSpan? previousInterval = null;

            for (int i = 0; i < rules.Count; i++)
            {
                RetentionRule currentRule = rules[i];
                TimeSpan currentInterval = currentRule.KeepEveryTimeSpan;

                if (previousInterval.HasValue)
                {
                    if (currentInterval.TotalDays % previousInterval.Value.TotalDays != 0)
                    {
                        throw new ArgumentException(
                            $"Rule {i + 1} has interval {currentInterval} which is not a multiple of the previous rule's interval {previousInterval.Value}. " +
                            $"Rules must form a sequence where each interval is a multiple of the previous one.");
                    }
                }

                previousInterval = currentInterval;
            }
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
            RetentionRule? retentionRule = GetRetentionRuleByDate(fileDate, currentDate);

            if (retentionRule == null)
                return true;

            double daysFromReference = (int)(fileDate - ReferenceDate).TotalDays;
            bool shouldBeKept = daysFromReference % retentionRule.KeepEveryTimeSpan.TotalDays == 0;

            return shouldBeKept;
        }

        /// <summary>
        /// Gets the retention rule that applies to a specific date based on the current date.
        /// </summary>
        /// <param name="date">The date to find a retention rule for.</param>
        /// <param name="currentDate">The current date used for duration calculations.</param>
        /// <returns>The retention rule that applies to the given date, or null if no rule applies.</returns>
        public RetentionRule? GetRetentionRuleByDate(DateTime date, DateTime currentDate)
        {
            foreach (RetentionRule rule in Rules)
            {
                TimeSpan? durationTimeSpan = rule.DurationTimeSpan;

                bool withinDuration = durationTimeSpan == null || date >= currentDate - durationTimeSpan.Value;

                if (withinDuration)
                    return rule;
            }

            return null;
        }

        /// <summary>
        /// Returns a string representation of the retention policy.
        /// </summary>
        /// <returns>A string describing the retention policy with all its rules.</returns>
        public override string ToString()
        {
            if (Rules.Count == 0)
                return "RetentionPolicy (no rules)";

            string rulesText = string.Join("; ", Rules.Select(rule => rule.ToString()));
            return $"RetentionPolicy: {rulesText}";
        }
    }
}