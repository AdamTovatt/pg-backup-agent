using System.Reflection;
using ByteShelfClient;
using ByteShelfClient.InMemory;
using ByteShelfCommon;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;
using PgBackupAgent.Services.Backup;

namespace PgBackupAgentTests.Services.Backup
{
    [TestClass]
    public class BackupOrchestratorTests
    {
        private const string ShelfName = "backups";

        /// <summary>A run at 01:00 UTC, as the recommended timer starts it.</summary>
        private static readonly DateTimeOffset FirstRun = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);

        private FakeTimeProvider _time = null!;
        private InMemoryByteShelfClient _byteShelf = null!;
        private IShelfClient _shelf = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _time = new FakeTimeProvider(FirstRun);
            _byteShelf = new InMemoryByteShelfClient(timeProvider: _time);
            await _byteShelf.CreateShelfAsync(ShelfName);
            _shelf = _byteShelf.Shelf(ShelfName);
        }

        [TestMethod]
        public async Task PerformBackupAsync_StoresEachDatabaseAsTarUnderSourceAndDate()
        {
            FakeDatabaseBackupService databases = new FakeDatabaseBackupService(_time, "shop", "wiki");
            _time.SetUtcNow(new DateTimeOffset(2026, 3, 7, 1, 2, 3, TimeSpan.Zero));

            BackupReport report = await Orchestrator("alpha", databases).PerformBackupAsync();

            Assert.IsTrue(report.Succeeded);
            Assert.AreEqual(PruningOutcome.Pruned, report.Pruning);
            CollectionAssert.AreEqual(
                new[] { "/alpha/2026/03/07/shop_2026-03-07_01-02-03.tar", "/alpha/2026/03/07/wiki_2026-03-07_01-02-03.tar" },
                report.StoredPaths.ToArray());

            ShelfFile stored = await _shelf.GetFileAsync("/alpha/2026/03/07/shop_2026-03-07_01-02-03.tar");
            Assert.AreEqual("application/x-tar", stored.ContentType);
            await using Stream content = await _shelf.OpenReadAsync(stored.Id);
            using MemoryStream read = new MemoryStream();
            await content.CopyToAsync(read);
            CollectionAssert.AreEqual(FakeDatabaseBackupService.DumpOf("shop"), read.ToArray());
        }

        [TestMethod]
        public async Task PerformBackupAsync_DailyForOverAYear_KeepsWhatThePolicyKeepsAndNoEmptyDirectories()
        {
            RetentionPolicy policy = ExamplePolicy();
            FakeDatabaseBackupService databases = new FakeDatabaseBackupService(_time, "shop", "wiki");
            BackupOrchestrator orchestrator = Orchestrator("alpha", databases, policy);

            // What the policy keeps of the same runs, applied after each run as the agent applies it.
            List<(string Path, DateTime CreatedAt)> expected = new List<(string, DateTime)>();
            const int Days = 420;

            for (int day = 0; day < Days; day++)
            {
                BackupReport report = await orchestrator.PerformBackupAsync();
                Assert.IsTrue(report.Succeeded, $"Run on day {day} failed");

                DateTime now = _time.GetUtcNow().UtcDateTime;
                expected.AddRange(report.StoredPaths.Select(path => (path, now)));
                expected.RemoveAll(file => !policy.ShouldKeepFile(file.CreatedAt, now));

                _time.Advance(TimeSpan.FromDays(1));
            }

            DirectoryTree tree = await WholeTreeAsync("/alpha");
            CollectionAssert.AreEquivalent(expected.Select(file => file.Path).ToList(), tree.Files.Select(file => file.Path).ToList());

            // Not vacuous: pruning removed most of the dumps, and with them whole day and month folders.
            Assert.IsTrue(tree.Files.Count < Days, $"{tree.Files.Count} files kept of {Days * 2}");
            Assert.IsTrue(tree.Directories.Count(directory => directory.Path.Split('/').Length == 5) < Days);

            foreach (ShelfDirectory directory in tree.Directories)
            {
                Assert.IsTrue(tree.Files.Any(file => file.Path.StartsWith(directory.Path + "/", StringComparison.Ordinal)), $"{directory.Path} was left empty");
            }
        }

        [TestMethod]
        public async Task PerformBackupAsync_PrunesOnlyItsOwnSourceFolder()
        {
            // Another server's agent stored the same database names under its own source a while ago.
            BackupReport other = await Orchestrator("beta", new FakeDatabaseBackupService(_time, "shop", "wiki")).PerformBackupAsync();
            await _shelf.UploadAsync("/notes.txt", new MemoryStream(new byte[] { 1 }));
            ShelfDirectory emptyOtherFolder = await _shelf.CreateDirectoryAsync("/beta/2020/01/01");

            _time.Advance(TimeSpan.FromDays(1));
            await RunDailyAsync("alpha", new FakeDatabaseBackupService(_time, "shop", "wiki"), days: 60);

            foreach (string path in other.StoredPaths)
            {
                Assert.AreEqual(path, (await _shelf.GetFileAsync(path)).Path, "beta's dump was pruned by alpha");
            }

            Assert.AreEqual("/beta/2020/01/01", (await _shelf.ListAsync(emptyOtherFolder.Id)).Directory.Path);
            Assert.AreEqual("/notes.txt", (await _shelf.GetFileAsync("/notes.txt")).Path);

            // Not vacuous: alpha did prune its own folder, and its policy would have pruned beta's dumps by their age.
            Assert.IsTrue((await WholeTreeAsync("/alpha")).Files.Count < 120);
            Assert.IsFalse(ExamplePolicy().ShouldKeepFile(FirstRun.UtcDateTime, _time.GetUtcNow().UtcDateTime), "The check needs beta dumps the policy would prune");
        }

        [TestMethod]
        public async Task PerformBackupAsync_UploadFails_DoesNotPruneAndFails()
        {
            FakeDatabaseBackupService databases = new FakeDatabaseBackupService(_time, "shop", "wiki");
            BackupOrchestrator orchestrator = await RunDailyAsync("alpha", databases, days: 21);
            List<string> before = await FilePathsAsync("/alpha");
            Assert.IsTrue(await WouldPruneAsync("/alpha"), "The check needs a dump the next run would prune");

            // The shelf is full, so the next upload is refused.
            ShelfInfo info = await _shelf.GetInfoAsync();
            await _shelf.UpdateAsync(ShelfName, info.UsedBytes + info.ReservedBytes);

            BackupReport report = await orchestrator.PerformBackupAsync();

            Assert.IsFalse(report.Succeeded);
            Assert.AreEqual(PruningOutcome.SkippedAfterFailedBackup, report.Pruning);
            CollectionAssert.AreEquivalent(new[] { "shop", "wiki" }, report.FailedDatabases.ToArray());
            CollectionAssert.AreEquivalent(before, await FilePathsAsync("/alpha"));
        }

        [TestMethod]
        public async Task PerformBackupAsync_DumpFails_StoresTheOthersButDoesNotPruneAndFails()
        {
            FakeDatabaseBackupService databases = new FakeDatabaseBackupService(_time, "shop", "wiki");
            BackupOrchestrator orchestrator = await RunDailyAsync("alpha", databases, days: 21);
            List<string> before = await FilePathsAsync("/alpha");
            Assert.IsTrue(await WouldPruneAsync("/alpha"), "The check needs a dump the next run would prune");

            databases.Broken.Add("wiki");
            BackupReport report = await orchestrator.PerformBackupAsync();

            Assert.IsFalse(report.Succeeded);
            Assert.AreEqual(PruningOutcome.SkippedAfterFailedBackup, report.Pruning);
            CollectionAssert.AreEqual(new[] { "wiki" }, report.FailedDatabases.ToArray());
            Assert.AreEqual(1, report.StoredPaths.Count);
            CollectionAssert.AreEquivalent(before.Append(report.StoredPaths[0]).ToList(), await FilePathsAsync("/alpha"));
        }

        [TestMethod]
        public async Task PerformBackupAsync_PruningFails_Fails()
        {
            FakeDatabaseBackupService databases = new FakeDatabaseBackupService(_time, "shop", "wiki");
            await RunDailyAsync("alpha", databases, days: 21);
            Assert.IsTrue(await WouldPruneAsync("/alpha"), "The check needs a dump the next run would prune");

            IShelfClient refusingDeletes = RefusingDeletes.Wrap(_shelf);
            BackupOrchestrator orchestrator = new BackupOrchestrator(databases, refusingDeletes, ExamplePolicy(), new BackupSettings("alpha", "/unused.json", 60), _time, NullLogger<BackupOrchestrator>.Instance);

            BackupReport report = await orchestrator.PerformBackupAsync();

            Assert.IsFalse(report.Succeeded);
            Assert.AreEqual(PruningOutcome.Failed, report.Pruning);
            Assert.AreEqual(0, report.FailedDatabases.Count);
        }

        private BackupOrchestrator Orchestrator(string source, IDatabaseBackupService databases, RetentionPolicy? policy = null)
        {
            return new BackupOrchestrator(databases, _shelf, policy ?? ExamplePolicy(), new BackupSettings(source, "/unused.json", 60), _time, NullLogger<BackupOrchestrator>.Instance);
        }

        /// <summary>Runs the agent once a day for <paramref name="days"/> days, leaving the clock at the next day's run.</summary>
        private async Task<BackupOrchestrator> RunDailyAsync(string source, IDatabaseBackupService databases, int days)
        {
            BackupOrchestrator orchestrator = Orchestrator(source, databases);
            for (int day = 0; day < days; day++)
            {
                Assert.IsTrue((await orchestrator.PerformBackupAsync()).Succeeded);
                _time.Advance(TimeSpan.FromDays(1));
            }

            return orchestrator;
        }

        /// <summary>Whether a successful run now would delete a file under <paramref name="folder"/>.</summary>
        private async Task<bool> WouldPruneAsync(string folder)
        {
            DateTime now = _time.GetUtcNow().UtcDateTime;
            return (await WholeTreeAsync(folder)).Files.Any(file => !ExamplePolicy().ShouldKeepFile(file.CreatedAt.UtcDateTime, now));
        }

        private async Task<List<string>> FilePathsAsync(string folder)
        {
            return (await WholeTreeAsync(folder)).Files.Select(file => file.Path).ToList();
        }

        private async Task<DirectoryTree> WholeTreeAsync(string folder)
        {
            DirectoryTree tree = await _shelf.ListTreeAsync(new TreeQuery { Under = folder, Limit = TreeQuery.MaxLimit });
            Assert.IsNull(tree.NextCursor, "The test expects one page");
            return tree;
        }

        /// <summary>The doubling schedule of Documentation/retention-policy.example.json.</summary>
        private static RetentionPolicy ExamplePolicy()
        {
            return new RetentionPolicy(new List<RetentionRule>
            {
                new RetentionRule("1.00:00:00", "14.00:00:00"),
                new RetentionRule("2.00:00:00", "28.00:00:00"),
                new RetentionRule("4.00:00:00", "74.00:00:00"),
                new RetentionRule("8.00:00:00", "194.00:00:00"),
                new RetentionRule("16.00:00:00", "374.00:00:00"),
                new RetentionRule("32.00:00:00", null),
            });
        }

        /// <summary>A shelf that refuses to delete files and passes everything else to the real one.</summary>
        public class RefusingDeletes : DispatchProxy
        {
            private IShelfClient _inner = null!;

            public static IShelfClient Wrap(IShelfClient inner)
            {
                IShelfClient proxy = Create<IShelfClient, RefusingDeletes>();
                ((RefusingDeletes)(object)proxy)._inner = inner;
                return proxy;
            }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod!.Name == nameof(IShelfClient.DeleteFileAsync))
                {
                    return Task.FromException<TrashEntry>(new ByteShelfException("Deletes are refused in this test.", ErrorCodes.PermissionDenied));
                }

                try
                {
                    return targetMethod.Invoke(_inner, args);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }
        }
    }
}
