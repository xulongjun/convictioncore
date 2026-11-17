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

## IBKR Flex sync (local, Development only)

Query ids live in `appsettings.json` (`Ibkr:Queries`). The Flex token is a secret and only goes into user-secrets:

```bash
dotnet user-secrets set "Ibkr:FlexToken" "<token>" --project src/ConvictionCore.Api
```

Register the IBKR account once (the account id stays in your local DB, never in the repo):

```bash
docker exec -i convictioncore-sql /opt/mssql-tools18/bin/sqlcmd -C -I -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
  -d ConvictionCore -v IbkrAccountId="<your IBKR account id>" <<'SQL'
DECLARE @owner uniqueidentifier = NEWID(), @portfolio uniqueidentifier = NEWID(), @now datetimeoffset = SYSUTCDATETIME();
INSERT portfolio.owner (owner_id, display_name, owner_kind, version, created_at, updated_at)
VALUES (@owner, N'Company', 'Company', 0, @now, @now);
INSERT portfolio.portfolio (portfolio_id, owner_id, name, reporting_currency, version, created_at, updated_at)
VALUES (@portfolio, @owner, N'Company', 'USD', 0, @now, @now);
INSERT portfolio.broker_account (broker_account_id, portfolio_id, owner_id, broker_id, display_name, external_reference,
  masked_reference, base_currency, status, version, created_at, updated_at)
VALUES (NEWID(), @portfolio, @owner, '22222222-2222-4222-8222-222222222222', N'IBKR company', '$(IbkrAccountId)',
  CONCAT(LEFT('$(IbkrAccountId)', 2), '****'), 'USD', 'Active', 0, @now, @now);
SQL
```

Run the API (`dotnet run --project src/ConvictionCore.Api`) and trigger a sync. Both dates are optional and default to
`Ibkr:HistoryStartDate` → today. The response carries the batch id and inserted / corrected / unchanged counts per table:

```bash
curl -s -X POST http://localhost:5080/api/ibkr/flex/sync \
  -H 'Content-Type: application/json' \
  -d '{ "fromDate": "2026-03-01", "toDate": "2026-11-17" }'
```

Test fixtures are anonymized with `tools/ibkr-fixtures/anonymize_flex.py`; its money/price scale factor is passed on the
command line and is never committed.

Do not commit real broker exports, account identifiers, or connection-string secrets.
