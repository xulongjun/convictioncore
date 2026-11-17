using ConvictionCore.Domain.Trading;

namespace ConvictionCore.Application.Ibkr;

public interface IIbkrFlexSync
{
    /// <summary>
    /// Fetches the Activity Flex statement for the window, stores the raw payload and
    /// upserts the parsed facts. Throws <see cref="IbkrFlexSyncException"/> when the run
    /// cannot start; a run that started always returns a result (Succeeded or Failed).
    /// </summary>
    Task<IbkrFlexSyncResult> SyncAsync(IbkrFlexSyncRequest request, CancellationToken cancellationToken);
}

public sealed record IbkrFlexSyncRequest(DateOnly? FromDate, DateOnly? ToDate, ImportTrigger Trigger);

public sealed record IbkrFlexSyncResult(
    Guid BatchId,
    ImportBatchStatus Status,
    DateOnly FromDate,
    DateOnly ToDate,
    int InsertedCount,
    int CorrectedCount,
    int UnchangedCount,
    IReadOnlyDictionary<string, IbkrFlexEntityCounts> Counts,
    IReadOnlyList<string> Warnings,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record IbkrFlexEntityCounts(int Inserted, int Corrected, int Unchanged);

public enum IbkrFlexSyncErrorKind
{
    InvalidRequest = 0,
    Configuration = 1,
    Conflict = 2
}

public sealed class IbkrFlexSyncException(IbkrFlexSyncErrorKind kind, string message) : Exception(message)
{
    public IbkrFlexSyncErrorKind Kind { get; } = kind;
}
