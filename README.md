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