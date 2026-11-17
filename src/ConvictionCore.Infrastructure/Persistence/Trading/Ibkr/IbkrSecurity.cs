namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

/// <summary>Contract master data; global per conid, not per account.</summary>
public sealed class IbkrSecurity : IbkrFactRow
{
    public long Conid { get; set; }

    public string? AssetCategory { get; set; }

    public string? SubCategory { get; set; }

    public string? Symbol { get; set; }

    public string? Description { get; set; }

    public long? UnderlyingConid { get; set; }

    public string? UnderlyingSymbol { get; set; }

    public string? UnderlyingCategory { get; set; }

    public decimal? Multiplier { get; set; }

    public decimal? Strike { get; set; }

    public DateOnly? Expiry { get; set; }

    public string? PutCall { get; set; }

    public string? ListingExchange { get; set; }

    public string? Currency { get; set; }

    public string? Isin { get; set; }

    public string? Cusip { get; set; }

    public string? Figi { get; set; }

    public string? SettlementPolicyMethod { get; set; }
}
