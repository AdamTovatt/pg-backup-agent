using PgBackupAgent.Configuration.Agent;
using PgBackupAgent.Services.Backup;

namespace PgBackupAgent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AgentConfiguration _configuration;
        private readonly IBackupOrchestrator _backupOrchestrator;

        public Worker(ILogger<Worker> logger, AgentConfiguration configuration, IBackupOrchestrator backupOrchestrator)
        {
            _logger = logger;
            _configuration = configuration;
            _backupOrchestrator = backupOrchestrator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PostgreSQL Backup Agent started");
            _logger.LogInformation("PostgreSQL Host: {Host}:{Port}", _configuration.Postgres.Host, _configuration.Postgres.Port);
            _logger.LogInformation("Database: {Database}", _configuration.Postgres.Database);
            _logger.LogInformation("ByteShelf URL: {BaseUrl}", _configuration.ByteShelf.BaseUrl);
            _logger.LogInformation("Retention Policy Path: {RetentionPolicyPath}", _configuration.Backup.RetentionPolicyPath);
            _logger.LogInformation("Timeout Minutes: {TimeoutMinutes}", _configuration.Backup.TimeoutMinutes);

            try
            {
                _logger.LogInformation("Starting backup operation at: {time}", DateTimeOffset.Now);
                await _backupOrchestrator.PerformBackupAsync(stoppingToken);
                _logger.LogInformation("Completed backup operation at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during backup operation");
                throw; // Re-throw to ensure the service exits with error code
            }
        }
    }
}
