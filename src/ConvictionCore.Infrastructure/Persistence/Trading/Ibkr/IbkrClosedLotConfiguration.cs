using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrClosedLotConfiguration : IEntityTypeConfiguration<IbkrClosedLot>
{
    public void Configure(EntityTypeBuilder<IbkrClosedLot> builder)
    {
        builder.ConfigureAccountFactTable("ibkr_closed_lot");
        builder.HasIndex(row => new { row.BrokerAccountId, row.CloseTransactionId, row.OpenTransactionId })
            .IsUnique()
            .HasDatabaseName("ux_ibkr_closed_lot_natural_key");
        builder.HasOne<IbkrTradeExecution>()
            .WithMany()
            .HasForeignKey(row => new { row.BrokerAccountId, row.CloseTransactionId })
            .HasPrincipalKey(execution => new { execution.BrokerAccountId, execution.TransactionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
