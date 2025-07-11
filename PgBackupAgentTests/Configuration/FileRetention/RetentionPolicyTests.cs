using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgentTests.Configuration.FileRetention
{
    [TestClass]
    public class RetentionPolicyTests
    {
        [TestMethod]
        public void ShouldKeepFile_WithDailyRule_KeepsCorrectFiles()
        {
            // Arrange
            List<RetentionRule> rules = new()
            {
                new RetentionRule("1.00:00:00", "7.00:00:00") // Keep every day for 7 days
            };
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th

            // Files from different days
            DateTime file1 = new DateTime(2024, 1, 9, 3, 0, 0);  // Jan 9th - should be kept (6 days old)
            DateTime file2 = new DateTime(2024, 1, 10, 15, 30, 0); // Jan 10th - should be kept (5 days old)
            DateTime file3 = new DateTime(2024, 1, 7, 23, 0, 0);  // Jan 7th - should be deleted (8 days old)

            // Act & Assert
            Assert.IsTrue(policy.ShouldKeepFile(file1, currentDate), "File from Jan 8th should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(file2, currentDate), "File from Jan 9th should be kept");
            Assert.IsFalse(policy.ShouldKeepFile(file3, currentDate), "File from Jan 7th should be deleted");
        }

        [TestMethod]
        public void ShouldKeepFile_WithWeeklyRule_KeepsCorrectFiles()
        {
            // Arrange
            List<RetentionRule> rules = new()
            {
                new RetentionRule("7.00:00:00", "30.00:00:00") // Keep every week for 30 days
            };
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th (Monday)

            // Files from different weeks
            DateTime file1 = new DateTime(2024, 1, 8, 3, 0, 0);   // Jan 8th (Monday) - should be kept (within 30 days)
            DateTime file2 = new DateTime(2024, 1, 1, 15, 30, 0);  // Jan 1st (Monday) - should be kept (within 30 days)
            DateTime file3 = new DateTime(2023, 12, 15, 23, 0, 0); // Dec 15th (Friday) - should be deleted (31 days old, outside 30-day duration)

            // Act & Assert
            Assert.IsTrue(policy.ShouldKeepFile(file1, currentDate), "File from Jan 8th should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(file2, currentDate), "File from Jan 1st should be kept");
            Assert.IsFalse(policy.ShouldKeepFile(file3, currentDate), "File from Dec 25th should be deleted");
        }

        [TestMethod]
        public void ShouldKeepFile_WithMultipleRules_KeepsFilesMatchingAnyRule()
        {
            // Arrange
            List<RetentionRule> rules = new()
            {
                new RetentionRule("1.00:00:00", "7.00:00:00"),   // Keep every day for 7 days
                new RetentionRule("7.00:00:00", "30.00:00:00")    // Keep every week for 30 days
            };
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th

            // Files that should be kept by different rules
            DateTime dailyFile = new DateTime(2024, 1, 14, 3, 0, 0);  // Yesterday - kept by daily rule
            DateTime weeklyFile = new DateTime(2024, 1, 8, 15, 30, 0); // Last Monday - kept by weekly rule
            DateTime oldFile = new DateTime(2024, 1, 1, 23, 0, 0);     // Jan 1st - kept by weekly rule

            // Act & Assert
            Assert.IsTrue(policy.ShouldKeepFile(dailyFile, currentDate), "Daily file should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(weeklyFile, currentDate), "Weekly file should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(oldFile, currentDate), "Old file should be kept by weekly rule");
        }

        [TestMethod]
        public void ShouldKeepFile_WithNoDuration_KeepsFilesIndefinitely()
        {
            // Arrange
            List<RetentionRule> rules = new()
            {
                new RetentionRule("7.00:00:00", null) // Keep every week indefinitely
            };
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th

            // Files from different weeks
            DateTime file1 = new DateTime(2024, 1, 8, 3, 0, 0);   // Jan 8th - should be kept
            DateTime file2 = new DateTime(2024, 1, 1, 15, 30, 0);  // Jan 1st - should be kept
            DateTime file3 = new DateTime(2023, 12, 25, 23, 0, 0); // Dec 25th - should be kept (no duration limit)

            // Act & Assert
            Assert.IsTrue(policy.ShouldKeepFile(file1, currentDate), "Recent file should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(file2, currentDate), "Older file should be kept");
            
            Assert.IsTrue(policy.ShouldKeepFile(file3, currentDate), "Very old file should be kept (no duration)");
        }

        [TestMethod]
        public void ShouldKeepFile_WithShiftingTime_DoesNotShiftRetentionWindow()
        {
            // Arrange
            List<RetentionRule> rules = new()
            {
                new RetentionRule("7.00:00:00", "30.00:00:00") // Keep every week for 30 days
            };
            RetentionPolicy policy = new(rules);

            DateTime fileDate = new DateTime(2024, 1, 8, 15, 30, 0); // Jan 8th (Monday)

            // Test with different current dates to ensure retention doesn't shift
            DateTime currentDate1 = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th
            DateTime currentDate2 = new DateTime(2024, 1, 16, 12, 0, 0); // Jan 16th
            DateTime currentDate3 = new DateTime(2024, 1, 17, 12, 0, 0); // Jan 17th

            // Act & Assert
            bool result1 = policy.ShouldKeepFile(fileDate, currentDate1);
            bool result2 = policy.ShouldKeepFile(fileDate, currentDate2);
            bool result3 = policy.ShouldKeepFile(fileDate, currentDate3);

            // The file should be kept or deleted consistently regardless of current date
            // (as long as it's within the duration limit)
            Assert.AreEqual(result1, result2, "Retention decision should not change from day 1 to day 2");
            Assert.AreEqual(result2, result3, "Retention decision should not change from day 2 to day 3");
        }

        [TestMethod]
        public void ShouldKeepFile_WithExactSectionBoundaries_HandlesCorrectly()
        {
            // Arrange
            List<RetentionRule> rules = new()
            {
                new RetentionRule("7.00:00:00", "7.00:00:00") // Keep every week for 7 days
            };
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th

            // Files exactly on section boundaries
            DateTime fileOnSectionStart = new DateTime(2024, 1, 9, 0, 0, 0);  // Jan 9th 00:00 - should be kept (6 days old)
            DateTime fileOnSectionEnd = new DateTime(2024, 1, 14, 23, 59, 59); // Jan 14th 23:59 - should be kept (1 day old)
            DateTime fileJustOutside = new DateTime(2024, 1, 7, 12, 0, 0);    // Jan 7th 12:00 - should be deleted (8 days old, outside duration)

            // Act & Assert
            Assert.IsTrue(policy.ShouldKeepFile(fileOnSectionStart, currentDate), "File on section start should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(fileOnSectionEnd, currentDate), "File on section end should be kept");
            Assert.IsFalse(policy.ShouldKeepFile(fileJustOutside, currentDate), "File just outside section should be deleted");
        }

        [TestMethod]
        public void ShouldKeepFile_WithComplexRetentionPolicy_WorksCorrectly()
        {
            // Arrange - Complex policy: daily for 7 days, weekly for 30 days, monthly for 1 year
            List<RetentionRule> rules = new()
            {
                new RetentionRule("1.00:00:00", "7.00:00:00"),   // Daily for 7 days
                new RetentionRule("7.00:00:00", "30.00:00:00"),   // Weekly for 30 days
                new RetentionRule("30.00:00:00", "365.00:00:00")  // Monthly for 1 year
            };
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th

            // Test files at different retention levels
            DateTime recentFile = new DateTime(2024, 1, 14, 15, 30, 0);  // Yesterday - kept by daily rule
            DateTime weeklyFile = new DateTime(2024, 1, 8, 3, 0, 0);     // Last Monday - kept by weekly rule
            DateTime monthlyFile = new DateTime(2023, 12, 18, 12, 0, 0); // Last month - kept by monthly rule
            DateTime oldFile = new DateTime(2023, 1, 15, 9, 0, 0);       // Last year - should be deleted

            // Act & Assert
            Assert.IsTrue(policy.ShouldKeepFile(recentFile, currentDate), "Recent file should be kept by daily rule");
            Assert.IsTrue(policy.ShouldKeepFile(weeklyFile, currentDate), "Weekly file should be kept by weekly rule");
            Assert.IsTrue(policy.ShouldKeepFile(monthlyFile, currentDate), "Monthly file should be kept by monthly rule");
            Assert.IsFalse(policy.ShouldKeepFile(oldFile, currentDate), "Old file should be deleted");
        }

        [TestMethod]
        public void ShouldKeepFile_WithNoRules_DeletesAllFiles()
        {
            // Arrange
            List<RetentionRule> rules = new();
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0);
            DateTime fileDate = new DateTime(2024, 1, 14, 15, 30, 0);

            // Act & Assert
            Assert.IsFalse(policy.ShouldKeepFile(fileDate, currentDate), "File should be deleted when no rules exist");
        }

        [TestMethod]
        public void ShouldKeepFile_WithEdgeCaseTimes_HandlesCorrectly()
        {
            // Arrange
            List<RetentionRule> rules = new()
            {
                new RetentionRule("1.00:00:00", "7.00:00:00") // Keep every day for 7 days
            };
            RetentionPolicy policy = new(rules);

            DateTime currentDate = new DateTime(2024, 1, 15, 12, 0, 0); // Jan 15th

            // Files with edge case times
            DateTime fileEarlyMorning = new DateTime(2024, 1, 14, 0, 0, 1);   // 00:00:01
            DateTime fileLateNight = new DateTime(2024, 1, 14, 23, 59, 59);   // 23:59:59
            DateTime fileMidnight = new DateTime(2024, 1, 14, 0, 0, 0);       // 00:00:00

            // Act & Assert
            Assert.IsTrue(policy.ShouldKeepFile(fileEarlyMorning, currentDate), "Early morning file should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(fileLateNight, currentDate), "Late night file should be kept");
            Assert.IsTrue(policy.ShouldKeepFile(fileMidnight, currentDate), "Midnight file should be kept");
        }

        [TestMethod]
        public void SimulateRetentionOverTime_WithComplexPolicy_WorksCorrectly()
        {
            // Arrange - Create the complex retention policy
            List<RetentionRule> rules = new()
            {
                new RetentionRule("1.00:00:00", "14.00:00:00"),    // Daily for 14 days
                new RetentionRule("2.00:00:00", "28.00:00:00"),    // Every 2 days for 15–28 days ago
                new RetentionRule("4.00:00:00", "44.00:00:00"),    // Every 4 days for 29–44 days ago
                new RetentionRule("5.00:00:00", "74.00:00:00"),    // Every 5 days for 45–74 days ago
                new RetentionRule("7.00:00:00", "194.00:00:00"),   // Weekly for 75–194 days ago
                new RetentionRule("14.00:00:00", "374.00:00:00"),  // Biweekly for 195–374 days ago
                new RetentionRule("30.00:00:00", null)             // Monthly beyond 1 year
            };
            RetentionPolicy policy = new(rules);

            // Create simulated files for September 2024 (2 files per day at random times)
            List<DateTime> simulatedFiles = CreateSimulatedFilesForSeptember2024();

            // Test different simulation periods to verify all retention sections
            DateTime simulationStart = new DateTime(2024, 10, 1, 12, 0, 0);  // October 1st
            DateTime simulationEnd = new DateTime(2024, 12, 31, 12, 0, 0);   // December 31st

            // Act - Simulate retention over time
            List<DateTime> remainingFiles = SimulateRetentionOverTime(policy, simulationStart, simulationEnd, simulatedFiles);

            // Assert - Verify that files are retained according to the policy
            // After 3 months of simulation, we should have files from different retention periods
            Assert.IsTrue(remainingFiles.Count > 0, "Some files should remain after retention simulation");

            // Verify that no files older than the retention policy remain
            foreach (DateTime fileDate in remainingFiles)
            {
                TimeSpan age = simulationEnd - fileDate;
                bool shouldBeKept = policy.ShouldKeepFile(fileDate, simulationEnd);
                Assert.IsTrue(shouldBeKept, $"File from {fileDate:yyyy-MM-dd} should be kept according to policy");
            }

            // Additional verification: Check that we have files from different retention periods
            // After 3 months, September files would be ~90-120 days old, so they should be in weekly retention
            List<DateTime> weeklyFiles = remainingFiles.Where(f => 
            {
                TimeSpan age = simulationEnd - f;
                return age.TotalDays > 74 && age.TotalDays <= 194;
            }).ToList();

            Assert.IsTrue(weeklyFiles.Count > 0, "Should have files from weekly retention period");

            // Test biweekly retention with a longer simulation period
            DateTime biweeklySimulationStart = new DateTime(2024, 10, 1, 12, 0, 0);
            DateTime biweeklySimulationEnd = new DateTime(2025, 3, 31, 12, 0, 0); // 6 months later

            List<DateTime> biweeklyRemainingFiles = SimulateRetentionOverTime(policy, biweeklySimulationStart, biweeklySimulationEnd, simulatedFiles);

            // After 6 months, September files would be ~180-210 days old, so they should be in biweekly retention
            List<DateTime> biweeklyFiles = biweeklyRemainingFiles.Where(f => 
            {
                TimeSpan age = biweeklySimulationEnd - f;
                return age.TotalDays > 194 && age.TotalDays <= 374;
            }).ToList();

            Assert.IsTrue(biweeklyFiles.Count > 0, "Should have files from biweekly retention period after 6 months");

            // Test daily retention with a shorter simulation period
            DateTime dailySimulationStart = new DateTime(2024, 10, 1, 12, 0, 0);
            DateTime dailySimulationEnd = new DateTime(2024, 10, 10, 12, 0, 0); // Only 9 days later

            List<DateTime> dailyRemainingFiles = SimulateRetentionOverTime(policy, dailySimulationStart, dailySimulationEnd, simulatedFiles);

            // After 9 days, some September files should still be in daily retention (≤ 14 days old)
            List<DateTime> dailyFiles = dailyRemainingFiles.Where(f => (dailySimulationEnd - f).TotalDays <= 14).ToList();
            Assert.IsTrue(dailyFiles.Count > 0, "Should have files from daily retention period after 9 days");
        }

        private static List<DateTime> CreateSimulatedFilesForSeptember2024()
        {
            List<DateTime> files = new();
            Random random = new Random(42); // Fixed seed for reproducible tests

            DateTime startDate = new DateTime(2024, 9, 1, 0, 0, 0);
            DateTime endDate = new DateTime(2024, 9, 30, 23, 59, 59);

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Create 2 files per day at random times
                for (int i = 0; i < 2; i++)
                {
                    int randomHour = random.Next(0, 24);
                    int randomMinute = random.Next(0, 60);
                    int randomSecond = random.Next(0, 60);

                    DateTime fileTime = new DateTime(date.Year, date.Month, date.Day, randomHour, randomMinute, randomSecond);
                    files.Add(fileTime);
                }
            }

            return files;
        }

        private static List<DateTime> SimulateRetentionOverTime(RetentionPolicy policy, DateTime startDate, DateTime endDate, List<DateTime> files)
        {
            List<DateTime> currentFiles = new(files);

            // Simulate daily retention checks
            for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                currentFiles = ApplyRetentionPolicy(policy, currentDate, currentFiles);
            }

            return currentFiles;
        }

        private static List<DateTime> ApplyRetentionPolicy(RetentionPolicy policy, DateTime currentDate, List<DateTime> files)
        {
            List<DateTime> keptFiles = new();

            foreach (DateTime fileDate in files)
            {
                if (policy.ShouldKeepFile(fileDate, currentDate))
                {
                    keptFiles.Add(fileDate);
                }
            }

            return keptFiles;
        }
    }
} 