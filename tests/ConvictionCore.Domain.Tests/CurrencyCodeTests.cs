using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Domain.Tests;

public class CurrencyCodeTests
{
    [Theory]
    [InlineData("usd", "USD")]
    [InlineData("EUR", "EUR")]
    [InlineData(" jpy ", "JPY")]
    public void Parse_normalizes_three_letter_codes(string input, string expected)
    {
        var currency = CurrencyCode.Parse(input);
        Assert.Equal(expected, currency.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("12A")]
    public void TryCreate_rejects_invalid_codes(string? input)
    {
        Assert.False(CurrencyCode.TryCreate(input, out _));
    }
}
