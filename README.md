# ConvictionCore

Clean rewrite of ConvictionOS. This repository is **code-only** — architecture and product docs live in Cursor Project Context, not here.

Stack: C# / .NET 10 modular monolith (Api, Application, Domain, Infrastructure) + SQL Server + EF Core.

## Prerequisites

- .NET SDK matching `global.json` (10.0.x)
- Docker (for local SQL Server and integration tests)

## Local SQL Server

```bash
cp .env.example .env   # edit MSSQL_SA_PASSWORD if you want
docker compose up -d
```

Set the API connection string (user secrets preferred):

```bash
dotnet user-secrets set "ConnectionStrings:ConvictionCore" \
  "Server=localhost,1433;Database=ConvictionCore;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=True" \
  --project src/ConvictionCore.Api

dotnet user-secrets set "DeveloperIdentity:PrincipalId" "local-dev" \
  --project src/ConvictionCore.Api
```

Or export `ConnectionStrings__ConvictionCore` / `DeveloperIdentity__PrincipalId` from `.env.example`.

## Migrate (explicit — not on API startup)

```bash
dotnet ef database update \
  --project src/ConvictionCore.Infrastructure \
  --startup-project src/ConvictionCore.Api
```

Restore the pinned `dotnet-ef` tool first: `dotnet tool restore`.

## Build / test / run

```bash
dotnet restore ConvictionCore.slnx
dotnet build ConvictionCore.slnx
dotnet test ConvictionCore.slnx
dotnet run --project src/ConvictionCore.Api
```

API listens on `http://localhost:5080` (see `launchSettings.json`). Health: `GET /health` or `GET /api/v1/health`.

Do not commit real broker exports, account identifiers, or connection-string secrets.
