using System.Text.Json;
using ConvictionCore.Application.Ibkr;
using ConvictionCore.Application.Portfolio;
using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using ConvictionCore.Domain.Trading;
using ConvictionCore.Infrastructure.Ibkr.Flex;
using ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;
using ConvictionCore.Infrastructure.Persistence;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConvictionCore.Infrastructure.Ibkr.Sync;

internal sealed partial class IbkrFlexSyncService(
    ConvictionCoreDbContext db,
    FlexWebServiceClient client,
    IOptions<IbkrOptions> options,
    TimeProvider time,
    ILogger<IbkrFlexSyncService> logger) : IIbkrFlexSync
{
    public const string TokenMissingMessage = "Ibkr:FlexToken missing; set via dotnet user-secrets";

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IbkrFlexSyncResult> SyncAsync(IbkrFlexSyncRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.FlexToken))
        {
            throw new IbkrFlexSyncException(IbkrFlexSyncErrorKind.Configuration, TokenMissingMessage);
        }

        var activity = settings.Queries.Activity;
        if (!activity.Enabled || string.IsNullOrWhiteSpace(activity.QueryId))
        {
            throw new IbkrFlexSyncException(
                IbkrFlexSyncErrorKind.Configuration,
                "Ibkr:Queries:Activity must be enabled and have a QueryId.");
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var from = request.FromDate ?? settings.HistoryStartDate;
        var to = request.ToDate ?? today;
        if (from > to)
        {
            throw new IbkrFlexSyncException(IbkrFlexSyncErrorKind.InvalidRequest, "fromDate must not be after toDate.");
        }

        if (to > today)
        {
            throw new IbkrFlexSyncException(IbkrFlexSyncErrorKind.InvalidRequest, $"toDate must not be after today ({today:yyyy-MM-dd}).");
        }

        var warnings = new List<string>();
        if (from < settings.HistoryStartDate)
        {
            warnings.Add($"fromDate {from:yyyy-MM-dd} is before Ibkr:HistoryStartDate {settings.HistoryStartDate:yyyy-MM-dd}.");
        }

        var account = await ResolveAccountAsync(cancellationToken);
        var accountNumber = account.ExternalReference!;
        var batch = await StartBatchAsync(account.Id, request.Trigger, from, to, cancellationToken);

        var counts = IbkrFactUpserter.Tables.ToDictionary(
            table => table,
            _ => new IbkrFlexEntityCounts(0, 0, 0),
            StringComparer.Ordinal);
        string? errorCode = null;
        string? errorMessage = null;
        FlexDateWindow? currentWindow = null;
        try
        {
            foreach (var window in FlexDateWindow.Split(from, to, settings.FlexWebService.MaxWindowDays))
            {
                currentWindow = window;
                var windowCounts = await SyncWindowAsync(batch.Id, account.Id, accountNumber, activity.QueryId, window, warnings, cancellationToken);
                foreach (var (table, value) in windowCounts)
                {
                    var total = counts[table];
                    counts[table] = new IbkrFlexEntityCounts(
                        total.Inserted + value.Inserted,
                        total.Corrected + value.Corrected,
                        total.Unchanged + value.Unchanged);
                }
            }
        }
        catch (FlexServiceException exception)
        {
            errorCode = exception.ErrorCode;
            errorMessage = $"Window {currentWindow}: {exception.Message}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            errorCode = FlexErrorCodes.Canceled;
            errorMessage = $"Window {currentWindow}: sync canceled.";
        }
        catch (Exception exception)
        {
            // A started batch must never stay Running; anything unexpected marks it Failed.
            LogUnexpectedFailure(logger, exception.GetType().Name);
            errorCode = FlexErrorCodes.Unexpected;
            errorMessage = $"Window {currentWindow}: {FlexRedaction.Redact(exception.Message, settings.FlexToken)}";
        }

        return await FinishBatchAsync(batch.Id, from, to, counts, warnings, errorCode, errorMessage);
    }

    private async Task<Dictionary<string, IbkrFlexEntityCounts>> SyncWindowAsync(
        ImportBatchId batchId,
        BrokerAccountId accountId,
        string accountNumber,
        string queryId,
        FlexDateWindow window,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var fetched = await client.FetchAsync(queryId, window, cancellationToken);

        FlexEnvelope? envelope = null;
        try
        {
            envelope = FlexEnvelope.Read(fetched.Payload);
        }
        catch (FlexParseException)
        {
            // Malformed payloads are still stored raw; the parse step reports the failure.
        }

        var rawStatement = new IbkrRawStatement
        {
            Id = Guid.CreateVersion7(),
            ImportBatchId = batchId,
            BrokerAccountId = accountId,
            QueryKind = IbkrFlexQueryKind.Activity,
            QueryId = queryId,
            FromDate = window.From,
            ToDate = window.To,
            ReferenceCode = fetched.ReferenceCode,
            WhenGeneratedUtc = FlexTimestamp.TryToUtc(envelope?.WhenGenerated, out var generated) ? generated : null,
            WhenGeneratedIbkrEt = envelope?.WhenGenerated,
            FetchedAt = fetched.FetchedAt,
            ByteLength = fetched.Payload.Length,
            Sha256 = FlexPayloadCodec.Sha256Hex(fetched.Payload),
            PayloadEncoding = FlexPayloadCodec.Encoding,
            Payload = FlexPayloadCodec.Compress(fetched.Payload)
        };
        db.IbkrRawStatements.Add(rawStatement);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        if (envelope is not null)
        {
            var problems = envelope.Validate(accountNumber, window);
            if (problems.Count > 0)
            {
                throw new FlexServiceException(FlexErrorCodes.EnvelopeMismatch, "Envelope check failed: " + string.Join("; ", problems) + ".");
            }
        }

        FlexActivityStatement statement;
        try
        {
            statement = FlexActivityParser.Parse(fetched.Payload);
        }
        catch (FlexParseException exception)
        {
            throw new FlexServiceException(FlexErrorCodes.ParseError, exception.Message);
        }

        warnings.AddRange(statement.Warnings);
        var upserter = new IbkrFactUpserter(db);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var counts = await upserter.StageAsync(statement, accountId, rawStatement.Id, time.GetUtcNow(), warnings, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return counts;
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    private async Task<BrokerAccount> ResolveAccountAsync(CancellationToken cancellationToken)
    {
        var ibkr = new BrokerId(BrokerSeedIds.IbkrId);
        var accounts = await db.BrokerAccounts
            .AsNoTracking()
            .Where(account => account.BrokerId == ibkr && account.Status == AccountStatus.Active)
            .Take(2)
            .ToListAsync(cancellationToken);
        return accounts switch
        {
            [] => throw new IbkrFlexSyncException(
                IbkrFlexSyncErrorKind.Configuration,
                "No active IBKR account is registered in portfolio.broker_account."),
            [var single] when string.IsNullOrWhiteSpace(single.ExternalReference) => throw new IbkrFlexSyncException(
                IbkrFlexSyncErrorKind.Configuration,
                "The registered IBKR account has no external_reference (IBKR account id)."),
            [var single] => single,
            _ => throw new IbkrFlexSyncException(
                IbkrFlexSyncErrorKind.Configuration,
                "More than one active IBKR account is registered; PBI-01 supports exactly one.")
        };
    }

    private async Task<ImportBatch> StartBatchAsync(
        BrokerAccountId accountId,
        ImportTrigger trigger,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (await db.ImportBatches.AnyAsync(
                batch => batch.BrokerAccountId == accountId && batch.Status == ImportBatchStatus.Running,
                cancellationToken))
        {
            throw RunningConflict();
        }

        var batch = ImportBatch.Start(
            ImportBatchId.New(),
            accountId,
            ImportSource.IbkrFlexActivity,
            trigger,
            from,
            to,
            FlexActivityParser.ParserVersion,
            time.GetUtcNow());
        db.ImportBatches.Add(batch);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.ChangeTracker.Clear();
            throw RunningConflict();
        }

        db.ChangeTracker.Clear();
        return batch;
    }

    private async Task<IbkrFlexSyncResult> FinishBatchAsync(
        ImportBatchId batchId,
        DateOnly from,
        DateOnly to,
        Dictionary<string, IbkrFlexEntityCounts> counts,
        List<string> warnings,
        string? errorCode,
        string? errorMessage)
    {
        // Always record the outcome, even when the caller's request was canceled.
        db.ChangeTracker.Clear();
        var batch = await db.ImportBatches.SingleAsync(row => row.Id == batchId, CancellationToken.None);
        var inserted = counts.Values.Sum(count => count.Inserted);
        var corrected = counts.Values.Sum(count => count.Corrected);
        var unchanged = counts.Values.Sum(count => count.Unchanged);
        var countsJson = JsonSerializer.Serialize(counts, Json);
        var warningsJson = warnings.Count == 0 ? null : JsonSerializer.Serialize(warnings, Json);
        var now = time.GetUtcNow();
        if (errorCode is null)
        {
            batch.Succeed(inserted, corrected, unchanged, countsJson, warningsJson, now);
        }
        else
        {
            batch.Fail(errorCode, errorMessage ?? errorCode, inserted, corrected, unchanged, countsJson, warningsJson, now);
            LogBatchFailed(logger, batchId.Value, errorCode);
        }

        await db.SaveChangesAsync(CancellationToken.None);
        return new IbkrFlexSyncResult(
            batch.Id.Value,
            batch.Status,
            from,
            to,
            inserted,
            corrected,
            unchanged,
            counts,
            warnings,
            batch.ErrorCode,
            batch.ErrorMessage);
    }

    private static IbkrFlexSyncException RunningConflict() =>
        new(IbkrFlexSyncErrorKind.Conflict, "An IBKR Flex sync is already running for this account.");

    [LoggerMessage(Level = LogLevel.Error, Message = "IBKR Flex sync failed unexpectedly ({ExceptionType})")]
    private static partial void LogUnexpectedFailure(ILogger logger, string exceptionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "IBKR Flex sync batch {BatchId} failed with {ErrorCode}")]
    private static partial void LogBatchFailed(ILogger logger, Guid batchId, string errorCode);
}
