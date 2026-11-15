using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Domain.Portfolio;

public sealed class Portfolio
{
    private Portfolio()
    {
    }

    public PortfolioId Id { get; private set; }

    public OwnerId OwnerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public CurrencyCode ReportingCurrency { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Portfolio Register(
        PortfolioId id,
        OwnerId ownerId,
        string name,
        CurrencyCode reportingCurrency,
        DateTimeOffset now)
    {
        if (id.Value == Guid.Empty)
        {
            throw new DomainRuleException("portfolioId", "Portfolio id is required.");
        }

        if (ownerId.Value == Guid.Empty)
        {
            throw new DomainRuleException("ownerId", "Owner id is required.");
        }

        if (string.IsNullOrEmpty(reportingCurrency.Value))
        {
            throw new DomainRuleException("reportingCurrency", "Reporting currency must be a three-letter ISO code.");
        }

        return new Portfolio
        {
            Id = id,
            OwnerId = ownerId,
            Name = RequireName(name),
            ReportingCurrency = reportingCurrency,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("name", "Portfolio name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new DomainRuleException("name", "Portfolio name must be at most 200 characters.");
        }

        return trimmed;
    }
}
