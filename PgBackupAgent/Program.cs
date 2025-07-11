using PgBackupAgent.Configuration;
using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Get configuration file path from environment variable
            string? configFilePath = Environment.GetEnvironmentVariable("BACKUP_CONFIG_PATH");
            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                throw new InvalidOperationException("Environment variable 'BACKUP_CONFIG_PATH' is required and must point to the configuration file.");
            }

            // Create configuration provider and validate configuration
            IAgentConfigurationProvider configProvider = new FileAgentConfigurationProvider(configFilePath);
            AgentConfiguration configuration = configProvider.GetConfiguration();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            // Register configuration as singleton
            builder.Services.AddSingleton(configuration);
            builder.Services.AddSingleton<IAgentConfigurationProvider>(configProvider);
            builder.Services.AddSingleton<IRetentionPolicyProvider, FileRetentionPolicyProvider>();
            builder.Services.AddHostedService<Worker>();

            IHost host = builder.Build();
            host.Run();
        }
    }
}