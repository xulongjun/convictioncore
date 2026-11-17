namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

public sealed class IbkrOptionEae : IbkrAccountFactRow
{
    public long TradeId { get; set; }

    public long Conid { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public DateOnly? Date { get; set; }

    public string? AssetCategory { get; set; }

    public string? Symbol { get; set; }

    public long? UnderlyingConid { get; set; }

    public string? UnderlyingSymbol { get; set; }

    public string? PutCall { get; set; }

    public decimal? Strike { get; set; }

    public DateOnly? Expiry { get; set; }

    public decimal? Multiplier { get; set; }

    public string? Currency { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? TradePrice { get; set; }

    public decimal? MarkPrice { get; set; }

    public decimal? Proceeds { get; set; }

    /// <summary>Maps IBKR's <c>commisionsAndTax</c> attribute (original spelling).</summary>
    public decimal? CommisionsAndTax { get; set; }

    public decimal? CostBasis { get; set; }

    public decimal? RealizedPnl { get; set; }

    public decimal? MtmPnl { get; set; }

    public decimal? FxPnl { get; set; }
}
