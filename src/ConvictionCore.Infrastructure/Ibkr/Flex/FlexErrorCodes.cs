namespace ConvictionCore.Infrastructure.Ibkr.Flex;

internal enum FlexErrorHandling
{
    /// <summary>Statement not ready yet: keep polling with backoff.</summary>
    Poll,

    /// <summary>1018: pause at least 60 s, then retry the same call.</summary>
    RateLimited,

    /// <summary>1017: reference code invalid or expired: SendRequest again once.</summary>
    RenewReference,

    Fatal
}

/// <summary>
/// Flex Web Service error codes (PBI-01 §8), checked against
/// https://www.interactivebrokers.com/docs/web-api/flex-web-service/error-codes.
/// </summary>
internal static class FlexErrorCodes
{
    public const string PollTimeout = "PollTimeout";
    public const string RateLimitExhausted = "1018";
    public const string HttpTimeout = "HttpTimeout";
    public const string UnexpectedResponse = "UnexpectedResponse";
    public const string EnvelopeMismatch = "EnvelopeMismatch";
    public const string ParseError = "ParseError";
    public const string Unexpected = "Unexpected";
    public const string Canceled = "Canceled";

    private static readonly HashSet<string> PollCodes = ["1019", "1001", "1004", "1005", "1006", "1007", "1008", "1009", "1021"];

    public static FlexErrorHandling Classify(string code) => code switch
    {
        _ when PollCodes.Contains(code) => FlexErrorHandling.Poll,
        "1018" => FlexErrorHandling.RateLimited,
        "1017" => FlexErrorHandling.RenewReference,
        _ => FlexErrorHandling.Fatal
    };

    public static string Hint(string code) => code switch
    {
        "1003" => "Statement is not available.",
        "1012" => "Flex token expired; rotate it and update user-secrets Ibkr:FlexToken.",
        "1015" => "Flex token invalid; check user-secrets Ibkr:FlexToken.",
        "1013" => "IP restriction; check the IP binding of the Flex token.",
        "1010" => "Legacy Flex query; recreate it as a Flex v3 query.",
        "1011" => "Service account is inactive.",
        "1014" => "Query is invalid; check Ibkr:Queries configuration.",
        "1016" => "Account is invalid.",
        "1020" => "Invalid request.",
        "1025" => "Too many failed attempts; stop and fix the cause before retrying.",
        _ => "Flex Web Service request failed."
    };
}
