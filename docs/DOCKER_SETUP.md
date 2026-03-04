# Docker Development Setup

## Quick Start

```bash
# From solution root
docker compose up -d --build
```

Access API at: http://localhost:5600/swagger

## Visual Studio Launch Profiles

### 1. https (LocalDB)

- Uses SQL Server LocalDB on Windows
- Best for pure .NET development without Docker
- Database: `(localdb)\MSSQLLocalDB`

### 2. Docker Compose (Recommended)

- Starts both API and SQL Server containers
- Runs `docker compose up --build -d` from solution root
- Access API at http://localhost:5600/swagger
- Waits for services to be ready before opening browser

### 3. Docker (API only)

- Runs only the API in Docker
- Requires manually starting SQL Server (see below)
- Useful for debugging API container issues

## Manual SQL Server Setup

If using "Docker (API only)" profile, start SQL Server manually:

```bash
docker run -d \
  --name ahkflow-sqlserver-manual \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=AHKFlow_Dev!2026" \
  -e "MSSQL_PID=Developer" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

## Connection Strings

The default connection string is configured in `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AHKFlowDb;..."
}
```

Docker profiles override this via the `ConnectionStrings__DefaultConnection` environment variable:

| Profile | Server | Set By |
|---------|--------|--------|
| https (LocalDB) | `(localdb)\MSSQLLocalDB` | appsettings.Development.json |
| Docker Compose | `sqlserver,1433` | docker-compose.yml |
| Docker (API only) | `host.docker.internal,1433` | launchSettings.json |

### Overriding Connection Strings

You can override via environment variables:
```bash
ConnectionStrings__DefaultConnection="Server=myserver;Database=AHKFlowDb;..."
```

## Architecture

```plaintext
┌─────────────────┐     ┌──────────────────┐
│   ahkflow-api   │────▶│ ahkflow-sqlserver│
│   (port 5600)   │     │   (port 1433)    │
└─────────────────┘     └──────────────────┘
        │
        ▼
   ahkflow-network
```

Both containers run on the `ahkflow-network` bridge network, allowing the API to reach SQL Server using the hostname `sqlserver`.
