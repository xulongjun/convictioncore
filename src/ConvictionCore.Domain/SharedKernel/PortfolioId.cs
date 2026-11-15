namespace ConvictionCore.Domain.SharedKernel;

public readonly record struct PortfolioId(Guid Value)
{
    public static PortfolioId New() => new(Guid.CreateVersion7());
}
