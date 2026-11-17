namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

public sealed class IbkrClosedLot : IbkrAccountFactRow
{
    /// <summary>transactionID of the closing execution the lot follows in the XML.</summary>
    public long CloseTransactionId { get; set; }

    /// <summary>The lot's own transactionID, i.e. the opening execution.</summary>
    public long OpenTransactionId { get; set; }

    public long Conid { get; set; }

    public string? AssetCategory { get; set; }

    public string? Symbol { get; set; }

    public string? Currency { get; set; }

    public decimal? Multiplier { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? TradePrice { get; set; }

    public decimal? Cost { get; set; }

    public decimal? FifoPnlRealized { get; set; }

    public DateTimeOffset? DateTimeUtc { get; set; }

    public string? DateTimeIbkrEt { get; set; }

    public DateOnly? TradeDate { get; set; }

    public DateTimeOffset? OpenDateTimeUtc { get; set; }

    public string? OpenDateTimeIbkrEt { get; set; }

    public DateTimeOffset? HoldingPeriodDateTimeUtc { get; set; }

    public string? HoldingPeriodDateTimeIbkrEt { get; set; }

    public string? BuySell { get; set; }

    public string? Notes { get; set; }
}
