namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

public sealed class IbkrOpenPositionLot : IbkrAccountFactRow
{
    public DateOnly ReportDate { get; set; }

    public long Conid { get; set; }

    public long OriginatingTransactionId { get; set; }

    public long? OriginatingOrderId { get; set; }

    public string? AssetCategory { get; set; }

    public string? Symbol { get; set; }

    public long? UnderlyingConid { get; set; }

    public string? PutCall { get; set; }

    public decimal? Strike { get; set; }

    public DateOnly? Expiry { get; set; }

    public decimal? Multiplier { get; set; }

    public string? Currency { get; set; }

    public decimal? FxRateToBase { get; set; }

    public decimal? Position { get; set; }

    public string? Side { get; set; }

    public decimal? MarkPrice { get; set; }

    public decimal? PositionValue { get; set; }

    public decimal? OpenPrice { get; set; }

    public decimal? CostBasisPrice { get; set; }

    public decimal? CostBasisMoney { get; set; }

    public decimal? FifoPnlUnrealized { get; set; }

    public DateTimeOffset? OpenDateTimeUtc { get; set; }

    public string? OpenDateTimeIbkrEt { get; set; }

    public DateTimeOffset? HoldingPeriodDateTimeUtc { get; set; }

    public string? HoldingPeriodDateTimeIbkrEt { get; set; }
}
