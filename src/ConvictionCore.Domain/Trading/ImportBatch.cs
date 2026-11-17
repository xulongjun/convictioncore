using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Domain.Trading;

/// <summary>
/// One sync run against a broker source. A run may cover several fetch windows;
/// the raw payloads and their hashes live on the raw statement rows.
/// </summary>
public sealed class ImportBatch
{
    public const int ErrorMessageMaxLength = 2000;

    private ImportBatch()
    {
    }

    public ImportBatchId Id { get; private set; }

    public BrokerAccountId BrokerAccountId { get; private set; }

    public ImportSource Source { get; private set; }

    public ImportTrigger Trigger { get; private set; }

    public DateOnly RequestedFrom { get; private set; }

    public DateOnly RequestedTo { get; private set; }

    public ImportBatchStatus Status { get; private set; }

    public string ParserVersion { get; private set; } = string.Empty;

    public int InsertedCount { get; private set; }

    public int CorrectedCount { get; private set; }

    public int UnchangedCount { get; private set; }

    public string? CountsJson { get; private set; }

    public string? WarningsJson { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ImportBatch Start(
        ImportBatchId id,
        BrokerAccountId brokerAccountId,
        ImportSource source,
        ImportTrigger trigger,
        DateOnly requestedFrom,
        DateOnly requestedTo,
        string parserVersion,
        DateTimeOffset now)
    {
        if (id.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Import batch id is required.");
        }

        if (brokerAccountId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Broker account id is required.");
        }

        if (requestedFrom > requestedTo)
        {
            throw new InvalidOperationException("Requested window must not end before it starts.");
        }

        if (string.IsNullOrWhiteSpace(parserVersion))
        {
            throw new InvalidOperationException("Parser version is required.");
        }

        return new ImportBatch
        {
            Id = id,
            BrokerAccountId = brokerAccountId,
            Source = source,
            Trigger = trigger,
            RequestedFrom = requestedFrom,
            RequestedTo = requestedTo,
            Status = ImportBatchStatus.Running,
            ParserVersion = parserVersion.Trim(),
            StartedAt = now,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Succeed(
        int inserted,
        int corrected,
        int unchanged,
        string? countsJson,
        string? warningsJson,
        DateTimeOffset now)
    {
        RequireRunning();
        SetCounts(inserted, corrected, unchanged, countsJson, warningsJson);
        Status = ImportBatchStatus.Succeeded;
        Finish(now);
    }

    public void Fail(
        string errorCode,
        string errorMessage,
        int inserted,
        int corrected,
        int unchanged,
        string? countsJson,
        string? warningsJson,
        DateTimeOffset now)
    {
        RequireRunning();
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new InvalidOperationException("Error code is required.");
        }

        SetCounts(inserted, corrected, unchanged, countsJson, warningsJson);
        ErrorCode = errorCode.Trim();
        ErrorMessage = errorMessage.Length > ErrorMessageMaxLength
            ? errorMessage[..ErrorMessageMaxLength]
            : errorMessage;
        Status = ImportBatchStatus.Failed;
        Finish(now);
    }

    private void SetCounts(int inserted, int corrected, int unchanged, string? countsJson, string? warningsJson)
    {
        if (inserted < 0 || corrected < 0 || unchanged < 0)
        {
            throw new InvalidOperationException("Counts must not be negative.");
        }

        InsertedCount = inserted;
        CorrectedCount = corrected;
        UnchangedCount = unchanged;
        CountsJson = countsJson;
        WarningsJson = warningsJson;
    }

    private void RequireRunning()
    {
        if (Status != ImportBatchStatus.Running)
        {
            throw new InvalidOperationException($"Import batch is already {Status}.");
        }
    }

    private void Finish(DateTimeOffset now)
    {
        FinishedAt = now;
        UpdatedAt = now;
        Version++;
    }
}
