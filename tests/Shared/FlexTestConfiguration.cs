namespace ConvictionCore.Tests.Shared;

public static class FlexTestConfiguration
{
    /// <summary>Not a real token: tests search logs, errors and the DB for this exact string.</summary>
    public const string SentinelToken = "SENTINEL-FLEX-TOKEN-4f1c9b";

    public const string ActivityQueryId = "1693063";

    public static readonly DateTimeOffset Now = new(2026, 11, 17, 22, 0, 0, TimeSpan.Zero);

    public static Dictionary<string, string?> Settings(string? token = SentinelToken) => new()
    {
        ["Ibkr:HistoryStartDate"] = "2026-03-01",
        ["Ibkr:FlexToken"] = token,
        ["Ibkr:FlexWebService:BaseUrl"] = "https://flex.example.test/AccountManagement/FlexWebService",
        ["Ibkr:FlexWebService:Version"] = "3",
        ["Ibkr:FlexWebService:UserAgent"] = "ConvictionCore/0.1",
        ["Ibkr:FlexWebService:MaxWindowDays"] = "365",
        ["Ibkr:FlexWebService:MinRequestIntervalMs"] = "1100",
        ["Ibkr:FlexWebService:MaxRequestsPerMinute"] = "10",
        ["Ibkr:FlexWebService:PollInitialDelaySeconds"] = "5",
        ["Ibkr:FlexWebService:PollMaxDelaySeconds"] = "60",
        ["Ibkr:FlexWebService:PollTimeoutSeconds"] = "600",
        ["Ibkr:Queries:Activity:QueryId"] = ActivityQueryId,
        ["Ibkr:Queries:Activity:Enabled"] = "true",
        ["Ibkr:Queries:TradeConfirmation:QueryId"] = "1693065",
        ["Ibkr:Queries:TradeConfirmation:Enabled"] = "false"
    };
}
