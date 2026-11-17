namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

public sealed class IbkrTradeExecution : IbkrAccountFactRow
{
    public long TransactionId { get; set; }

    public long? TradeId { get; set; }

    public string? IbExecId { get; set; }

    public long? IbOrderId { get; set; }

    public long? OrigTradeId { get; set; }

    public long? OrigTransactionId { get; set; }

    public long? RelatedTransactionId { get; set; }

    public string? TransactionType { get; set; }

    public string? AssetCategory { get; set; }

    public long Conid { get; set; }

    public long? UnderlyingConid { get; set; }

    public string? Symbol { get; set; }

    public string? Description { get; set; }

    public string? UnderlyingSymbol { get; set; }

    public string? PutCall { get; set; }

    public decimal? Strike { get; set; }

    public DateOnly? Expiry { get; set; }

    public decimal? Multiplier { get; set; }

    public string? Currency { get; set; }

    public decimal? FxRateToBase { get; set; }

    public DateTimeOffset? DateTimeUtc { get; set; }

    public string? DateTimeIbkrEt { get; set; }

    public DateOnly? TradeDate { get; set; }

    public DateOnly? SettleDateTarget { get; set; }

    public string? BuySell { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? TradePrice { get; set; }

    public decimal? Proceeds { get; set; }

    public decimal? IbCommission { get; set; }

    public string? IbCommissionCurrency { get; set; }

    public decimal? Taxes { get; set; }

    public decimal? NetCash { get; set; }

    public decimal? Cost { get; set; }

    public decimal? FifoPnlRealized { get; set; }

    public decimal? MtmPnl { get; set; }

    public string? OpenCloseIndicator { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset? OpenDateTimeUtc { get; set; }

    public string? OpenDateTimeIbkrEt { get; set; }

    public DateTimeOffset? HoldingPeriodDateTimeUtc { get; set; }

    public string? HoldingPeriodDateTimeIbkrEt { get; set; }

    public string LevelOfDetail { get; set; } = string.Empty;
}
