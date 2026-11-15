using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgentTests.Configuration.Agent
{
    [TestClass]
    public class ByteShelfSettingsTests
    {
        private const string Url = "https://byteshelf.example.com";
        private const string Shelf = "backups";
        private const string ShelfKey = "bsk_secret-for-tests";

        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            ByteShelfSettings settings = new(Url, Shelf, ShelfKey);

            Assert.AreEqual(Url, settings.Url);
            Assert.AreEqual(Shelf, settings.Shelf);
            Assert.AreEqual(ShelfKey, settings.ShelfKey);
        }

        [TestMethod]
        [DataRow("https://byteshelf.example.com", "https://byteshelf.example.com/")]
        [DataRow("https://byteshelf.example.com/", "https://byteshelf.example.com/")]
        [DataRow("http://localhost:5000/files", "http://localhost:5000/files/")]
        public void ServerAddress_EndsInSlash_AsTheClientNeeds(string url, string expected)
        {
            ByteShelfSettings settings = new(url, Shelf, ShelfKey);

            Assert.AreEqual(expected, settings.ServerAddress.AbsoluteUri);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not a url")]
        [DataRow("ftp://byteshelf.example.com")]
        [DataRow("/relative/path")]
        public void Constructor_WithUrlThatIsNoHttpAddress_ThrowsArgumentException(string? url)
        {
            Assert.ThrowsException<ArgumentException>(() => new ByteShelfSettings(url!, Shelf, ShelfKey));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("Backups")]
        [DataRow("my backups")]
        [DataRow("-backups")]
        public void Constructor_WithInvalidShelfName_ThrowsArgumentException(string? shelf)
        {
            Assert.ThrowsException<ArgumentException>(() => new ByteShelfSettings(Url, shelf!, ShelfKey));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("an-api-key-from-byteshelf-1")]
        public void Constructor_WithKeyThatIsNoShelfKey_ThrowsArgumentException(string? shelfKey)
        {
            Assert.ThrowsException<ArgumentException>(() => new ByteShelfSettings(Url, Shelf, shelfKey!));
        }
    }
}
