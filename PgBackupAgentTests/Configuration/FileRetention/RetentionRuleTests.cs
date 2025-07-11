using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgentTests.Configuration.FileRetention
{
    [TestClass]
    public class RetentionRuleTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            string keepEvery = "1.00:00:00";
            string? duration = "14.00:00:00";

            // Act
            RetentionRule rule = new(keepEvery, duration);

            // Assert
            Assert.AreEqual(keepEvery, rule.KeepEvery);
            Assert.AreEqual(duration, rule.Duration);
        }

        [TestMethod]
        public void Constructor_WithNullDuration_SetsPropertiesCorrectly()
        {
            // Arrange
            string keepEvery = "7.00:00:00";
            string? duration = null;

            // Act
            RetentionRule rule = new(keepEvery, duration);

            // Assert
            Assert.AreEqual(keepEvery, rule.KeepEvery);
            Assert.IsNull(rule.Duration);
        }

        [TestMethod]
        public void Constructor_WithNullKeepEvery_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new RetentionRule(null!, "14.00:00:00"));
        }

        [TestMethod]
        public void Constructor_WithEmptyKeepEvery_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => new RetentionRule("", "14.00:00:00"));
        }

        [TestMethod]
        public void KeepEveryTimeSpan_WithValidFormat_ReturnsCorrectTimeSpan()
        {
            // Arrange
            RetentionRule rule = new("1.00:00:00", "14.00:00:00");

            // Act
            TimeSpan result = rule.KeepEveryTimeSpan;

            // Assert
            Assert.AreEqual(TimeSpan.FromDays(1), result);
        }

        [TestMethod]
        public void KeepEveryTimeSpan_WithInvalidFormat_ThrowsFormatException()
        {
            // Arrange
            RetentionRule rule = new("invalid-format", "14.00:00:00");

            // Act & Assert
            Assert.ThrowsException<FormatException>(() => _ = rule.KeepEveryTimeSpan);
        }

        [TestMethod]
        public void DurationTimeSpan_WithValidFormat_ReturnsCorrectTimeSpan()
        {
            // Arrange
            RetentionRule rule = new("1.00:00:00", "14.00:00:00");

            // Act
            TimeSpan? result = rule.DurationTimeSpan;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TimeSpan.FromDays(14), result);
        }

        [TestMethod]
        public void DurationTimeSpan_WithNullDuration_ReturnsNull()
        {
            // Arrange
            RetentionRule rule = new("1.00:00:00", null);

            // Act
            TimeSpan? result = rule.DurationTimeSpan;

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void DurationTimeSpan_WithInvalidFormat_ThrowsFormatException()
        {
            // Arrange
            RetentionRule rule = new("1.00:00:00", "invalid-format");

            // Act & Assert
            Assert.ThrowsException<FormatException>(() => _ = rule.DurationTimeSpan);
        }
    }
} 