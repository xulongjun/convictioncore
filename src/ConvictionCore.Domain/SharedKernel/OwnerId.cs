namespace ConvictionCore.Domain.SharedKernel;

public readonly record struct OwnerId(Guid Value)
{
    public static OwnerId New() => new(Guid.CreateVersion7());
}
