using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Domain.Portfolio;

public sealed class Owner
{
    private Owner()
    {
    }

    public OwnerId Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public OwnerKind Kind { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Owner Register(OwnerId id, string displayName, OwnerKind kind, DateTimeOffset now)
    {
        if (id.Value == Guid.Empty)
        {
            throw new DomainRuleException("ownerId", "Owner id is required.");
        }

        return new Owner
        {
            Id = id,
            DisplayName = RequireDisplayName(displayName),
            Kind = kind,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
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
}
