# AGENTS.md — ConvictionCore

C# / .NET 10 modular monolith + EF Core + SQL Server. This repo is **public** and **code-only**:
specs live outside the repo (see "Specs and PBI workflow"). Treat every file you commit as world-readable.

## Commands

- Restore tools first: `dotnet tool restore` (pins `dotnet-ef` via `.config/dotnet-tools.json`).
- Build: `dotnet build ConvictionCore.slnx -c Release` — warnings are errors and code style is enforced in build; fix them, never suppress with `#pragma` or `NoWarn` without saying why in the PR.
- Format: `dotnet format ConvictionCore.slnx` before committing; CI runs `--verify-no-changes`.
- Test: `dotnet test ConvictionCore.slnx -c Release` — needs Docker (Testcontainers SQL Server 2022). If Docker is unavailable, say so in the PR; do not skip or delete tests.
- Add a migration:
  `dotnet ef migrations add <Name> --project src/ConvictionCore.Infrastructure --startup-project src/ConvictionCore.Api`
- Model/migration drift check (CI runs this):
  `dotnet ef migrations has-pending-model-changes --project src/ConvictionCore.Infrastructure --startup-project src/ConvictionCore.Api`
- Fixture hygiene check (CI runs this): `.github/scripts/check-fixture-hygiene.sh`

## Layers and dependency rules

Enforced by `tests/ConvictionCore.Architecture.Tests` — if they fail, fix the design, not the test.

- `Domain` → nothing in this solution. No EF Core, SqlClient, ASP.NET Core, `HttpClient`, `System.Xml`, logging frameworks.
- `Application` → `Domain` only. Defines ports (interfaces) for broker clients, clock, persistence.
- `Infrastructure` → `Application`, `Domain`. Owns EF Core (`DbContext`, `IEntityTypeConfiguration`, migrations), HTTP clients (e.g. IBKR Flex), XML parsing.
- `Api` → composition root: controllers + DI wiring. No business logic, no direct `DbContext` use in controllers.
- Modules are SQL schemas inside one `DbContext`: `portfolio`, `trading`. Do not add a second `DbContext`.
- Use `TimeProvider` for time; never `DateTime.Now`/`UtcNow` in Domain/Application.
- Do not add NuGet packages, change `global.json`, `Directory.Build.props`, `.editorconfig`, or `.github/` unless the PBI asks for it; if you must, call it out in the PR.

## Database, migrations, SQL conventions

- Names: `snake_case` for schemas, tables, columns; lowercase schema per module.
- Mapping lives only in Infrastructure fluent configs under `Persistence/<Module>/`. No data annotations on Domain types.
- Money `decimal(28,16)`; quantity / price / FX rate `decimal(28,10)` — use `HasMoneyPrecision()` / `HasQuantityPrecision()` from `FinancialPrecision`. Never `float`, `double`, `money`, `rowversion`.
- Concurrency: integer `version` column as concurrency token.
- Timestamps: `datetimeoffset` in UTC. Pure dates: `date`.
- Migrations live in `src/ConvictionCore.Infrastructure/Persistence/Migrations/`. Commit Designer + snapshot.
- **Never edit a migration that is already on `main`**; add a new forward migration.
- Read the generated `Up()` before committing: no silent precision/nullability changes, no dropped columns unless the PBI says so.
- Migrations are an explicit ops step. Never call `Migrate()` on API startup or in a request path.
- Never point tools at a non-local database. Reserved catalogs (`*_Prod`, shared `*_Test`) are rejected by `SqlConnectionGuard` — keep it that way.

## Tests

- xUnit. Domain logic → `ConvictionCore.Domain.Tests`; boundaries → `Architecture.Tests`; anything touching SQL → `Integration.Tests` on Testcontainers (disposable DB per fixture; never a shared DB).
- HTTP clients: fake `HttpMessageHandler`. Time/retry/rate-limit: `FakeTimeProvider`, no real sleeps.
- Name tests after behavior; when a PBI has numbered acceptance criteria, reference them (e.g. `[Trait("AC", "4")]`).
- **Fixtures must be anonymized and synthetic.** No real broker exports, account numbers (use `U0000001`; `U00000xx` for extra synthetic accounts), names, emails (only reserved domains such as `example.com`, `.invalid`, `.test`), dates of birth, or real amounts. Never commit raw IBKR XML samples or anonymization parameters. CI greps `tests/` for account ids and emails.
- Never weaken an assertion or delete a test to make CI green.

## Secrets and sensitive data

- Secrets only via `dotnet user-secrets` (dev) or environment variables (`Section__Key`). Never in `appsettings*.json`, code, tests, logs, PR text, or commit messages.
- Known secret keys: `ConnectionStrings:ConvictionCore`, `Ibkr:FlexToken`.
- Redact tokens in logs and exception messages (the Flex token travels as URL param `t=` → log `t=***`). Add a test when you add a client that carries a token.
- Parse broker payloads with field allowlists; ignore PII fields even if present.
- `.env` is gitignored; only `.env.example` with placeholders is committed.

## Specs and PBI workflow

- Specs live in the ConvictionCore wiki in Cursor Project Context (`docs/wiki/`), not in this repo.
  Your task prompt names the PBI (e.g. `docs/wiki/pbi/pbi-01-ibkr-flex-importer.md`). Read it, plus
  `docs/wiki/architecture/overview.md` and `docs/wiki/sql-devops-advice.md`.
- If you cannot access the PBI, stop and ask — do not guess scope from code.
- The PBI wins over this file on *what* to build; this file wins on *how*. If they conflict, or the PBI is ambiguous, ask in the PR instead of inventing behavior.
- One PBI per branch and PR. Stay inside the PBI's scope; list anything out-of-scope you noticed under "Follow-ups" in the PR.
- Do not copy spec text into the repo (no `docs/`, no long README sections).
- Start the PR description with a short plan: layers touched, tables/migrations, AC numbers covered. Fill in `.github/pull_request_template.md`.

## Definition of done

- [ ] `dotnet build -c Release`, `dotnet format --verify-no-changes`, and `dotnet test -c Release` pass locally (or the PR says exactly what could not run and why).
- [ ] Every PBI acceptance criterion in scope maps to at least one test, listed in the PR.
- [ ] New/changed tables have a migration; `has-pending-model-changes` is clean; `Up()` reviewed.
- [ ] No secrets, real account data, or PII in the diff, fixtures, logs, or PR text.
- [ ] Architecture tests untouched or strengthened, never loosened.
- [ ] PR states what was verified and what was not. Don't claim more than you ran.
