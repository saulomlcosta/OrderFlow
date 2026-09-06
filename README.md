# OrderFlow

OrderFlow is a software engineering laboratory that evolves from demonstrated
problems instead of anticipated complexity.

## Prerequisites

- .NET 10 SDK
- Node.js 24
- Docker Desktop

## Run Locally

Start the persistent PostgreSQL database:

```powershell
docker compose up -d postgres
```

Start the API in one terminal. The API applies pending migrations in the
Development environment and listens on `http://localhost:5216`.

```powershell
dotnet run --project src/OrderFlow.Api/OrderFlow.Api.csproj
```

Start Angular in another terminal and open `http://localhost:4200`:

```powershell
cd src/OrderFlow.Web
npm install
npm start
```

The PostgreSQL volume survives API and container restarts. Stop the environment
without deleting its data:

```powershell
docker compose down
```

Explicitly remove the local database and start from an empty volume:

```powershell
docker compose down -v
```

The credentials committed in `appsettings.Development.json` and `compose.yaml`
are local-development defaults. Production credentials must come from a secret
store or environment variables.

## Tests

Backend tests use isolated SQLite in-memory databases and do not require Docker:

```powershell
dotnet test OrderFlow.slnx
```

Frontend behavioral and browser tests also remain self-contained:

```powershell
cd src/OrderFlow.Web
npm test -- --watch=false --browsers=ChromeHeadless
npm run test:e2e
```

## Documentation

- [Current state](docs/CURRENT_STATE.MD)
- [System flow report](docs/reports/SYSTEM_FLOW.md)
- [Engineering experiments](docs/experiments)
- [Learning journal](docs/journal)
