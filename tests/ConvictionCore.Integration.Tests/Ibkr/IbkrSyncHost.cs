using System.Data.Common;
using System.Text;
using ConvictionCore.Application.Ibkr;
using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using ConvictionCore.Domain.Trading;
using ConvictionCore.Infrastructure.Ibkr.Flex;
using ConvictionCore.Infrastructure.Persistence;
using ConvictionCore.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortfolioRoot = ConvictionCore.Domain.Portfolio.Portfolio;

namespace ConvictionCore.Integration.Tests.Ibkr;

/// <summary>
/// The real API host on a fresh, migrated database, with the Flex HTTP handler and the clock
/// replaced by test doubles. No real IBKR call is ever made.
/// </summary>
public sealed class IbkrSyncHost : IAsyncDisposable
{
    public static readonly string[] FactTables =
    [
        "ibkr_trade_execution",
        "ibkr_closed_lot",
        "ibkr_option_eae",
        "ibkr_open_position_lot",
        "ibkr_security",
        "ibkr_corporate_action",
        "ibkr_account_info"
    ];

    private readonly WebApplicationFactory<Program> _factory;

    private IbkrSyncHost(string connectionString, AutoAdvancingTimeProvider time, string environment, Dictionary<string, string?> settings)
    {
        ConnectionString = connectionString;
        Time = time;
        Handler = new FakeFlexHandler(time);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:ConvictionCore", connectionString);
            builder.UseSetting("DeveloperIdentity:PrincipalId", string.Empty);
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<TimeProvider>(time);
                services.AddSingleton<ILoggerProvider>(Logs);
                services.AddHttpClient(FlexWebServiceClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => Handler);
            });
        });
    }

    public string ConnectionString { get; }

    public AutoAdvancingTimeProvider Time { get; }

    public FakeFlexHandler Handler { get; }

    public CapturingLoggerProvider Logs { get; } = new();

    public static async Task<IbkrSyncHost> StartAsync(
        SqlServerFixture sql,
        string environment = "Development",
        DateTimeOffset? now = null,
        Dictionary<string, string?>? overrides = null,
        bool registerAccount = true)
    {
        var connectionString = await sql.CreateDatabaseAsync();
        var settings = FlexTestConfiguration.Settings();
        foreach (var (key, value) in overrides ?? [])
        {
            settings[key] = value;
        }

        var host = new IbkrSyncHost(connectionString, new AutoAdvancingTimeProvider(now ?? FlexTestConfiguration.Now), environment, settings);
        if (registerAccount)
        {
            await host.RegisterAccountAsync();
        }

        return host;
    }

    public HttpClient CreateClient() => _factory.CreateClient();

    public ConvictionCoreDbContext Db() =>
        new(new DbContextOptionsBuilder<ConvictionCoreDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task<IbkrFlexSyncResult> SyncAsync(DateOnly? from, DateOnly? to)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sync = scope.ServiceProvider.GetRequiredService<IIbkrFlexSync>();
        return await sync.SyncAsync(new IbkrFlexSyncRequest(from, to, ImportTrigger.Manual), CancellationToken.None);
    }

    public Task<IbkrFlexSyncResult> SyncFixtureAsync(byte[] payload, DateOnly from, DateOnly to)
    {
        Handler.ServeStatement(payload);
        return SyncAsync(from, to);
    }

    public async Task<Dictionary<string, int>> CountRowsAsync()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in FactTables.Append("ibkr_raw_statement").Append("import_batch"))
        {
            counts[table] = Convert.ToInt32(await ScalarAsync($"SELECT COUNT(*) FROM trading.{table}"), System.Globalization.CultureInfo.InvariantCulture);
        }

        return counts;
    }

    public async Task<int> FactRowCountAsync() =>
        (await CountRowsAsync()).Where(pair => FactTables.Contains(pair.Key)).Sum(pair => pair.Value);

    /// <summary>Every row of every trading table as JSON, for full-text searches (token, PII).</summary>
    public async Task<string> DumpTradingTablesAsync()
    {
        var dump = new StringBuilder();
        foreach (var table in FactTables.Append("ibkr_raw_statement").Append("import_batch"))
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM trading.{table} FOR JSON PATH";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dump.Append(reader.GetString(0));
            }
        }

        return dump.ToString();
    }

    public async Task<object?> ScalarAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }

    public async Task<BrokerAccountId> AccountIdAsync()
    {
        await using var db = Db();
        return await db.BrokerAccounts.Select(account => account.Id).SingleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        Handler.Dispose();
    }

    private async Task RegisterAccountAsync()
    {
        var now = FlexTestConfiguration.Now;
        var owner = Owner.Register(OwnerId.New(), "Synthetic Company", OwnerKind.Company, now);
        var portfolio = PortfolioRoot.Register(PortfolioId.New(), owner.Id, "Company", CurrencyCode.Parse("USD"), now);
        var account = BrokerAccount.Register(
            BrokerAccountId.New(),
            portfolio.Id,
            owner.Id,
            new BrokerId(Application.Portfolio.BrokerSeedIds.IbkrId),
            "IBKR company",
            FlexFixtures.AccountId,
            "U****001",
            CurrencyCode.Parse("USD"),
            now);
        await using var db = Db();
        db.AddRange(owner, portfolio, account);
        await db.SaveChangesAsync();
    }
}
