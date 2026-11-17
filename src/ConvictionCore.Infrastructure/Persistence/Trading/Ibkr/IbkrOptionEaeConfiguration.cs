using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrOptionEaeConfiguration : IEntityTypeConfiguration<IbkrOptionEae>
{
    public void Configure(EntityTypeBuilder<IbkrOptionEae> builder)
    {
        builder.ConfigureAccountFactTable("ibkr_option_eae");
        builder.HasIndex(row => new { row.BrokerAccountId, row.TradeId, row.Conid, row.TransactionType })
            .IsUnique()
            .HasDatabaseName("ux_ibkr_option_eae_natural_key");
        builder.Property(row => row.TransactionType).IsRequired();
    }
}
