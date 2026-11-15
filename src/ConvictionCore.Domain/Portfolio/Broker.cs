namespace ConvictionCore.Domain.Portfolio;

public sealed class Broker
{
    private Broker()
    {
    }

    public BrokerId Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public static Broker Seed(Guid id, string code, string displayName) =>
        new()
        {
            Id = new BrokerId(id),
            Code = code,
            DisplayName = displayName
        };
}
