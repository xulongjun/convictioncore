using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrAccountInfoConfiguration : IEntityTypeConfiguration<IbkrAccountInfo>
{
    public void Configure(EntityTypeBuilder<IbkrAccountInfo> builder)
    {
        builder.ConfigureAccountFactTable("ibkr_account_info");
        builder.HasIndex(row => row.BrokerAccountId)
            .IsUnique()
            .HasDatabaseName("ux_ibkr_account_info_broker_account");
    }
}
