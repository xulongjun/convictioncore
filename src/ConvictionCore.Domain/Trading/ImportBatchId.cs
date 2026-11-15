namespace ConvictionCore.Domain.Trading;

public readonly record struct ImportBatchId(Guid Value)
{
    public static ImportBatchId New() => new(Guid.CreateVersion7());
}
