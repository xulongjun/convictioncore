using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.Trading;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("portfolio");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConvictionCoreDbContext).Assembly);
    }
}
