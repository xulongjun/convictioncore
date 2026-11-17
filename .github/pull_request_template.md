## Plan

<!-- PBI (name only, e.g. PBI-01), layers touched, tables/migrations, AC numbers covered. -->

## Acceptance criteria → tests

| AC | Test(s) |
|----|---------|
|    |         |

## Verification

- [ ] `dotnet build ConvictionCore.slnx -c Release`
- [ ] `dotnet format ConvictionCore.slnx --verify-no-changes`
- [ ] `dotnet test ConvictionCore.slnx -c Release` (Docker available: yes / no)
- [ ] `dotnet ef migrations has-pending-model-changes --project src/ConvictionCore.Infrastructure --startup-project src/ConvictionCore.Api`

**Not verified / could not run:**

## Migrations

<!-- None, or: new/changed tables and columns, precision/nullability changes, anything destructive. Confirm Up() was read. -->

## Data hygiene

- [ ] No secrets, tokens, or connection strings in the diff, logs, or this description
- [ ] Fixtures are anonymized and synthetic (account `U0000001`, reserved email domains only); no raw broker exports
- [ ] Architecture tests untouched or strengthened

## Follow-ups

<!-- Out-of-scope issues noticed while working. -->
