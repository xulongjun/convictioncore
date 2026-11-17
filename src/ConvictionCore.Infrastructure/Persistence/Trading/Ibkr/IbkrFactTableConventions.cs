using System.Text;
using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal static class IbkrFactTableConventions
{
    public const int DefaultStringLength = 64;
    public const int DescriptionLength = 512;

    private static readonly HashSet<string> MoneyProperties =
    [
        nameof(IbkrTradeExecution.Proceeds),
        nameof(IbkrTradeExecution.IbCommission),
        nameof(IbkrTradeExecution.Taxes),
        nameof(IbkrTradeExecution.NetCash),
        nameof(IbkrTradeExecution.Cost),
        nameof(IbkrTradeExecution.FifoPnlRealized),
        nameof(IbkrTradeExecution.MtmPnl),
        nameof(IbkrOptionEae.CommisionsAndTax),
        nameof(IbkrOptionEae.CostBasis),
        nameof(IbkrOptionEae.RealizedPnl),
        nameof(IbkrOptionEae.FxPnl),
        nameof(IbkrOpenPositionLot.PositionValue),
        nameof(IbkrOpenPositionLot.CostBasisMoney),
        nameof(IbkrOpenPositionLot.FifoPnlUnrealized),
        nameof(IbkrCorporateAction.Amount),
        nameof(IbkrCorporateAction.Value)
    ];

    private static readonly HashSet<string> DescriptionProperties =
    [
        nameof(IbkrTradeExecution.Description),
        nameof(IbkrCorporateAction.ActionDescription)
    ];

    /// <summary>
    /// snake_case columns, financial precisions and bounded strings for every property, then
    /// the shared audit columns and raw-statement foreign keys.
    /// </summary>
    public static void ConfigureFactTable<T>(this EntityTypeBuilder<T> builder, string table)
        where T : IbkrFactRow
    {
        builder.ToTable(table, "trading");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.RowHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(row => row.Version).IsConcurrencyToken();
        builder.HasOne<IbkrRawStatement>()
            .WithMany()
            .HasForeignKey(row => row.FirstRawStatementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IbkrRawStatement>()
            .WithMany()
            .HasForeignKey(row => row.LastChangedRawStatementId)
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.Name));
            var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            if (clrType == typeof(decimal))
            {
                var isMoney = MoneyProperties.Contains(property.Name);
                property.SetPrecision(isMoney ? FinancialPrecision.MoneyPrecision : FinancialPrecision.QuantityPrecision);
                property.SetScale(isMoney ? FinancialPrecision.MoneyScale : FinancialPrecision.QuantityScale);
            }
            else if (clrType == typeof(string) && property.Name != nameof(IbkrFactRow.RowHash))
            {
                property.SetMaxLength(DescriptionProperties.Contains(property.Name) ? DescriptionLength : DefaultStringLength);
            }
        }
    }

    public static void ConfigureAccountFactTable<T>(this EntityTypeBuilder<T> builder, string table)
        where T : IbkrAccountFactRow
    {
        builder.Property(row => row.BrokerAccountId)
            .HasConversion(id => id.Value, value => new BrokerAccountId(value));
        builder.HasOne<BrokerAccount>()
            .WithMany()
            .HasForeignKey(row => row.BrokerAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureFactTable(table);
    }

    public static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);
        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (char.IsUpper(character))
            {
                if (index > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
