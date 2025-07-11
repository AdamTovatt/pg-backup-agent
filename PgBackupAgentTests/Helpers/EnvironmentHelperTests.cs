using PgBackupAgent.Helpers;

namespace PgBackupAgentTests.Helpers
{
    [TestClass]
    public class EnvironmentHelperTests
    {
        private const string TestVariableName = "TEST_ENV_VARIABLE";

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(TestVariableName, null);
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithValidVariable_ReturnsValue()
        {
            // Arrange
            string expectedValue = "test-value";
            Environment.SetEnvironmentVariable(TestVariableName, expectedValue);

            // Act
            string result = EnvironmentHelper.GetEnvironmentVariable(TestVariableName);

            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMissingVariable_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => EnvironmentHelper.GetEnvironmentVariable("NON_EXISTENT_VARIABLE"));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithEmptyVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestVariableName, "");

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => EnvironmentHelper.GetEnvironmentVariable(TestVariableName));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithWhitespaceVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestVariableName, "   ");

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => EnvironmentHelper.GetEnvironmentVariable(TestVariableName));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMinLength_ValidLength_ReturnsValue()
        {
            // Arrange
            string expectedValue = "test-value";
            Environment.SetEnvironmentVariable(TestVariableName, expectedValue);

            // Act
            string result = EnvironmentHelper.GetEnvironmentVariable(TestVariableName, 5);

            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMinLength_TooShort_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestVariableName, "short");

            // Act & Assert
            InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => EnvironmentHelper.GetEnvironmentVariable(TestVariableName, 10));
            Assert.IsTrue(exception.Message.Contains("minimum required length is 10"));
        }

        [TestMethod]
        public void GetEnvironmentVariable_WithMinLength_ExactLength_ReturnsValue()
        {
            // Arrange
            string expectedValue = "exact";
            Environment.SetEnvironmentVariable(TestVariableName, expectedValue);

            // Act
            string result = EnvironmentHelper.GetEnvironmentVariable(TestVariableName, 5);

            // Assert
            Assert.AreEqual(expectedValue, result);
        }
    }
} 