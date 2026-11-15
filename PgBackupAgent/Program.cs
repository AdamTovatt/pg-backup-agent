using ByteShelfClient;
using ByteShelfClient.Http;
using PgBackupAgent.Configuration;
using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;
using PgBackupAgent.Helpers;
using PgBackupAgent.Services.Backup;

namespace PgBackupAgent
{
    /// <summary>
    /// Runs one backup and exits: 0 when every database was stored and pruning went through, 1 otherwise,
    /// so a systemd oneshot run by a timer shows the failure.
    /// </summary>
    public class Program
    {
        private const int Success = 0;
        private const int Failure = 1;

        public static async Task<int> Main(string[] args)
        {
            AgentConfiguration configuration;
            RetentionPolicy retentionPolicy;
            try
            {
                EnvironmentHelper.ValidateEnvironmentVariableNames(typeof(EnvironmentVariableNames));
                string configFilePath = EnvironmentHelper.GetEnvironmentVariable(EnvironmentVariableNames.BackupConfigPath);

                IAgentConfigurationProvider configProvider = new FileAgentConfigurationProvider(configFilePath);
                configuration = configProvider.GetConfiguration();
                retentionPolicy = new FileRetentionPolicyProvider(configProvider).GetRetentionPolicy();
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return Failure;
            }

            // appsettings.json is read from beside the executable, whatever directory it is started in.
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory,
            });

            builder.Services.AddSingleton(configuration);
            builder.Services.AddSingleton(configuration.Postgres);
            builder.Services.AddSingleton(configuration.ByteShelf);
            builder.Services.AddSingleton(configuration.Backup);
            builder.Services.AddSingleton(retentionPolicy);
            builder.Services.AddSingleton(TimeProvider.System);

            HttpClient http = new HttpClient { BaseAddress = configuration.ByteShelf.ServerAddress };
            IByteShelfClient byteShelf = new ByteShelfHttpClient(http, new ShelfKeyCredentials(configuration.ByteShelf.ShelfKey));
            builder.Services.AddSingleton(byteShelf.Shelf(configuration.ByteShelf.Shelf));

            builder.Services.AddSingleton<IDatabaseBackupService, PostgresDatabaseBackupService>();
            builder.Services.AddSingleton<IBackupOrchestrator, BackupOrchestrator>();

            using IHost host = builder.Build();
            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
            IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            // Started for its console lifetime alone: a SIGTERM or Ctrl+C cancels the run through ApplicationStopping.
            await host.StartAsync();

            int exitCode;
            try
            {
                logger.LogInformation("Backing up {Host}:{Port} as source {Source} to shelf {Shelf} on {Url}",
                    configuration.Postgres.Host, configuration.Postgres.Port, configuration.Backup.Source, configuration.ByteShelf.Shelf, configuration.ByteShelf.Url);

                BackupReport report = await host.Services.GetRequiredService<IBackupOrchestrator>().PerformBackupAsync(lifetime.ApplicationStopping);

                if (report.Succeeded)
                {
                    logger.LogInformation("Backup completed: stored {StoredCount} databases and pruned", report.StoredPaths.Count);
                    exitCode = Success;
                }
                else
                {
                    logger.LogError("Backup failed: stored {StoredCount} databases, {FailedCount} failed ({FailedDatabases}), pruning {Pruning}",
                        report.StoredPaths.Count, report.FailedDatabases.Count, string.Join(", ", report.FailedDatabases), report.Pruning);
                    exitCode = Failure;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Backup failed");
                exitCode = Failure;
            }

            await host.StopAsync();
            return exitCode;
        }
    }
}
