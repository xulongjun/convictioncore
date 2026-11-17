using System.Globalization;
using ConvictionCore.Application.Ibkr;
using ConvictionCore.Domain.SharedKernel;
using ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;
using ConvictionCore.Infrastructure.Persistence;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;
using Microsoft.EntityFrameworkCore;

namespace ConvictionCore.Infrastructure.Ibkr.Sync;

/// <summary>
/// Natural-key upsert of one parsed statement. New key: insert. Same key and row hash:
/// unchanged, nothing written. Same key, different hash: overwrite with the latest values,
/// version + 1. Rows absent from the payload are never deleted.
/// </summary>
internal sealed class IbkrFactUpserter(ConvictionCoreDbContext db)
{
    public const string ExecutionTable = "ibkr_trade_execution";
    public const string ClosedLotTable = "ibkr_closed_lot";
    public const string OptionEaeTable = "ibkr_option_eae";
    public const string OpenPositionLotTable = "ibkr_open_position_lot";
    public const string SecurityTable = "ibkr_security";
    public const string CorporateActionTable = "ibkr_corporate_action";
    public const string AccountInfoTable = "ibkr_account_info";

    public static IReadOnlyList<string> Tables { get; } =
    [
        ExecutionTable,
        ClosedLotTable,
        OptionEaeTable,
        OpenPositionLotTable,
        SecurityTable,
        CorporateActionTable,
        AccountInfoTable
    ];

    /// <summary>Stages all changes on the context; the caller saves them in one transaction.</summary>
    public async Task<Dictionary<string, IbkrFlexEntityCounts>> StageAsync(
        FlexActivityStatement statement,
        BrokerAccountId account,
        Guid rawStatementId,
        DateTimeOffset now,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var stamp = new Stamp(rawStatementId, now);
        var counts = new Dictionary<string, IbkrFlexEntityCounts>(StringComparer.Ordinal);

        foreach (var row in statement.Executions)
        {
            row.BrokerAccountId = account;
        }

        var transactionIds = statement.Executions.Select(row => row.TransactionId).ToList();
        counts[ExecutionTable] = await UpsertAsync(
            db.IbkrTradeExecutions.Where(row => row.BrokerAccountId == account && transactionIds.Contains(row.TransactionId)),
            statement.Executions,
            row => row.TransactionId.ToString(CultureInfo.InvariantCulture),
            stamp,
            cancellationToken);

        foreach (var row in statement.ClosedLots)
        {
            row.BrokerAccountId = account;
        }

        var closeIds = statement.ClosedLots.Select(row => row.CloseTransactionId).Distinct().ToList();
        var existingLots = await db.IbkrClosedLots
            .Where(row => row.BrokerAccountId == account && closeIds.Contains(row.CloseTransactionId))
            .ToListAsync(cancellationToken);
        WarnAboutMissingLots(statement.ClosedLots, existingLots, warnings);
        counts[ClosedLotTable] = Upsert(
            existingLots,
            statement.ClosedLots,
            row => $"{row.CloseTransactionId}/{row.OpenTransactionId}",
            stamp);

        foreach (var row in statement.OptionEaes)
        {
            row.BrokerAccountId = account;
        }

        var tradeIds = statement.OptionEaes.Select(row => row.TradeId).Distinct().ToList();
        counts[OptionEaeTable] = await UpsertAsync(
            db.IbkrOptionEaes.Where(row => row.BrokerAccountId == account && tradeIds.Contains(row.TradeId)),
            statement.OptionEaes,
            row => $"{row.TradeId}/{row.Conid}/{row.TransactionType}",
            stamp,
            cancellationToken);

        foreach (var row in statement.OpenPositionLots)
        {
            row.BrokerAccountId = account;
        }

        var reportDates = statement.OpenPositionLots.Select(row => row.ReportDate).Distinct().ToList();
        counts[OpenPositionLotTable] = await UpsertAsync(
            db.IbkrOpenPositionLots.Where(row => row.BrokerAccountId == account && reportDates.Contains(row.ReportDate)),
            statement.OpenPositionLots,
            row => $"{row.ReportDate:yyyyMMdd}/{row.Conid}/{row.OriginatingTransactionId}",
            stamp,
            cancellationToken);

        var conids = statement.Securities.Select(row => row.Conid).ToList();
        counts[SecurityTable] = await UpsertAsync(
            db.IbkrSecurities.Where(row => conids.Contains(row.Conid)),
            statement.Securities,
            row => row.Conid.ToString(CultureInfo.InvariantCulture),
            stamp,
            cancellationToken);

        foreach (var row in statement.CorporateActions)
        {
            row.BrokerAccountId = account;
        }

        counts[CorporateActionTable] = await UpsertAsync(
            db.IbkrCorporateActions.Where(row => row.BrokerAccountId == account),
            statement.CorporateActions,
            FlexActivityParser.CorporateActionKey,
            stamp,
            cancellationToken);

        List<IbkrAccountInfo> accountInfo = [];
        if (statement.AccountInfo is { } info)
        {
            info.BrokerAccountId = account;
            accountInfo.Add(info);
        }

        counts[AccountInfoTable] = await UpsertAsync(
            db.IbkrAccountInfos.Where(row => row.BrokerAccountId == account),
            accountInfo,
            _ => "account",
            stamp,
            cancellationToken);

        return counts;
    }

    private async Task<IbkrFlexEntityCounts> UpsertAsync<T>(
        IQueryable<T> existingQuery,
        IReadOnlyList<T> incoming,
        Func<T, string> key,
        Stamp stamp,
        CancellationToken cancellationToken)
        where T : IbkrFactRow
    {
        if (incoming.Count == 0)
        {
            return new IbkrFlexEntityCounts(0, 0, 0);
        }

        var existing = await existingQuery.ToListAsync(cancellationToken);
        return Upsert(existing, incoming, key, stamp);
    }

    private IbkrFlexEntityCounts Upsert<T>(IReadOnlyList<T> existing, IReadOnlyList<T> incoming, Func<T, string> key, Stamp stamp)
        where T : IbkrFactRow
    {
        var byKey = existing.ToDictionary(key, StringComparer.Ordinal);
        int inserted = 0, corrected = 0, unchanged = 0;
        foreach (var row in incoming)
        {
            row.RowHash = IbkrRowHasher.Compute(row);
            if (!byKey.TryGetValue(key(row), out var current))
            {
                row.Id = Guid.CreateVersion7();
                row.FirstRawStatementId = stamp.RawStatementId;
                row.LastChangedRawStatementId = stamp.RawStatementId;
                row.CreatedAt = stamp.Now;
                row.UpdatedAt = stamp.Now;
                row.Version = 0;
                db.Add(row);
                inserted++;
            }
            else if (string.Equals(current.RowHash, row.RowHash, StringComparison.Ordinal))
            {
                unchanged++;
            }
            else
            {
                row.Id = current.Id;
                row.FirstRawStatementId = current.FirstRawStatementId;
                row.LastChangedRawStatementId = stamp.RawStatementId;
                row.CreatedAt = current.CreatedAt;
                row.UpdatedAt = stamp.Now;
                row.Version = current.Version + 1;
                db.Entry(current).CurrentValues.SetValues(row);
                corrected++;
            }
        }

        return new IbkrFlexEntityCounts(inserted, corrected, unchanged);
    }

    private static void WarnAboutMissingLots(List<IbkrClosedLot> incoming, List<IbkrClosedLot> existing, List<string> warnings)
    {
        var incomingKeys = incoming.Select(row => (row.CloseTransactionId, row.OpenTransactionId)).ToHashSet();
        foreach (var missing in existing.Where(row => !incomingKeys.Contains((row.CloseTransactionId, row.OpenTransactionId))))
        {
            warnings.Add(
                $"Closed lot {missing.CloseTransactionId}/{missing.OpenTransactionId} is stored but missing from this statement; kept.");
        }
    }

    private readonly record struct Stamp(Guid RawStatementId, DateTimeOffset Now);
}
