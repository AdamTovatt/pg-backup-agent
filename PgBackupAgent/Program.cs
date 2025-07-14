using ByteShelfClient;
using ByteShelfCommon;
using PgBackupAgent.Configuration;
using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;
using PgBackupAgent.Helpers;
using PgBackupAgent.Services.Backup;

namespace PgBackupAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Validate all required environment variables
            EnvironmentHelper.ValidateEnvironmentVariableNames(typeof(EnvironmentVariableNames));

            // Get configuration file path from environment variable
            string configFilePath = EnvironmentHelper.GetEnvironmentVariable(EnvironmentVariableNames.BackupConfigPath);

            // Create configuration provider and validate configuration
            IAgentConfigurationProvider configProvider = new FileAgentConfigurationProvider(configFilePath);
            AgentConfiguration configuration = configProvider.GetConfiguration();

            // Create retention policy provider and get retention policy
            IRetentionPolicyProvider retentionPolicyProvider = new FileRetentionPolicyProvider(configProvider);
            RetentionPolicy retentionPolicy = retentionPolicyProvider.GetRetentionPolicy();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            // Register configuration as singleton
            builder.Services.AddSingleton(configuration);
            builder.Services.AddSingleton<IAgentConfigurationProvider>(configProvider);
            builder.Services.AddSingleton<IRetentionPolicyProvider>(retentionPolicyProvider);
            builder.Services.AddSingleton(retentionPolicy);

            // Register individual settings for dependency injection
            builder.Services.AddSingleton(configuration.Postgres);
            builder.Services.AddSingleton(configuration.ByteShelf);
            builder.Services.AddSingleton(configuration.Backup);

            // Register ByteShelf HTTP client
            HttpClient httpClient = HttpShelfFileProvider.CreateHttpClient(configuration.ByteShelf.BaseUrl);

            IShelfFileProvider shelfFileProvider = new HttpShelfFileProvider(httpClient, configuration.ByteShelf.ApiKey);
            builder.Services.AddSingleton(shelfFileProvider);

            // Register backup services
            builder.Services.AddSingleton<IDatabaseBackupService, PostgresDatabaseBackupService>();
            builder.Services.AddSingleton<ISubtenantStructureService, SubtenantStructureService>();
            builder.Services.AddSingleton<IBackupOrchestrator, BackupOrchestrator>();

            // Register worker service
            builder.Services.AddHostedService<Worker>();

            IHost host = builder.Build();
            host.Run();
        }
    }
}