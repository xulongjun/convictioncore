namespace ConvictionCore.Domain.SharedKernel;

public readonly record struct BrokerAccountId(Guid Value)
{
    public static BrokerAccountId New() => new(Guid.CreateVersion7());
}
