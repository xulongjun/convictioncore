using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrCorporateActionConfiguration : IEntityTypeConfiguration<IbkrCorporateAction>
{
    public void Configure(EntityTypeBuilder<IbkrCorporateAction> builder)
    {
        builder.ConfigureAccountFactTable("ibkr_corporate_action");
        builder.HasIndex(row => new { row.BrokerAccountId, row.TransactionId })
            .IsUnique()
            .HasFilter("[transaction_id] IS NOT NULL")
            .HasDatabaseName("ux_ibkr_corporate_action_transaction");
        builder.HasIndex(row => new { row.BrokerAccountId, row.ActionId, row.Conid })
            .IsUnique()
            .HasFilter("[transaction_id] IS NULL")
            .HasDatabaseName("ux_ibkr_corporate_action_action_conid");
    }
}
