using PgBackupAgent.Services.Backup;

namespace PgBackupAgentTests.Services.Backup
{
    /// <summary>
    /// Databases that dump to a few known bytes, dated by the test's clock; the ones named broken fail to dump.
    /// </summary>
    public sealed class FakeDatabaseBackupService : IDatabaseBackupService
    {
        private readonly TimeProvider _timeProvider;
        private readonly List<string> _databases;

        public FakeDatabaseBackupService(TimeProvider timeProvider, params string[] databases)
        {
            _timeProvider = timeProvider;
            _databases = databases.ToList();
        }

        /// <summary>The databases whose dump throws.</summary>
        public HashSet<string> Broken { get; } = new HashSet<string>();

        /// <summary>What a database's dump holds.</summary>
        public static byte[] DumpOf(string databaseName)
        {
            return System.Text.Encoding.UTF8.GetBytes($"dump of {databaseName}");
        }

        public Task<List<string>> GetDatabasesToBackupAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_databases.ToList());
        }

        public BackupFile CreateBackup(string databaseName, CancellationToken cancellationToken = default)
        {
            return new BackupFile(databaseName, new Stream(databaseName, Broken.Contains(databaseName)), _timeProvider.GetUtcNow().UtcDateTime, 0);
        }

        public async Task<IEnumerable<BackupFile>> CreateBackupsAsync(CancellationToken cancellationToken = default)
        {
            List<string> databases = await GetDatabasesToBackupAsync(cancellationToken);
            return databases.Select(database => CreateBackup(database, cancellationToken)).ToList();
        }

        private sealed class Stream : IBackupDataStream
        {
            private readonly string _databaseName;
            private readonly bool _broken;

            public Stream(string databaseName, bool broken)
            {
                _databaseName = databaseName;
                _broken = broken;
            }

            public Task<System.IO.Stream> CreateStreamAsync(CancellationToken cancellationToken = default)
            {
                if (_broken)
                {
                    throw new InvalidOperationException($"pg_dump of {_databaseName} failed");
                }

                return Task.FromResult<System.IO.Stream>(new MemoryStream(DumpOf(_databaseName)));
            }
        }
    }
}
