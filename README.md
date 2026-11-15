# PgBackupAgent

Backs up every database on a PostgreSQL server to a shelf on a [ByteShelf](https://github.com/AdamTovatt/byte-shelf) 2.0 server, then prunes old backups by a retention policy. It runs once and exits, so it is meant to be started by a systemd timer as a oneshot service.

## What it does

Each run:

1. Lists the server's databases, leaving out `postgres`, `template0` and `template1`.
2. Dumps each database with `pg_dump` in its tar format and stores the dump on the shelf at
   `/<source>/YYYY/MM/DD/<database>_<yyyy-MM-dd_HH-mm-ss>.tar` (UTC), typed `application/x-tar`.
   `<source>` names the Postgres server, so agents backing up different servers can share one shelf even when their databases have the same names.
3. If every database was stored, prunes `/<source>/`: each file the retention policy does not keep, judged by when the file was stored, is deleted, and then every directory left with no file under it. Nothing outside `/<source>/` is touched.
   If any database failed, nothing is pruned, so a failing backup never thins out the backups that still work.

Pruned files go to the shelf's trash, where ByteShelf keeps them restorable for its restore window (30 days by default). They count against the shelf's quota until they are purged.

A dump is held in memory while it is uploaded, one database at a time.

### Exit code

The process exits with `0` when every database was stored and pruning went through, and with `1` when the configuration is invalid, the databases cannot be listed, any dump or upload fails, or pruning fails. Under systemd a failed run shows as a failed unit (`systemctl status`, `systemctl --failed`).

### Restoring

Download a dump from the shelf (the ByteShelf web UI or `byteshelf get <shelf>:<path> <file>`) and restore it with `pg_restore`, for example `pg_restore -d <database> <database>_<time>.tar`.

## Requirements

- `pg_dump` and `psql` on the `PATH` (on Debian and Ubuntu, the `postgresql-client` package), of the server's major version or newer.
- A ByteShelf 2.0 server with a shelf for the backups, and a **read-write** key for that shelf (pruning deletes files, which an add-only key cannot). Give each agent its own key.

## Configuration

The agent reads two JSON files.

### 1. Main configuration file

Named by the `BACKUP_CONFIG_PATH` environment variable (required). Example: [config.example.json](PgBackupAgent/Documentation/config.example.json).

```json
{
  "postgres": {
    "host": "localhost",
    "port": 5432,
    "username": "postgres",
    "password": "your_password_here",
    "database": "postgres"
  },
  "byteShelf": {
    "url": "https://files.example.com",
    "shelf": "postgres-backups",
    "shelfKey": "bsk_your_read_write_shelf_key"
  },
  "backup": {
    "source": "db-server-1",
    "retentionPolicyPath": "/etc/pg-backup-agent/retention-policy.json",
    "timeoutMinutes": 60
  }
}
```

| Setting | Meaning |
|---|---|
| `postgres.*` | How to reach the Postgres server. The user must be allowed to list and dump every database. |
| `byteShelf.url` | The ByteShelf server's address, `http` or `https`. |
| `byteShelf.shelf` | The shelf the backups are stored on. |
| `byteShelf.shelfKey` | A read-write key's secret for that shelf (it starts with `bsk_`). |
| `backup.source` | Names the Postgres server; its backups are stored under `/<source>/`. One name, no `/`. Agents sharing a shelf each need their own. |
| `backup.retentionPolicyPath` | Path to the retention policy file. |
| `backup.timeoutMinutes` | How long one database's dump may take. |

The file holds the Postgres password and the shelf key, so make it readable only by the user the agent runs as (`chmod 600`).

### 2. Retention policy file

Named by `backup.retentionPolicyPath`. Example: [retention-policy.example.json](PgBackupAgent/Documentation/retention-policy.example.json).

```json
{
  "rules": [
    { "keepEvery": "1.00:00:00", "duration": "14.00:00:00" },
    { "keepEvery": "2.00:00:00", "duration": "28.00:00:00" },
    { "keepEvery": "4.00:00:00", "duration": "74.00:00:00" },
    { "keepEvery": "8.00:00:00", "duration": "194.00:00:00" },
    { "keepEvery": "16.00:00:00", "duration": "374.00:00:00" },
    { "keepEvery": "32.00:00:00", "duration": null }
  ]
}
```

A file is judged by the first rule whose `duration` (counted back from now) covers when it was stored, and kept when its day falls on that rule's `keepEvery` interval, counted in whole days from 2024-01-01. Each `keepEvery` must be a multiple of the one before. The example keeps every day's backup for two weeks, then every second day's up to four weeks, and so on, doubling the gap as backups age; `null` means the last rule applies forever. A file older than every rule's duration is kept.

## Installing with updaemon

Each release carries `pg-backup-agent-linux-arm64.zip` and `pg-backup-agent-linux-x64.zip` (self-contained, no .NET runtime needed) with an `updaemon.json`, and a `SHA256SUMS`, so [updaemon](https://github.com/AdamTovatt/updaemon) 1.0 installs and updates it:

```bash
sudo updaemon install pg-backup-agent --from github:AdamTovatt/pg-backup-agent \
  --set BACKUP_CONFIG_PATH=/etc/pg-backup-agent/config.json
```

The manifest describes the agent as a **command**, `pg-backup-agent`, put on the `PATH` (`/usr/local/bin` system-wide), with `BACKUP_CONFIG_PATH` as its one setting. It is not an updaemon **service**: updaemon runs a service continuously, restarts it whenever it stops and rolls back a version that does not keep running, while the agent exits after each run. So updaemon keeps the command current, and the timer below runs it. The command loads the app's settings and then `exec`s the agent, so the agent's exit code is the unit's.

## Running it on a timer

Create `/etc/systemd/system/pg-backup-agent.service`:

```ini
[Unit]
Description=Back up PostgreSQL to ByteShelf
Wants=network-online.target
After=network-online.target

[Service]
Type=oneshot
User=pg-backup
ExecStart=/usr/local/bin/pg-backup-agent
```

and `/etc/systemd/system/pg-backup-agent.timer`:

```ini
[Unit]
Description=Run pg-backup-agent daily at 01:00 UTC

[Timer]
OnCalendar=*-*-* 01:00:00 UTC
Persistent=false

[Install]
WantedBy=timers.target
```

`User=` is the user the agent runs as; it must be able to read the configuration file. Then turn the timer on:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now pg-backup-agent.timer
```

`systemctl list-timers` shows when it runs next, `sudo systemctl start pg-backup-agent.service` runs it now, and `journalctl -u pg-backup-agent.service` shows its log.

Without updaemon, unzip a release (or `dotnet publish PgBackupAgent -c Release -r linux-x64 --self-contained true -o /opt/pg-backup-agent`) and point `ExecStart=` at the `PgBackupAgent` executable, with `Environment=BACKUP_CONFIG_PATH=/etc/pg-backup-agent/config.json` in the `[Service]` section.

## Releasing

The version is `<Version>` in [PgBackupAgent.csproj](PgBackupAgent/PgBackupAgent.csproj). To release it, push a tag `v<that version>`, for example `v2.0.0`. The [release workflow](.github/workflows/release.yml) refuses a tag that does not match the csproj, runs the tests, publishes both platforms, writes `SHA256SUMS` and creates the GitHub release.

## Development

```bash
dotnet build
dotnet test
```

The orchestrator's tests run against ByteShelfClient's in-memory client with a fake clock, so they need neither a ByteShelf server nor Postgres.
