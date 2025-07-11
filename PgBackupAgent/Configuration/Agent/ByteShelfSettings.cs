namespace PgBackupAgent.Configuration.Agent
{
    /// <summary>
    /// ByteShelf storage settings.
    /// </summary>
    public class ByteShelfSettings
    {
        /// <summary>
        /// ByteShelf API base URL.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// ByteShelf API key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteShelfSettings"/> class.
        /// </summary>
        /// <param name="baseUrl">ByteShelf API base URL.</param>
        /// <param name="apiKey">ByteShelf API key.</param>
        public ByteShelfSettings(string baseUrl, string apiKey)
        {
            if (baseUrl is null)
                throw new ArgumentNullException(nameof(baseUrl));
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentException(nameof(baseUrl));
            if (apiKey is null)
                throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException(nameof(apiKey));
            if (apiKey.Length < 16)
                throw new ArgumentException("API key must be at least 16 characters long", nameof(apiKey));

            BaseUrl = baseUrl;
            ApiKey = apiKey;
        }
    }
} 