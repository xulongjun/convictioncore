using System.Security.Cryptography;
using ConvictionCore.Infrastructure.Ibkr.Flex;
using ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;
using ConvictionCore.Infrastructure.Ibkr.Sync;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;
using ConvictionCore.Tests.Shared;

namespace ConvictionCore.Infrastructure.Tests;

public class FlexBuildingBlockTests
{
    [Fact]
    [Trait("AC", "8")]
    public void Long_windows_split_into_consecutive_windows_of_at_most_365_days()
    {
        var windows = FlexDateWindow.Split(new DateOnly(2026, 3, 1), new DateOnly(2027, 6, 30), 365);

        Assert.Equal(2, windows.Count);
        Assert.Equal(new FlexDateWindow(new DateOnly(2026, 3, 1), new DateOnly(2027, 2, 28)), windows[0]);
        Assert.Equal(new FlexDateWindow(new DateOnly(2027, 3, 1), new DateOnly(2027, 6, 30)), windows[1]);
        Assert.All(windows, window => Assert.True(window.Days <= 365));
    }

    [Fact]
    public void Short_window_stays_a_single_window()
    {
        var window = Assert.Single(FlexDateWindow.Split(new DateOnly(2026, 3, 1), new DateOnly(2026, 11, 17), 365));

        Assert.Equal(262, window.Days);
    }

    [Theory]
    [InlineData("1019", FlexErrorHandling.Poll)]
    [InlineData("1001", FlexErrorHandling.Poll)]
    [InlineData("1009", FlexErrorHandling.Poll)]
    [InlineData("1021", FlexErrorHandling.Poll)]
    [InlineData("1018", FlexErrorHandling.RateLimited)]
    [InlineData("1017", FlexErrorHandling.RenewReference)]
    [InlineData("1003", FlexErrorHandling.Fatal)]
    [InlineData("1012", FlexErrorHandling.Fatal)]
    [InlineData("1015", FlexErrorHandling.Fatal)]
    [InlineData("1025", FlexErrorHandling.Fatal)]
    [InlineData("9999", FlexErrorHandling.Fatal)]
    public void Error_codes_are_classified(string code, FlexErrorHandling expected) =>
        Assert.Equal(expected, FlexErrorCodes.Classify(code));

    [Theory]
    [InlineData("https://host/SendRequest?t=abc123&q=1&v=3", "https://host/SendRequest?t=***&q=1&v=3")]
    [InlineData("GET https://host/GetStatement?q=9&t=abc123", "GET https://host/GetStatement?q=9&t=***")]
    public void Token_query_parameter_is_masked(string input, string expected) =>
        Assert.Equal(expected, FlexRedaction.Redact(input));

    [Fact]
    public void Literal_token_is_masked_anywhere()
    {
        var redacted = FlexRedaction.Redact($"failed: {FlexTestConfiguration.SentinelToken} rejected", FlexTestConfiguration.SentinelToken);

        Assert.DoesNotContain(FlexTestConfiguration.SentinelToken, redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void Gzip_round_trips_and_sha256_hashes_uncompressed_bytes()
    {
        var payload = FlexFixtures.Read(FlexFixtures.ActivityV3);

        var compressed = FlexPayloadCodec.Compress(payload);

        Assert.True(compressed.Length < payload.Length / 4);
        Assert.Equal(payload, FlexPayloadCodec.Decompress(compressed));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(payload)), FlexPayloadCodec.Sha256Hex(payload));
    }

    [Fact]
    public void Row_hash_ignores_attribute_order_and_unrelated_attributes()
    {
        var original = FlexFixtures.ReadText(FlexFixtures.Synthetic);
        var reordered = original
            .Replace("tradeID=\"900000101\" multiplier=\"1\"", "multiplier=\"1\" tradeID=\"900000101\" newIbkrField=\"x\"", StringComparison.Ordinal)
            .Replace("tradePrice=\"100\"", "tradePrice=\"100.000\"", StringComparison.Ordinal);

        var first = FlexActivityParser.Parse(FlexFixtures.Bytes(original)).Executions[0];
        var second = FlexActivityParser.Parse(FlexFixtures.Bytes(reordered)).Executions[0];

        Assert.Equal(IbkrRowHasher.Compute(first), IbkrRowHasher.Compute(second));
    }

    [Fact]
    [Trait("AC", "4")]
    public void Row_hash_changes_when_a_persisted_field_changes()
    {
        var execution = new IbkrTradeExecution { TransactionId = 1, Conid = 2, IbCommission = -1m, LevelOfDetail = "EXECUTION" };
        var before = IbkrRowHasher.Compute(execution);

        execution.IbCommission = -1.5m;

        Assert.NotEqual(before, IbkrRowHasher.Compute(execution));
    }

    [Fact]
    public void Row_hash_excludes_audit_columns()
    {
        var execution = new IbkrTradeExecution { TransactionId = 1, Conid = 2, LevelOfDetail = "EXECUTION" };
        var before = IbkrRowHasher.Compute(execution);

        execution.Version = 7;
        execution.Id = Guid.NewGuid();
        execution.UpdatedAt = DateTimeOffset.UnixEpoch;

        Assert.Equal(before, IbkrRowHasher.Compute(execution));
    }

    [Theory]
    [InlineData("1.50", "1.5")]
    [InlineData("-0.000", "0")]
    [InlineData("100", "100")]
    [InlineData("0.0000001", "0.0000001")]
    public void Decimals_hash_in_invariant_trailing_zero_free_form(string input, string expected) =>
        Assert.Equal(expected, IbkrRowHasher.Format(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)));
}
