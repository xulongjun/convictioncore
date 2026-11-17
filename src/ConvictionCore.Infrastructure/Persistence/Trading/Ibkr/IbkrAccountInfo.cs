namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

/// <summary>
/// Minimal account facts. Personal fields (e-mail, date of birth, names, representatives)
/// are never parsed or stored.
/// </summary>
public sealed class IbkrAccountInfo : IbkrAccountFactRow
{
    public string? Currency { get; set; }

    public string? AccountType { get; set; }

    public string? CustomerType { get; set; }

    public DateOnly? DateOpened { get; set; }

    public DateOnly? DateFunded { get; set; }
}
