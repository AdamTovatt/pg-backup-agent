using ByteShelfCommon;

namespace PgBackupAgent.Configuration.Agent
{
    /// <summary>
    /// Where the backups are stored: a shelf on a ByteShelf server, reached with a key for that shelf.
    /// </summary>
    public class ByteShelfSettings
    {
        /// <summary>
        /// The ByteShelf server's address, for example <c>https://files.example.com</c>.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// The name of the shelf the backups are stored on.
        /// </summary>
        public string Shelf { get; }

        /// <summary>
        /// The secret of a read-write key for <see cref="Shelf"/>, starting <c>bsk_</c>. Read-write, since
        /// pruning deletes files.
        /// </summary>
        public string ShelfKey { get; }

        /// <summary>
        /// The server's address as the client needs it: absolute, ending in <c>/</c>.
        /// </summary>
        public Uri ServerAddress { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteShelfSettings"/> class.
        /// </summary>
        /// <param name="url">The ByteShelf server's address, http or https.</param>
        /// <param name="shelf">The name of the shelf the backups are stored on.</param>
        /// <param name="shelfKey">The secret of a read-write key for the shelf.</param>
        /// <exception cref="ArgumentException">A value is missing or not allowed.</exception>
        public ByteShelfSettings(string url, string shelf, string shelfKey)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("ByteShelf.Url is required.", nameof(url));
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? address) || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException($"ByteShelf.Url '{url}' is not an http or https address.", nameof(url));
            if (!ShelfName.TryValidate(shelf, out string? shelfError))
                throw new ArgumentException($"ByteShelf.Shelf '{shelf}' is not a shelf name: {shelfError}", nameof(shelf));
            if (string.IsNullOrWhiteSpace(shelfKey))
                throw new ArgumentException("ByteShelf.ShelfKey is required.", nameof(shelfKey));
            if (!shelfKey.StartsWith(ShelfKeys.SecretPrefix, StringComparison.Ordinal))
                throw new ArgumentException($"ByteShelf.ShelfKey must be a shelf key's secret, which starts '{ShelfKeys.SecretPrefix}'.", nameof(shelfKey));

            Url = url;
            Shelf = shelf;
            ShelfKey = shelfKey;
            ServerAddress = address.AbsolutePath.EndsWith('/') ? address : new Uri(address.AbsoluteUri + "/");
        }
    }
}
