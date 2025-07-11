using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgentTests.Configuration.FileRetention
{
    [TestClass]
    public class RetentionPolicyTimeSimulationTests
    {
        [TestMethod]
        public void SimulateTimePassing_WithDailyAndWeeklyRules_RetainsCorrectFiles()
        {
            // Arrange - Policy: Keep daily for 7 days, weekly for 30 days
            List<RetentionRule> rules = new()
            {
                new RetentionRule("1.00:00:00", "7.00:00:00"),   // Daily for 7 days
                new RetentionRule("7.00:00:00", "30.00:00:00")    // Weekly for 30 days
            };
            RetentionPolicy policy = new(rules);

            DateTime februaryStart = new DateTime(2024, 2, 1, 12, 0, 0);
            DateTime februaryEnd = new DateTime(2024, 2, 29, 12, 0, 0);

            // Assert: Verify the retention behavior over time
            // Day 0 (Feb 1st): Should have all 29 files (all within 30 days)
            Assert.AreEqual(29, CountFilesKept(policy, februaryStart, februaryEnd, new DateTime(2024, 2, 1, 12, 0, 0)), "Day 0 should keep all files");

            // Day 7 (Feb 8th): Should have all 29 files (all within 30-day duration)
            Assert.AreEqual(29, CountFilesKept(policy, februaryStart, februaryEnd, new DateTime(2024, 2, 8, 12, 0, 0)), "Day 7 should keep all files (within 30-day duration)");

            // Day 30 (Mar 2nd): Should have 29 files (all within 30-day duration)
            DateTime day30Date = new DateTime(2024, 3, 2, 12, 0, 0);
            Console.WriteLine($"Testing Day 30 retention on {day30Date:yyyy-MM-dd}");
            Console.WriteLine($"Files from {februaryStart:yyyy-MM-dd} to {februaryEnd:yyyy-MM-dd}");

            // Debug: Check specific files to understand the issue
            for (DateTime date = februaryStart; date <= februaryStart.AddDays(5); date = date.AddDays(1))
            {
                bool kept = policy.ShouldKeepFile(date, day30Date);
                Console.WriteLine($"  File {date:yyyy-MM-dd}: {(kept ? "KEPT" : "DELETED")}");
            }

            int actualCount = CountFilesKept(policy, februaryStart, februaryEnd, day30Date);
            Console.WriteLine($"  CountFilesKept returned: {actualCount}");

            Assert.AreEqual(29, actualCount, "Day 30 should keep all files (within 30-day duration)");

            // Day 45 (Mar 17th): Should have 0 files (all outside 30-day duration)
            DateTime day45Date = new DateTime(2024, 4, 15, 12, 0, 0); // More than 30 days after February files
            Console.WriteLine($"Testing Day 45 retention on {day45Date:yyyy-MM-dd}");
            Console.WriteLine($"Files from {februaryStart:yyyy-MM-dd} to {februaryEnd:yyyy-MM-dd}");

            // Debug: Check specific files to understand the issue
            for (DateTime date = februaryStart; date <= februaryStart.AddDays(5); date = date.AddDays(1))
            {
                bool kept = policy.ShouldKeepFile(date, day45Date);
                Console.WriteLine($"  File {date:yyyy-MM-dd}: {(kept ? "KEPT" : "DELETED")}");
            }

            actualCount = CountFilesKept(policy, februaryStart, februaryEnd, day45Date);
            Console.WriteLine($"  CountFilesKept returned: {actualCount}");

            Assert.AreEqual(0, actualCount, "Day 45 should keep 0 files");
        }

        [TestMethod]
        public void SimulateTimePassing_WithComplexPolicy_RetainsCorrectFiles()
        {
            // Arrange - Complex policy: daily for 7 days, weekly for 30 days, monthly for 1 year
            List<RetentionRule> rules = new()
            {
                new RetentionRule("1.00:00:00", "7.00:00:00"),   // Daily for 7 days
                new RetentionRule("7.00:00:00", "30.00:00:00"),   // Weekly for 30 days
                new RetentionRule("30.00:00:00", "365.00:00:00")  // Monthly for 1 year
            };
            RetentionPolicy policy = new(rules);

            DateTime januaryStart = new DateTime(2024, 1, 1);
            DateTime januaryEnd = new DateTime(2024, 1, 31);

            // Assert: Verify the retention behavior over time
            // Week 0 (Jan 1st): Should have all 31 files
            Assert.AreEqual(31, CountFilesKept(policy, januaryStart, januaryEnd, new DateTime(2024, 1, 1, 12, 0, 0)), "Week 0 should keep all files");

            // Week 1 (Jan 8th): Should have all 31 files (all within 30-day duration)
            Assert.AreEqual(31, CountFilesKept(policy, januaryStart, januaryEnd, new DateTime(2024, 1, 8, 12, 0, 0)), "Week 1 should keep all files (within 30-day duration)");

            // Week 4 (Jan 29th): Should have 31 files (all within 30-day duration)
            Assert.AreEqual(31, CountFilesKept(policy, januaryStart, januaryEnd, new DateTime(2024, 1, 29, 12, 0, 0)), "Week 4 should keep 31 files (within 30-day duration)");

            // Week 52 (Dec 30th): Should have 31 files (all within 365-day duration)
            Assert.AreEqual(31, CountFilesKept(policy, januaryStart, januaryEnd, new DateTime(2024, 12, 30, 12, 0, 0)), "Week 52 should keep 31 files (within 365-day duration)");

            // Test monthly retention with files outside the 365-day duration
            // This would be in 2025, where January 2024 files are outside the 365-day duration
            DateTime testDate = new DateTime(2025, 2, 15, 12, 0, 0); // More than 365 days after January 2024
            Console.WriteLine($"Testing retention on {testDate:yyyy-MM-dd}");
            Console.WriteLine($"Files from {januaryStart:yyyy-MM-dd} to {januaryEnd:yyyy-MM-dd}");

            // Debug: Check a few specific files
            for (DateTime date = januaryStart; date <= januaryStart.AddDays(5); date = date.AddDays(1))
            {
                bool kept = policy.ShouldKeepFile(date, testDate);
                Console.WriteLine($"  File {date:yyyy-MM-dd}: {(kept ? "KEPT" : "DELETED")}");
            }

            // Debug: Check what CountFilesKept actually returns
            int actualCount = CountFilesKept(policy, januaryStart, januaryEnd, testDate);
            Console.WriteLine($"  CountFilesKept returned: {actualCount}");

            Assert.AreEqual(0, CountFilesKept(policy, januaryStart, januaryEnd, testDate), "Files should be deleted after 365 days");
        }

        [TestMethod]
        public void SimulateTimePassing_WithNoDuration_KeepsFilesIndefinitely()
        {
            // Arrange - Policy: Keep weekly indefinitely
            List<RetentionRule> rules = new()
            {
                new RetentionRule("7.00:00:00", null) // Weekly indefinitely
            };
            RetentionPolicy policy = new(rules);

            // Create weekly files (one per week) instead of daily files
            List<DateTime> weeklyFiles = new();
            DateTime weekStart = new DateTime(2024, 1, 1, 12, 0, 0); // Start of first week
            for (int week = 0; week < 52; week++)
            {
                weeklyFiles.Add(weekStart.AddDays(week * 7));
            }

            // Assert: Verify that files are kept indefinitely
            // Month 0 (Jan 1st): Should have all 52 weekly files
            int keptCount = weeklyFiles.Count(fileDate => policy.ShouldKeepFile(fileDate, new DateTime(2024, 1, 1, 12, 0, 0)));
            Assert.AreEqual(52, keptCount, "Month 0 should keep all 52 weekly files");

            // Month 11 (Dec 1st): Should still have all 52 weekly files (no duration limit)
            keptCount = weeklyFiles.Count(fileDate => policy.ShouldKeepFile(fileDate, new DateTime(2024, 12, 1, 12, 0, 0)));
            Assert.AreEqual(52, keptCount, "Month 11 should keep all 52 weekly files");
        }

        /// <summary>
        /// Counts how many files would be kept for a given date range at a specific current time.
        /// </summary>
        /// <param name="policy">The retention policy to apply.</param>
        /// <param name="startDate">Start of the date range to check.</param>
        /// <param name="endDate">End of the date range to check.</param>
        /// <param name="currentTime">The current time to evaluate retention against.</param>
        /// <returns>Number of files that would be kept.</returns>
        private static int CountFilesKept(RetentionPolicy policy, DateTime startDate, DateTime endDate, DateTime currentTime)
        {
            int count = 0;
            DateTime date = startDate;
            while (date <= endDate)
            {
                // Use the same time component as the current time for consistency
                DateTime fileDateWithTime = new DateTime(date.Year, date.Month, date.Day, currentTime.Hour, currentTime.Minute, currentTime.Second);
                if (policy.ShouldKeepFile(fileDateWithTime, currentTime))
                {
                    count++;
                    Console.WriteLine($"  COUNTING: {date:yyyy-MM-dd}");
                }
                date = date.AddDays(1);
            }
            return count;
        }
    }
}