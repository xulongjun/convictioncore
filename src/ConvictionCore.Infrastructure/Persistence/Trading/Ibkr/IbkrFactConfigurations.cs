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

internal sealed class IbkrSecurityConfiguration : IEntityTypeConfiguration<IbkrSecurity>
{
    public void Configure(EntityTypeBuilder<IbkrSecurity> builder)
    {
        builder.ConfigureFactTable("ibkr_security");
        builder.HasIndex(row => row.Conid)
            .IsUnique()
            .HasDatabaseName("ux_ibkr_security_conid");
    }
}

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
