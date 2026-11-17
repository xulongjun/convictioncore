using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence;

/// <summary>
/// Canonical SQL Server decimal scales for financial columns.
/// Money: decimal(28,16). Quantity/price/FX: decimal(28,10).
/// </summary>
internal static class FinancialPrecision
{
    public const int MoneyPrecision = 28;
    public const int MoneyScale = 16;
    public const int QuantityPrecision = 28;
    public const int QuantityScale = 10;

    public static PropertyBuilder<decimal> HasMoneyPrecision(this PropertyBuilder<decimal> property) =>
        property.HasPrecision(MoneyPrecision, MoneyScale);

    public static PropertyBuilder<decimal?> HasMoneyPrecision(this PropertyBuilder<decimal?> property) =>
        property.HasPrecision(MoneyPrecision, MoneyScale);

    public static PropertyBuilder<decimal> HasQuantityPrecision(this PropertyBuilder<decimal> property) =>
        property.HasPrecision(QuantityPrecision, QuantityScale);

    public static PropertyBuilder<decimal?> HasQuantityPrecision(this PropertyBuilder<decimal?> property) =>
        property.HasPrecision(QuantityPrecision, QuantityScale);
}
