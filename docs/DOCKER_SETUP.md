# Docker Development Setup

This guide explains how to run AHKFlow services locally using Docker.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- .NET 10 SDK (for local development without Docker)

## Quick Start with Docker Compose (Recommended)

Docker Compose provides a complete local development environment with both the API and SQL Server.

### Start all services

```bash
# From solution root
docker compose up -d --build
```

### Access the API

- **Swagger UI**: http://localhost:5600/swagger
- **API Base URL**: http://localhost:5600/api

### View logs

```bash
# All services
docker compose logs -f

# API only
docker logs ahkflow-api -f

# SQL Server only
docker logs ahkflow-sqlserver -f
```

### Stop services

```bash
docker compose down
```

### Reset database (removes all data)

```bash
docker compose down -v
docker compose up -d --build
```

## Visual Studio Launch Profiles

Three launch profiles are configured in `launchSettings.json`:

### 1. https (LocalDB)

- Uses SQL Server LocalDB on Windows
- Best for pure .NET development without Docker
- Database: `(localdb)\MSSQLLocalDB`

### 2. Docker Compose (Recommended)

- Starts both API and SQL Server containers
- Runs `docker compose up --build -d` from solution root
- Access API at http://localhost:5600/swagger

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

| Environment | Connection String |
|------------|-------------------|
| LocalDB (Development) | `Server=(localdb)\MSSQLLocalDB;Database=AHKFlowDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |
| Docker | `Server=sqlserver,1433;Database=AHKFlowDb;User Id=sa;Password=AHKFlow_Dev!2026;TrustServerCertificate=True;MultipleActiveResultSets=true` |

## Database Migrations

Migrations are automatically applied when the API starts in `Development` or `Docker` environments.

### Create a new migration

```bash
cd src/Backend/AHKFlow.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../AHKFlow.API
```

### Apply migrations manually

```bash
cd src/Backend/AHKFlow.API
dotnet ef database update
```

## Troubleshooting

### Container won't start

```bash
# Check container status
docker ps -a

# View container logs
docker logs ahkflow-api

# Rebuild from scratch
docker compose down -v
docker compose up -d --build
```

### Database connection issues

1. Ensure SQL Server container is healthy: `docker ps` should show "healthy"
2. Wait for SQL Server to fully start (30+ seconds on first run)
3. Check connection string matches the environment

### Port conflicts

If ports 5600 or 1433 are already in use:

```bash
# Find what's using the port
netstat -ano | findstr :5600

# Or modify docker-compose.yml to use different ports
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
