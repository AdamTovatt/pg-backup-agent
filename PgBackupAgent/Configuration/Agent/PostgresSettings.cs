namespace PgBackupAgent.Configuration.Agent
{
    /// <summary>
    /// PostgreSQL connection settings.
    /// </summary>
    public class PostgresSettings
    {
        /// <summary>
        /// PostgreSQL server hostname or IP address.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// PostgreSQL server port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// PostgreSQL username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// PostgreSQL password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// PostgreSQL database name.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresSettings"/> class.
        /// </summary>
        /// <param name="host">PostgreSQL server hostname or IP address.</param>
        /// <param name="port">PostgreSQL server port.</param>
        /// <param name="username">PostgreSQL username.</param>
        /// <param name="password">PostgreSQL password.</param>
        /// <param name="database">PostgreSQL database name.</param>
        public PostgresSettings(string host, int port, string username, string password, string database)
        {
            if (host is null)
                throw new ArgumentNullException(nameof(host));
            if (string.IsNullOrEmpty(host))
                throw new ArgumentException(nameof(host));
            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port));
            if (username is null)
                throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException(nameof(username));
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException(nameof(password));
            if (database is null)
                throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrEmpty(database))
                throw new ArgumentException(nameof(database));

            Host = host;
            Port = port;
            Username = username;
            Password = password;
            Database = database;
        }
    }
} 