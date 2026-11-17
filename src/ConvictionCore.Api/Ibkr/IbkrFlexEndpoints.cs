using System.Globalization;
using ConvictionCore.Application.Ibkr;
using ConvictionCore.Domain.Trading;

namespace ConvictionCore.Api.Ibkr;

internal static class IbkrFlexEndpoints
{
    public const string SyncRoute = "/api/ibkr/flex/sync";

    /// <summary>Manual Flex sync. Only mapped in Development; other environments return 404.</summary>
    public static IEndpointRouteBuilder MapIbkrFlexEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(SyncRoute, SyncAsync).WithName("IbkrFlexSync");
        return endpoints;
    }

    private static async Task<IResult> SyncAsync(
        FlexSyncBody? body,
        IIbkrFlexSync sync,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(body?.FromDate, out var fromDate) || !TryParseDate(body?.ToDate, out var toDate))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid sync window",
                detail: "fromDate and toDate must use yyyy-MM-dd.");
        }

        try
        {
            var result = await sync.SyncAsync(new IbkrFlexSyncRequest(fromDate, toDate, ImportTrigger.Manual), cancellationToken);
            var response = new
            {
                batchId = result.BatchId,
                status = result.Status.ToString(),
                fromDate = result.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                toDate = result.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                insertedCount = result.InsertedCount,
                correctedCount = result.CorrectedCount,
                unchangedCount = result.UnchangedCount,
                counts = result.Counts,
                warnings = result.Warnings,
                errorCode = result.ErrorCode,
                errorMessage = result.ErrorMessage
            };
            return result.Status == ImportBatchStatus.Succeeded
                ? Results.Ok(response)
                : Results.UnprocessableEntity(response);
        }
        catch (IbkrFlexSyncException exception)
        {
            return Results.Problem(
                statusCode: exception.Kind == IbkrFlexSyncErrorKind.Conflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest,
                title: exception.Kind switch
                {
                    IbkrFlexSyncErrorKind.Conflict => "Sync already running",
                    IbkrFlexSyncErrorKind.Configuration => "IBKR Flex configuration error",
                    _ => "Invalid sync request"
                },
                detail: exception.Message);
        }
    }

    private static bool TryParseDate(string? text, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            date = parsed;
            return true;
        }

        return false;
    }

    internal sealed record FlexSyncBody(string? FromDate, string? ToDate);
}
