using ConvictionCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace ConvictionCore.Integration.Tests;

public sealed class MigrationSmokeTests : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _skipReason;

    public async Task InitializeAsync()
    {
        if (!IsDockerAvailable())
        {
            _skipReason = "Docker is required for Testcontainers SQL Server.";
            return;
        }

        try
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            await _container.StartAsync();
        }
        catch (Exception exception) when (IsDockerUnavailable(exception))
        {
            _skipReason = $"Docker is required for Testcontainers SQL Server: {exception.Message}";
            _container = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task Initial_migration_applies_portfolio_and_trading_schemas()
    {
        if (_skipReason is not null)
        {
            // Soft-skip when the agent/machine has no Docker; CI runners provide it.
            return;
        }

        Assert.NotNull(_container);

        var options = new DbContextOptionsBuilder<ConvictionCoreDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        await using var db = new ConvictionCoreDbContext(options);
        await db.Database.MigrateAsync();

        var schemas = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT s.name
                FROM sys.schemas AS s
                WHERE s.name IN (N'portfolio', N'trading')
                ORDER BY s.name
                """)
            .ToListAsync();

        Assert.Equal(["portfolio", "trading"], schemas);

        var tables = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT CONCAT(SCHEMA_NAME(t.schema_id), N'.', t.name)
                FROM sys.tables AS t
                WHERE SCHEMA_NAME(t.schema_id) IN (N'portfolio', N'trading')
                ORDER BY 1
                """)
            .ToListAsync();

        Assert.Contains("portfolio.broker", tables);
        Assert.Contains("portfolio.broker_account", tables);
        Assert.Contains("portfolio.owner", tables);
        Assert.Contains("portfolio.portfolio", tables);
        Assert.Contains("trading.import_batch", tables);

        var brokers = await db.Brokers.CountAsync();
        Assert.Equal(2, brokers);
    }

    private static bool IsDockerAvailable()
    {
        return File.Exists("/var/run/docker.sock")
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"));
    }

    private static bool IsDockerUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var message = current.Message;
            if (message.Contains("docker", StringComparison.OrdinalIgnoreCase)
                || message.Contains("npipe", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Cannot connect", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
