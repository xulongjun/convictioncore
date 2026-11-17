namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

public sealed class IbkrCorporateAction : IbkrAccountFactRow
{
    public long? TransactionId { get; set; }

    public long? ActionId { get; set; }

    public long Conid { get; set; }

    public string? Type { get; set; }

    public DateOnly? ReportDate { get; set; }

    public DateTimeOffset? DateTimeUtc { get; set; }

    public string? DateTimeIbkrEt { get; set; }

    public string? AssetCategory { get; set; }

    public string? Symbol { get; set; }

    public string? Currency { get; set; }

    public string? Description { get; set; }

    public string? ActionDescription { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Proceeds { get; set; }

    public decimal? Value { get; set; }

    public decimal? FifoPnlRealized { get; set; }
}
