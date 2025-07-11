using PgBackupAgent.Configuration.Agent;

namespace PgBackupAgent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AgentConfiguration _configuration;

        public Worker(ILogger<Worker> logger, AgentConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PostgreSQL Backup Agent started");
            _logger.LogInformation("PostgreSQL Host: {Host}:{Port}", _configuration.Postgres.Host, _configuration.Postgres.Port);
            _logger.LogInformation("Database: {Database}", _configuration.Postgres.Database);
            _logger.LogInformation("ByteShelf URL: {BaseUrl}", _configuration.ByteShelf.BaseUrl);
            _logger.LogInformation("Retention Policy Path: {RetentionPolicyPath}", _configuration.Backup.RetentionPolicyPath);
            _logger.LogInformation("Timeout Minutes: {TimeoutMinutes}", _configuration.Backup.TimeoutMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
