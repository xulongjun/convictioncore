namespace ConvictionCore.Domain.SharedKernel;

public readonly record struct CurrencyCode
{
    private CurrencyCode(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? text, out CurrencyCode currency)
    {
        currency = default;
        if (text is null)
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.Length != 3)
        {
            return false;
        }

        Span<char> letters = stackalloc char[3];
        for (var index = 0; index < 3; index++)
        {
            var character = trimmed[index];
            if (!char.IsAsciiLetter(character))
            {
                return false;
            }

            letters[index] = char.ToUpperInvariant(character);
        }

        currency = new CurrencyCode(new string(letters));
        return true;
    }

    public static CurrencyCode Parse(string text)
    {
        if (!TryCreate(text, out var currency))
        {
            throw new FormatException("Currency code must be three letters.");
        }

        return currency;
    }

    public override string ToString() => Value;
}
