using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrOpenPositionLotConfiguration : IEntityTypeConfiguration<IbkrOpenPositionLot>
{
    public void Configure(EntityTypeBuilder<IbkrOpenPositionLot> builder)
    {
        builder.ConfigureAccountFactTable("ibkr_open_position_lot");
        builder.HasIndex(row => new { row.BrokerAccountId, row.ReportDate, row.Conid, row.OriginatingTransactionId })
            .IsUnique()
            .HasDatabaseName("ux_ibkr_open_position_lot_natural_key");
    }
}
