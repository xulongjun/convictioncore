namespace ConvictionCore.Infrastructure.Ibkr;

public sealed class IbkrOptions
{
    public const string SectionName = "Ibkr";

    public DateOnly HistoryStartDate { get; set; } = new(2026, 3, 1);

    /// <summary>Flex Web Service token. Comes from user-secrets / environment only.</summary>
    public string? FlexToken { get; set; }

    public FlexWebServiceOptions FlexWebService { get; set; } = new();

    public FlexQueriesOptions Queries { get; set; } = new();
}

public sealed class FlexWebServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public int Version { get; set; } = 3;

    public string UserAgent { get; set; } = string.Empty;

    public int MaxWindowDays { get; set; } = 365;

    public int MinRequestIntervalMs { get; set; } = 1100;

    public int MaxRequestsPerMinute { get; set; } = 10;

    public int PollInitialDelaySeconds { get; set; } = 5;

    public int PollMaxDelaySeconds { get; set; } = 60;

    public int PollTimeoutSeconds { get; set; } = 600;
}

public sealed class FlexQueriesOptions
{
    public FlexQueryOptions Activity { get; set; } = new();

    public FlexQueryOptions TradeConfirmation { get; set; } = new();
}

public sealed class FlexQueryOptions
{
    public string QueryId { get; set; } = string.Empty;

    public bool Enabled { get; set; }
}
