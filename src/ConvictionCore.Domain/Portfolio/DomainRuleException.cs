namespace ConvictionCore.Domain.Portfolio;

public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string field, string message)
        : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}
