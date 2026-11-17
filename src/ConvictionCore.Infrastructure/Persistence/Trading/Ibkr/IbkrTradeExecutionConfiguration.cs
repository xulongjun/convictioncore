using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrTradeExecutionConfiguration : IEntityTypeConfiguration<IbkrTradeExecution>
{
    public void Configure(EntityTypeBuilder<IbkrTradeExecution> builder)
    {
        builder.ConfigureAccountFactTable("ibkr_trade_execution");
        builder.HasAlternateKey(row => new { row.BrokerAccountId, row.TransactionId })
            .HasName("ux_ibkr_trade_execution_natural_key");
        builder.Property(row => row.LevelOfDetail).IsRequired();
    }
}
