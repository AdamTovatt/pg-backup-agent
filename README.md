# PgBackupAgent

A PostgreSQL backup agent that creates database backups and uploads them to ByteShelf storage with configurable retention policies. Designed to be run as a systemctl one-shot service.

## Configuration

The agent requires two configuration files to run:

### 1. Main Configuration File

Referenced by the `BACKUP_CONFIG_PATH` environment variable, this JSON file contains the agent configuration:

```json
{
  "postgres": {
    "host": "localhost",
    "port": 5432,
    "username": "postgres",
    "password": "your_password",
    "database": "postgres"
  },
  "byteShelf": {
    "baseUrl": "https://your-byteshelf-server.com",
    "apiKey": "your-api-key"
  },
  "backup": {
    "retentionPolicyPath": "/path/to/retention-policy.json",
    "timeoutMinutes": 30
  }
}
```

### 2. Retention Policy File

Referenced by the `retentionPolicyPath` in the main configuration, this JSON file defines the backup retention rules:

```json
{
  "rules": [
    {
      "keepEvery": "1.00:00:00",
      "duration": "14.00:00:00"
    },
    {
      "keepEvery": "2.00:00:00",
      "duration": "28.00:00:00"
    },
    {
      "keepEvery": "4.00:00:00",
      "duration": "74.00:00:00"
    },
    {
      "keepEvery": "8.00:00:00",
      "duration": "194.00:00:00"
    },
    {
      "keepEvery": "16.00:00:00",
      "duration": "374.00:00:00"
    },
    {
      "keepEvery": "32.00:00:00",
      "duration": null
    }
  ]
}
```

## Environment Variables

- `BACKUP_CONFIG_PATH` - Path to the main configuration file (required)

## Systemd Service Setup

To run the agent as a scheduled service using systemd, create the following files:

### Timer Configuration

Create `/etc/systemd/system/pg-backup-agent.timer`:

```ini
[Unit]
Description=Run Pg-Backup-Agent daily at 01:00 UTC

[Timer]
OnCalendar=*-*-* 01:00:00
Persistent=false
Timezone=UTC

[Install]
WantedBy=timers.target
```

### Service Configuration

Create `/etc/systemd/system/pg-backup-agent.service`:

```ini
[Unit]
Description=Pg-Backup-Agent Service
After=nginx.service

[Service]
Type=oneshot
User=pi
WorkingDirectory=/opt/pg-backup-agent
ExecStart=/opt/pg-backup-agent/PgBackupAgent
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=BACKUP_CONFIG_PATH=/etc/pg-backup-agent/config.json

[Install]
WantedBy=multi-user.target
```

After creating these files, enable and start the timer:

```bash
sudo systemctl daemon-reload
sudo systemctl enable pg-backup-agent.timer
sudo systemctl start pg-backup-agent.timer
``` 

Run `sudo systemctl list-timers --all` to verify that it was registered.