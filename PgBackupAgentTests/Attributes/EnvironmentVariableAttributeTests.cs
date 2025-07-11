using PgBackupAgent.Attributes;

namespace PgBackupAgentTests.Attributes
{
    [TestClass]
    public class EnvironmentVariableAttributeTests
    {
        [TestMethod]
        public void EnvironmentVariableNameAttribute_WithDefaultConstructor_SetsDefaultValues()
        {
            // Act
            EnvironmentVariableNameAttribute attribute = new();

            // Assert
            Assert.AreEqual(0, attribute.MinLength);
        }

        [TestMethod]
        public void EnvironmentVariableNameAttribute_WithMinLength_SetsMinLength()
        {
            // Arrange
            int expectedMinLength = 16;

            // Act
            EnvironmentVariableNameAttribute attribute = new(expectedMinLength);

            // Assert
            Assert.AreEqual(expectedMinLength, attribute.MinLength);
        }

        [TestMethod]
        public void EnvironmentVariableNameContainerAttribute_CanBeInstantiated()
        {
            // Act
            EnvironmentVariableNameContainerAttribute attribute = new();

            // Assert
            Assert.IsNotNull(attribute);
        }
    }
} 