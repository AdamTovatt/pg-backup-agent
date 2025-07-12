using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgentTests.Configuration.FileRetention
{
    [TestClass]
    public class RetentionPolicyTests
    {
        [TestMethod]
        public void GetRetentionRuleByDate_WithExamplePolicy_ReturnsCorrectRules()
        {
            // Arrange
            RetentionPolicy policy = CreateExampleRetentionPolicy();
            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0);

            // Act & Assert
            // Test dates within 14 days (should return first rule)
            DateTime recentDate = currentDate.AddDays(-5);
            RetentionRule? result = policy.GetRetentionRuleByDate(recentDate, currentDate);
            Assert.IsNotNull(result);
            Assert.AreEqual("1.00:00:00", result.KeepEvery);

            // Test date 20 days ago (should return second rule)
            DateTime twentyDaysAgo = currentDate.AddDays(-20);
            result = policy.GetRetentionRuleByDate(twentyDaysAgo, currentDate);
            Assert.IsNotNull(result);
            Assert.AreEqual("2.00:00:00", result.KeepEvery);

            // Test date 35 days ago (should return third rule)
            DateTime thirtyFiveDaysAgo = currentDate.AddDays(-35);
            result = policy.GetRetentionRuleByDate(thirtyFiveDaysAgo, currentDate);
            Assert.IsNotNull(result);
            Assert.AreEqual("4.00:00:00", result.KeepEvery);

            // Test date 60 days ago (should return third rule)
            DateTime sixtyDaysAgo = currentDate.AddDays(-60);
            result = policy.GetRetentionRuleByDate(sixtyDaysAgo, currentDate);
            Assert.IsNotNull(result);
            Assert.AreEqual("4.00:00:00", result.KeepEvery);

            // Test date 100 days ago (should return fourth rule)
            DateTime hundredDaysAgo = currentDate.AddDays(-100);
            result = policy.GetRetentionRuleByDate(hundredDaysAgo, currentDate);
            Assert.IsNotNull(result);
            Assert.AreEqual("8.00:00:00", result.KeepEvery);

            // Test date 200 days ago (should return fifth rule)
            DateTime twoHundredDaysAgo = currentDate.AddDays(-200);
            result = policy.GetRetentionRuleByDate(twoHundredDaysAgo, currentDate);
            Assert.IsNotNull(result);
            Assert.AreEqual("16.00:00:00", result.KeepEvery);

            // Test date 400 days ago (should return sixth rule - indefinite)
            DateTime fourHundredDaysAgo = currentDate.AddDays(-400);
            result = policy.GetRetentionRuleByDate(fourHundredDaysAgo, currentDate);
            Assert.IsNotNull(result);
            Assert.AreEqual("32.00:00:00", result.KeepEvery);
        }

        [TestMethod]
        public void RetentionPolicy_WithInvalidRuleSequence_ThrowsArgumentException()
        {
            // Arrange
            List<RetentionRule> invalidRules = new List<RetentionRule>
            {
                new RetentionRule("1.00:00:00", "14.00:00:00"),    // 1 day
                new RetentionRule("2.00:00:00", "28.00:00:00"),    // 2 days (valid - multiple of 1)
                new RetentionRule("4.00:00:00", "44.00:00:00"),    // 4 days (valid - multiple of 2)
                new RetentionRule("9.00:00:00", "74.00:00:00"),    // 9 days (invalid - not multiple of 4)
            };

            // Act & Assert
            ArgumentException exception = Assert.ThrowsException<ArgumentException>(() => new RetentionPolicy(invalidRules));
            Assert.IsTrue(exception.Message.Contains("Rule 4 has interval 9.00:00:00 which is not a multiple"));
        }

        [TestMethod]
        public void RetentionPolicy_WithValidRuleSequence_DoesNotThrow()
        {
            // Arrange
            List<RetentionRule> validRules = new List<RetentionRule>
            {
                new RetentionRule("1.00:00:00", "14.00:00:00"),    // 1 day
                new RetentionRule("2.00:00:00", "28.00:00:00"),    // 2 days (valid - multiple of 1)
                new RetentionRule("4.00:00:00", "44.00:00:00"),    // 4 days (valid - multiple of 2)
                new RetentionRule("8.00:00:00", "74.00:00:00"),    // 8 days (valid - multiple of 4)
            };

            // Act & Assert
            RetentionPolicy policy = new RetentionPolicy(validRules);
            Assert.IsNotNull(policy);
            Assert.AreEqual(4, policy.Rules.Count);
        }

        [TestMethod]
        [DataRow("2024-01-10", "2024-01-15", true)]   // 5 days ago, should be kept (daily rule)
        [DataRow("2024-01-10", "2024-01-13", true)]   // 3 days ago, should be kept (daily rule)
        [DataRow("2024-01-10", "2024-01-17", true)]   // 7 days ago, should be kept (daily rule)
        [DataRow("2024-01-10", "2024-01-23", true)]   // 13 days ago, should be kept (daily rule)
        [DataRow("2024-01-10", "2024-01-24", true)]   // 14 days ago, should be kept (daily rule)
        [DataRow("2024-01-10", "2024-01-25", false)]  // 15 days ago, should be deleted (outside daily rule period)
        [DataRow("2024-01-10", "2024-01-26", false)]  // 16 days ago, should also be deleted because the first date, 10th of january, is not on a day that's every second day to keep
        [DataRow("2024-01-10", "2024-01-27", false)]  // 17 days ago, should also be deleted because the first date, 10th of january, is not on a day that's every second day to keep
        [DataRow("2024-01-10", "2024-01-28", false)]  // 18 days ago, should also be deleted because the first date, 10th of january, is not on a day that's every second day to keep
        [DataRow("2024-01-10", "2024-01-29", false)]  // 19 days ago, should also be deleted because the first date, 10th of january, is not on a day that's every second day to keep
        [DataRow("2024-01-10", "2024-02-02", false)]  // should also be deleted, since no matter how far in time we move, the first date is still not a keep date
        [DataRow("2024-01-10", "2024-02-25", false)]  // should also be deleted, since no matter how far in time we move, the first date is still not a keep date
        [DataRow("2024-01-10", "2024-03-16", false)]  // should also be deleted, since no matter how far in time we move, the first date is still not a keep date
        [DataRow("2024-01-10", "2024-04-28", false)]  // should also be deleted, since no matter how far in time we move, the first date is still not a keep date
        [DataRow("2024-01-10", "2024-05-25", false)]  // should also be deleted, since no matter how far in time we move, the first date is still not a keep date
        [DataRow("2024-01-10", "2024-10-12", false)]  // should also be deleted, since no matter how far in time we move, the first date is still not a keep date
        [DataRow("2024-01-10", "2025-11-04", false)]  // should also be deleted, since no matter how far in time we move, the first date is still not a keep date
        [DataRow("2024-01-11", "2024-01-12", true)]   // here we change the first date, it's now a keep day for every second day
        [DataRow("2024-01-11", "2024-01-13", true)]   // should of course be keept the first days no matter what
        [DataRow("2024-01-11", "2024-01-15", true)]   // should of course be keept the first days no matter what
        [DataRow("2024-01-11", "2024-01-18", true)]   // should of course be keept the first days no matter what
        [DataRow("2024-01-11", "2024-01-22", true)]   // should of course be keept the first days no matter what
        [DataRow("2024-01-11", "2024-01-25", true)]   // 14 days after, still keep
        [DataRow("2024-01-11", "2024-01-26", true)]   // 15 days after, should now, still be kept since it's on a every second keep date
        [DataRow("2024-01-11", "2024-01-27", true)]   // 16 days after, should now, still be kept since it's on a every second keep date
        [DataRow("2024-01-11", "2024-02-08", true)]   // 28 days - should still be kept since it's on a every second keep date
        [DataRow("2024-01-11", "2024-02-09", false)]  // 29 days - should be deleted here since we move to the next section, which is every fourth date
        [DataRow("2024-01-11", "2024-02-15", false)]  // still deleted
        [DataRow("2024-01-11", "2026-07-24", false)]  // still deleted
        [DataRow("2024-01-12", "2024-01-28", false)]  // 16 days - test next date, should be deleted since this is not a keep date in every second date
        [DataRow("2024-01-13", "2024-01-28", true)]   // 15 days - test next date, should be keept since this is a keep date in every second date
        [DataRow("2024-01-13", "2024-01-29", true)]   // 16 days, should be keept since this is a keep date in every second date
        [DataRow("2024-01-14", "2024-01-29", false)]  // next date 15 days - delete on every second
        [DataRow("2024-01-14", "2024-01-30", false)]  // 16 days - delete on every second
        [DataRow("2024-01-14", "2024-01-27", true)]   // 14 days - keep since it's within 14 days of start
        [DataRow("2024-01-14", "2024-01-18", true)]   // 4 days - keep since it's within 14 days of start
        [DataRow("2024-01-14", "2024-02-23", false)]  // 40 days - delete since it's not every fourth date
        [DataRow("2024-01-15", "2024-02-24", false)]  // 40 days - delete since it's not every fourth date
        [DataRow("2024-01-15", "2024-02-27", false)]  // delete since it's not every fourth date
        [DataRow("2024-01-16", "2024-02-25", false)]  // 40 days delete, since it's not every fourth date
        [DataRow("2024-01-17", "2024-02-26", true)]   // 40 days keep, it's a fourth date
        [DataRow("2024-01-17", "2024-01-25", true)]   // keep, it's a fourth date and this is even lower than fourth date section, just making sure
        [DataRow("2024-01-17", "2024-03-15", true)]   // keep, it's a fourth date
        [DataRow("2024-01-17", "2024-04-10", true)]   // moving to 8 day section, but still keep since it's 16 days from reference which is divisible by 8
        [DataRow("2024-01-13", "2024-02-22", true)]   // 40 days with 12 days from reference, should be keept in every 4 section
        [DataRow("2024-01-13", "2024-03-15", true)]   // again keep since it's a fourth date
        [DataRow("2024-01-13", "2024-04-10", false)]  // now 8 day section, should not be kept
        [DataRow("2024-01-13", "2026-04-10", false)]  // deleted in the future
        [DataRow("2024-01-17", "2026-04-10", false)]  // 8 day compatible date also deleted in the future
        [DataRow("2024-02-02", "2026-04-10", true)]  // 32 day compatible date kept forever
        public void ShouldKeepFile_WithExamplePolicy_ReturnsExpectedResult(string evaluationDateString, string currentDateString, bool expectedResult)
        {
            // Arrange
            RetentionPolicy policy = CreateExampleRetentionPolicy();
            DateTime evaluationDate = DateTime.Parse(evaluationDateString);
            DateTime currentDate = DateTime.Parse(currentDateString);

            // Act
            bool actualResult = policy.ShouldKeepFile(evaluationDate, currentDate);

            // Assert
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [DataRow("2025-01-31", "2025-01-21", "2025-01-31", 20)]   // Simulate to last of january, check previous 10 days, expect 20 files (10 days * 2 files per day)
        [DataRow("2025-02-15", "2025-01-21", "2025-01-31", 10)]   // Simulate to Feb 15, check last 10 days of january, should have only 10 files since half of the days should have been deleted
        [DataRow("2025-11-01", "2025-01-01", "2025-01-31", 4)]    // Simulate almost to the end of the year, check first month, should have two dates with file left since that's every 16th (but 2 files in a day)
        [DataRow("2028-08-12", "2025-01-01", "2025-01-31", 2)]    // Simulate a looong time forward, should still be two files left in january
        [DataRow("2030-02-14", "2025-01-01", "2025-12-31", 22)]   // Simulate a loooooooong time forward, the year of 2025 should still have around a day of files per month left (every 32 days is kept)
        public void SimulateRetentionPolicy_WithExamplePolicy_ReturnsExpectedFileCounts(string simulationEndDateString, string checkStartDateString, string checkEndDateString, int expectedFileCount)
        {
            // Arrange
            RetentionPolicy policy = CreateExampleRetentionPolicy();
            DateTime simulationStartDate = new DateTime(2025, 1, 1);
            DateTime simulationEndDate = DateTime.Parse(simulationEndDateString);
            DateTime checkStartDate = DateTime.Parse(checkStartDateString);
            DateTime checkEndDate = DateTime.Parse(checkEndDateString);

            RetentionPolicyTimeSimulator simulator = new RetentionPolicyTimeSimulator(policy, simulationStartDate);

            // Act
            int daysToSimulate = (int)(simulationEndDate - simulationStartDate).TotalDays;
            simulator.SimulateForward(daysToSimulate);

            int actualFileCount = simulator.GetFileCountBetweenDates(checkStartDate, checkEndDate);

            // Assert
            Assert.AreEqual(expectedFileCount, actualFileCount);
        }

        /// <summary>
        /// Creates an example retention policy with 6 rules covering different time periods.
        /// </summary>
        /// <returns>A retention policy with example rules.</returns>
        private static RetentionPolicy CreateExampleRetentionPolicy()
        {
            List<RetentionRule> rules = new List<RetentionRule>
            {
                new RetentionRule("1.00:00:00", "14.00:00:00"),    // Daily for 14 days
                new RetentionRule("2.00:00:00", "28.00:00:00"),    // Every 2 days for 15–28 days ago
                new RetentionRule("4.00:00:00", "74.00:00:00"),    // Every 4 days for 29–74 days ago
                new RetentionRule("8.00:00:00", "194.00:00:00"),   // Every 8 days for 75–194 days ago
                new RetentionRule("16.00:00:00", "374.00:00:00"),  // Every 16 days for 195–374 days ago
                new RetentionRule("32.00:00:00", null)             // Every 32 days beyond 1 year
            };

            return new RetentionPolicy(rules);
        }
    }
}