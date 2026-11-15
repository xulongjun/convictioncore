using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Domain.Portfolio;

public sealed class BrokerAccount
{
    private BrokerAccount()
    {
    }

    public BrokerAccountId Id { get; private set; }

    public PortfolioId PortfolioId { get; private set; }

    public OwnerId OwnerId { get; private set; }

    public BrokerId BrokerId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? ExternalReference { get; private set; }

    public string MaskedReference { get; private set; } = string.Empty;

    public CurrencyCode BaseCurrency { get; private set; }

    public AccountStatus Status { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static BrokerAccount Register(
        BrokerAccountId id,
        PortfolioId portfolioId,
        OwnerId ownerId,
        BrokerId brokerId,
        string displayName,
        string? externalReference,
        string maskedReference,
        CurrencyCode baseCurrency,
        DateTimeOffset now)
    {
        if (id.Value == Guid.Empty)
        {
            throw new DomainRuleException("brokerAccountId", "Broker account id is required.");
        }

        if (portfolioId.Value == Guid.Empty)
        {
            throw new DomainRuleException("portfolioId", "Portfolio id is required.");
        }

        if (ownerId.Value == Guid.Empty)
        {
            throw new DomainRuleException("ownerId", "Owner id is required.");
        }

        if (brokerId.Value == Guid.Empty)
        {
            throw new DomainRuleException("brokerId", "Broker id is required.");
        }

        if (string.IsNullOrEmpty(baseCurrency.Value))
        {
            throw new DomainRuleException("baseCurrency", "Base currency must be a three-letter ISO code.");
        }

        return new BrokerAccount
        {
            Id = id,
            PortfolioId = portfolioId,
            OwnerId = ownerId,
            BrokerId = brokerId,
            DisplayName = RequireDisplayName(displayName),
            ExternalReference = NormalizeExternalReference(externalReference),
            MaskedReference = RequireMaskedReference(maskedReference),
            BaseCurrency = baseCurrency,
            Status = AccountStatus.Active,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    private static string RequireDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainRuleException("displayName", "Display name is required.");
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > 200)
        {
            throw new DomainRuleException("displayName", "Display name must be at most 200 characters.");
        }

        return trimmed;
    }

    private static string RequireMaskedReference(string maskedReference)
    {
        if (string.IsNullOrWhiteSpace(maskedReference))
        {
            throw new DomainRuleException("maskedReference", "Masked reference is required.");
        }

        var trimmed = maskedReference.Trim();
        if (trimmed.Length > 32)
        {
            throw new DomainRuleException("maskedReference", "Masked reference must be at most 32 characters.");
        }

        return trimmed;
    }

    private static string? NormalizeExternalReference(string? externalReference)
    {
        if (string.IsNullOrWhiteSpace(externalReference))
        {
            return null;
        }

        var trimmed = externalReference.Trim();
        if (trimmed.Length > 128)
        {
            throw new DomainRuleException("externalReference", "External reference must be at most 128 characters.");
        }

        return trimmed;
    }
}
