using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

/// <summary>
/// Columns shared by every parsed IBKR fact table. Only properties declared on the
/// concrete row type take part in <see cref="RowHash"/>.
/// </summary>
public abstract class IbkrFactRow
{
    public Guid Id { get; set; }

    public string RowHash { get; set; } = string.Empty;

    public Guid FirstRawStatementId { get; set; }

    public Guid LastChangedRawStatementId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int Version { get; set; }
}

public abstract class IbkrAccountFactRow : IbkrFactRow
{
    public BrokerAccountId BrokerAccountId { get; set; }
}
