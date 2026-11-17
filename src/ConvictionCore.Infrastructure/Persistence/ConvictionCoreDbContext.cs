using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.Trading;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;
using Microsoft.EntityFrameworkCore;
using PortfolioRoot = ConvictionCore.Domain.Portfolio.Portfolio;

namespace ConvictionCore.Infrastructure.Persistence;

public sealed class ConvictionCoreDbContext(DbContextOptions<ConvictionCoreDbContext> options) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<PortfolioRoot> Portfolios => Set<PortfolioRoot>();

    public DbSet<Broker> Brokers => Set<Broker>();

    public DbSet<BrokerAccount> BrokerAccounts => Set<BrokerAccount>();

    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    public DbSet<IbkrRawStatement> IbkrRawStatements => Set<IbkrRawStatement>();

    public DbSet<IbkrTradeExecution> IbkrTradeExecutions => Set<IbkrTradeExecution>();

    public DbSet<IbkrClosedLot> IbkrClosedLots => Set<IbkrClosedLot>();

    public DbSet<IbkrOptionEae> IbkrOptionEaes => Set<IbkrOptionEae>();

    public DbSet<IbkrOpenPositionLot> IbkrOpenPositionLots => Set<IbkrOpenPositionLot>();

    public DbSet<IbkrSecurity> IbkrSecurities => Set<IbkrSecurity>();

    public DbSet<IbkrCorporateAction> IbkrCorporateActions => Set<IbkrCorporateAction>();

    public DbSet<IbkrAccountInfo> IbkrAccountInfos => Set<IbkrAccountInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("portfolio");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConvictionCoreDbContext).Assembly);
    }
}
