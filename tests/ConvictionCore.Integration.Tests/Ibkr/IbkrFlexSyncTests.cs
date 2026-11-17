using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConvictionCore.Application.Ibkr;
using ConvictionCore.Domain.Trading;
using ConvictionCore.Infrastructure.Ibkr.Flex;
using ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;
using ConvictionCore.Tests.Shared;
using Microsoft.EntityFrameworkCore;

namespace ConvictionCore.Integration.Tests.Ibkr;

[Collection(SqlServerCollection.Name)]
public sealed class IbkrFlexSyncTests(SqlServerFixture sql)
{
    private const long CorrectedTransactionId = 6206381238;

    private static readonly byte[] V3 = FlexFixtures.Read(FlexFixtures.ActivityV3);
    private static readonly byte[] Synthetic = FlexFixtures.Read(FlexFixtures.Synthetic);

    [Fact]
    [Trait("AC", "1")]
    public async Task Full_backfill_stores_every_fact_and_one_raw_statement()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);

        var result = await host.SyncFixtureAsync(V3, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);

        Assert.Equal(ImportBatchStatus.Succeeded, result.Status);
        var rows = await host.CountRowsAsync();
        Assert.Equal(138, rows["ibkr_trade_execution"]);
        Assert.Equal(76, rows["ibkr_closed_lot"]);
        Assert.Equal(4, rows["ibkr_open_position_lot"]);
        Assert.Equal(11, rows["ibkr_option_eae"]);
        Assert.Equal(40, rows["ibkr_security"]);
        Assert.Equal(0, rows["ibkr_corporate_action"]);
        Assert.Equal(1, rows["ibkr_account_info"]);
        Assert.Equal(1, rows["ibkr_raw_statement"]);

        await using var db = host.Db();
        var raw = await db.IbkrRawStatements.SingleAsync();
        Assert.Equal(FlexPayloadCodec.Sha256Hex(V3), raw.Sha256);
        Assert.Equal(V3.Length, raw.ByteLength);
        Assert.Equal("gzip", raw.PayloadEncoding);
        Assert.Equal(V3, FlexPayloadCodec.Decompress(raw.Payload));
        Assert.Equal(new DateTimeOffset(2026, 11, 17, 17, 0, 0, TimeSpan.Zero), raw.WhenGeneratedUtc);

        var batch = await db.ImportBatches.SingleAsync();
        Assert.Equal(ImportBatchStatus.Succeeded, batch.Status);
        Assert.Equal(138 + 76 + 4 + 11 + 40 + 1, batch.InsertedCount);
        Assert.Equal(0, batch.CorrectedCount);
        Assert.Equal(0, batch.UnchangedCount);
        Assert.Equal(138, result.Counts["ibkr_trade_execution"].Inserted);
        Assert.Equal(76, result.Counts["ibkr_closed_lot"].Inserted);
        using var counts = JsonDocument.Parse(batch.CountsJson!);
        Assert.Equal(138, counts.RootElement.GetProperty("ibkr_trade_execution").GetProperty("inserted").GetInt32());
    }

    [Fact]
    [Trait("AC", "2")]
    public async Task Rerunning_the_same_window_inserts_and_corrects_nothing()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        await host.SyncFixtureAsync(V3, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);
        var before = await host.CountRowsAsync();

        var rerun = await host.SyncFixtureAsync(V3, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);

        Assert.Equal(ImportBatchStatus.Succeeded, rerun.Status);
        Assert.Equal(0, rerun.InsertedCount);
        Assert.Equal(0, rerun.CorrectedCount);
        Assert.Equal(270, rerun.UnchangedCount);
        Assert.All(rerun.Counts.Values, count => Assert.Equal((0, 0), (count.Inserted, count.Corrected)));
        var after = await host.CountRowsAsync();
        foreach (var table in IbkrSyncHost.FactTables)
        {
            Assert.Equal(before[table], after[table]);
        }

        Assert.Equal(before["ibkr_raw_statement"] + 1, after["ibkr_raw_statement"]);
        Assert.Equal(before["import_batch"] + 1, after["import_batch"]);
        await using var db = host.Db();
        Assert.All(await db.ImportBatches.ToListAsync(), batch => Assert.Equal(0, batch.CorrectedCount));
        Assert.All(await db.IbkrTradeExecutions.ToListAsync(), execution => Assert.Equal(0, execution.Version));
    }

    [Fact]
    [Trait("AC", "3")]
    public async Task Stored_fifo_totals_and_lot_links_line_up()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        await host.SyncFixtureAsync(V3, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);

        await using var db = host.Db();
        Assert.Equal(8727.98938831m, await db.IbkrClosedLots.SumAsync(lot => lot.FifoPnlRealized ?? 0m));
        Assert.Equal(8727.98938831m, await db.IbkrTradeExecutions.SumAsync(execution => execution.FifoPnlRealized ?? 0m));
        var transactionIds = (await db.IbkrTradeExecutions.Select(execution => execution.TransactionId).ToListAsync()).ToHashSet();
        var lots = await db.IbkrClosedLots.ToListAsync();
        Assert.Equal(76, lots.Count);
        Assert.All(lots, lot =>
        {
            Assert.Contains(lot.CloseTransactionId, transactionIds);
            Assert.Contains(lot.OpenTransactionId, transactionIds);
        });
    }

    [Fact]
    [Trait("AC", "4")]
    public async Task Corrected_execution_is_overwritten_and_old_value_stays_in_the_first_raw()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        await host.SyncFixtureAsync(V3, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);
        var corrected = FlexFixtures.WithExecutionAttribute(V3, CorrectedTransactionId, "ibCommission", "-2.5");

        var result = await host.SyncFixtureAsync(corrected, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);

        Assert.Equal(1, result.CorrectedCount);
        Assert.Equal(0, result.InsertedCount);
        Assert.Equal(269, result.UnchangedCount);
        Assert.Equal(1, result.Counts["ibkr_trade_execution"].Corrected);

        await using var db = host.Db();
        var raws = await db.IbkrRawStatements.OrderBy(raw => raw.FetchedAt).ThenBy(raw => raw.Id).ToListAsync();
        Assert.Equal(2, raws.Count);
        var row = await db.IbkrTradeExecutions.SingleAsync(execution => execution.TransactionId == CorrectedTransactionId);
        Assert.Equal(-2.5m, row.IbCommission);
        Assert.Equal(1, row.Version);
        Assert.Equal(raws[1].Id, row.LastChangedRawStatementId);
        Assert.Equal(raws[0].Id, row.FirstRawStatementId);
        Assert.Equal(1, await db.ImportBatches.CountAsync(batch => batch.CorrectedCount == 1));

        var original = FlexActivityParser.Parse(FlexPayloadCodec.Decompress(raws[0].Payload))
            .Executions.Single(execution => execution.TransactionId == CorrectedTransactionId);
        Assert.NotEqual(-2.5m, original.IbCommission);
        Assert.NotNull(original.IbCommission);
    }

    [Fact]
    [Trait("AC", "5")]
    public async Task Incremental_run_inserts_only_the_new_rows()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        var first = await host.SyncFixtureAsync(FlexFixtures.WithFirstExecutions(V3, 50), FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);
        Assert.Equal(50, first.Counts["ibkr_trade_execution"].Inserted);
        var firstLots = first.Counts["ibkr_closed_lot"].Inserted;

        var second = await host.SyncFixtureAsync(V3, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);

        Assert.Equal(new IbkrFlexEntityCounts(88, 0, 50), second.Counts["ibkr_trade_execution"]);
        Assert.Equal(new IbkrFlexEntityCounts(76 - firstLots, 0, firstLots), second.Counts["ibkr_closed_lot"]);
        Assert.Equal(0, second.CorrectedCount);
        var rows = await host.CountRowsAsync();
        Assert.Equal(138, rows["ibkr_trade_execution"]);
        Assert.Equal(76, rows["ibkr_closed_lot"]);
    }

    [Fact]
    [Trait("AC", "6")]
    public async Task Generation_in_progress_is_polled_until_success()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        host.Handler.EnqueueGetStatement(FakeFlexHandler.Error("1019")).EnqueueGetStatement(FakeFlexHandler.Error("1019"));

        var result = await host.SyncFixtureAsync(Synthetic, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        Assert.Equal(ImportBatchStatus.Succeeded, result.Status);
        Assert.Equal(3, host.Handler.CallsTo("GetStatement").Count());
    }

    [Theory]
    [Trait("AC", "6")]
    [InlineData("1015")]
    [InlineData("1012")]
    public async Task Token_errors_fail_the_batch_without_retry(string code)
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        host.Handler.EnqueueSendRequest(FakeFlexHandler.Error(code));

        var result = await host.SyncFixtureAsync(Synthetic, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        Assert.Equal(ImportBatchStatus.Failed, result.Status);
        Assert.Equal(code, result.ErrorCode);
        Assert.Single(host.Handler.Calls);
        await using var db = host.Db();
        var batch = await db.ImportBatches.SingleAsync();
        Assert.Equal(code, batch.ErrorCode);
        Assert.NotNull(batch.FinishedAt);
        Assert.Equal(0, await db.IbkrRawStatements.CountAsync());
    }

    [Fact]
    [Trait("AC", "8")]
    public async Task Window_longer_than_365_days_is_fetched_as_two_ordered_windows()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql, now: new DateTimeOffset(2027, 7, 1, 12, 0, 0, TimeSpan.Zero));
        host.Handler.DefaultGetStatement = _ =>
        {
            var send = host.Handler.CallsTo("SendRequest").Last();
            return FakeFlexHandler.Bytes(FlexFixtures.WithWindow(
                Synthetic,
                DateOnly.ParseExact(send.Query("fd")!, "yyyyMMdd"),
                DateOnly.ParseExact(send.Query("td")!, "yyyyMMdd")));
        };

        var result = await host.SyncAsync(new DateOnly(2026, 3, 1), new DateOnly(2027, 6, 30));

        Assert.Equal(ImportBatchStatus.Succeeded, result.Status);
        var windows = host.Handler.CallsTo("SendRequest").Select(call => (call.Query("fd"), call.Query("td"))).ToList();
        Assert.Equal([("20260301", "20270228"), ("20270301", "20270630")], windows);
        Assert.Equal(2, (await host.CountRowsAsync())["ibkr_raw_statement"]);
    }

    public static TheoryData<string> EnvelopeMismatches => ["account", "type", "window"];

    [Theory]
    [Trait("AC", "9")]
    [MemberData(nameof(EnvelopeMismatches))]
    public async Task Envelope_mismatch_fails_after_storing_the_raw_statement(string mismatch)
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        var (payload, to) = mismatch switch
        {
            "account" => (FlexFixtures.WithAccountId(Synthetic, "U0000002"), FlexFixtures.SyntheticTo),
            "type" => (FlexFixtures.WithStatementType(Synthetic, "TCF"), FlexFixtures.SyntheticTo),
            _ => (Synthetic, FlexFixtures.SyntheticTo.AddDays(-1))
        };

        var result = await host.SyncFixtureAsync(payload, FlexFixtures.SyntheticFrom, to);

        Assert.Equal(ImportBatchStatus.Failed, result.Status);
        Assert.Equal(FlexErrorCodes.EnvelopeMismatch, result.ErrorCode);
        Assert.Equal(1, (await host.CountRowsAsync())["ibkr_raw_statement"]);
        Assert.Equal(0, await host.FactRowCountAsync());
        Assert.DoesNotContain("U0000002", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "10")]
    public async Task Parse_failure_writes_no_facts_but_keeps_the_raw_statement()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);

        var result = await host.SyncFixtureAsync(FlexFixtures.WithOrphanLot(Synthetic), FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        Assert.Equal(ImportBatchStatus.Failed, result.Status);
        Assert.Equal(FlexErrorCodes.ParseError, result.ErrorCode);
        Assert.Equal(1, (await host.CountRowsAsync())["ibkr_raw_statement"]);
        Assert.Equal(0, await host.FactRowCountAsync());
    }

    [Fact]
    [Trait("AC", "10")]
    public async Task Database_failure_mid_upsert_rolls_back_every_section()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        var tooLong = new string('x', 600);
        var payload = FlexFixtures.WithExecutionAttribute(V3, CorrectedTransactionId, "description", tooLong);

        var result = await host.SyncFixtureAsync(payload, FlexFixtures.ActivityV3From, FlexFixtures.ActivityV3To);

        Assert.Equal(ImportBatchStatus.Failed, result.Status);
        Assert.Equal(FlexErrorCodes.Unexpected, result.ErrorCode);
        Assert.Equal(1, (await host.CountRowsAsync())["ibkr_raw_statement"]);
        Assert.Equal(0, await host.FactRowCountAsync());
    }

    [Fact]
    [Trait("AC", "11")]
    public async Task Missing_token_returns_a_clear_error_and_sends_nothing()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql, overrides: new() { ["Ibkr:FlexToken"] = string.Empty });
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ibkr/flex/sync", new { fromDate = "2026-03-01", toDate = "2026-11-16" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Ibkr:FlexToken missing; set via dotnet user-secrets", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Empty(host.Handler.Calls);
        Assert.Equal(0, (await host.CountRowsAsync())["import_batch"]);
    }

    [Fact]
    [Trait("AC", "11")]
    public async Task Token_never_reaches_logs_errors_or_the_database()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        await host.SyncFixtureAsync(Synthetic, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);
        host.Handler.EnqueueSendRequest(_ => FakeFlexHandler.Xml("busy", HttpStatusCode.BadGateway));
        host.Handler.EnqueueSendRequest(FakeFlexHandler.Error("1015"));

        var failed = await host.SyncAsync(FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        Assert.Equal("1015", failed.ErrorCode);
        Assert.Contains(host.Handler.Calls, call => call.Uri.Query.Contains(FlexTestConfiguration.SentinelToken, StringComparison.Ordinal));
        Assert.DoesNotContain(FlexTestConfiguration.SentinelToken, failed.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(FlexTestConfiguration.SentinelToken, await host.DumpTradingTablesAsync(), StringComparison.Ordinal);
        Assert.Contains(host.Logs.Messages, message => message.Contains("t=***", StringComparison.Ordinal));
        Assert.All(host.Logs.Messages, message => Assert.DoesNotContain(FlexTestConfiguration.SentinelToken, message, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AC", "11")]
    public async Task Query_id_comes_from_configuration()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql, overrides: new() { ["Ibkr:Queries:Activity:QueryId"] = "4242" });

        await host.SyncFixtureAsync(Synthetic, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        Assert.Equal("4242", Assert.Single(host.Handler.CallsTo("SendRequest")).Query("q"));
        await using var db = host.Db();
        Assert.Equal("4242", (await db.IbkrRawStatements.SingleAsync()).QueryId);
    }

    [Fact]
    [Trait("AC", "12")]
    public async Task Pii_in_account_information_is_never_stored_in_facts()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        var payload = FlexFixtures.WithAccountInformationAttributes(
            Synthetic,
            "primaryEmail=\"someone@example.invalid\" dateOfBirth=\"19700101\" accountRepName=\"Synthetic Rep\"");

        var result = await host.SyncFixtureAsync(payload, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        Assert.Equal(ImportBatchStatus.Succeeded, result.Status);
        Assert.Equal(1, (await host.CountRowsAsync())["ibkr_account_info"]);
        foreach (var table in IbkrSyncHost.FactTables)
        {
            var json = await host.ScalarAsync($"SELECT (SELECT * FROM trading.{table} FOR JSON PATH)") as string ?? string.Empty;
            Assert.DoesNotContain("example.invalid", json, StringComparison.Ordinal);
            Assert.DoesNotContain("19700101", json, StringComparison.Ordinal);
            Assert.DoesNotContain("1970-01-01", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Synthetic Rep", json, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("AC", "13")]
    public async Task Timestamps_are_stored_as_utc_with_the_original_eastern_string()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        await host.SyncFixtureAsync(Synthetic, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        await using var db = host.Db();
        var summer = await db.IbkrTradeExecutions.SingleAsync(execution => execution.TransactionId == 900000301);
        Assert.Equal(new DateTimeOffset(2026, 3, 9, 13, 30, 0, TimeSpan.Zero), summer.DateTimeUtc);
        Assert.Equal(TimeSpan.Zero, summer.DateTimeUtc!.Value.Offset);
        Assert.Equal("20260309;093000", summer.DateTimeIbkrEt);
        var winter = await db.IbkrTradeExecutions.SingleAsync(execution => execution.TransactionId == 900000302);
        Assert.Equal(new DateTimeOffset(2026, 11, 13, 15, 0, 0, TimeSpan.Zero), winter.DateTimeUtc);
        Assert.Equal("20261113;100000", winter.DateTimeIbkrEt);
    }

    [Fact]
    [Trait("AC", "14")]
    public async Task Corporate_action_is_inserted_once_then_unchanged()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);

        var first = await host.SyncFixtureAsync(Synthetic, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);
        var second = await host.SyncFixtureAsync(Synthetic, FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);

        Assert.Equal(new IbkrFlexEntityCounts(1, 0, 0), first.Counts["ibkr_corporate_action"]);
        Assert.Equal(new IbkrFlexEntityCounts(0, 0, 1), second.Counts["ibkr_corporate_action"]);
        await using var db = host.Db();
        var action = await db.IbkrCorporateActions.SingleAsync();
        Assert.Equal("FS", action.Type);
        Assert.Equal(900000401, action.TransactionId);
    }

    [Fact]
    [Trait("AC", "16")]
    public async Task Second_trigger_while_a_batch_is_running_returns_409_without_flex_calls()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        var accountId = await host.AccountIdAsync();
        await using (var db = host.Db())
        {
            db.ImportBatches.Add(ImportBatch.Start(
                ImportBatchId.New(),
                accountId,
                ImportSource.IbkrFlexActivity,
                ImportTrigger.Manual,
                FlexFixtures.SyntheticFrom,
                FlexFixtures.SyntheticTo,
                FlexActivityParser.ParserVersion,
                FlexTestConfiguration.Now));
            await db.SaveChangesAsync();
        }

        using var client = host.CreateClient();
        var response = await client.PostAsJsonAsync("/api/ibkr/flex/sync", new { fromDate = "2026-03-01", toDate = "2026-11-16" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(host.Handler.Calls);
    }

    [Fact]
    [Trait("AC", "16")]
    public async Task Database_allows_only_one_running_batch_per_account()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        var accountId = await host.AccountIdAsync();
        await using var db = host.Db();
        for (var index = 0; index < 2; index++)
        {
            db.ImportBatches.Add(ImportBatch.Start(
                ImportBatchId.New(),
                accountId,
                ImportSource.IbkrFlexActivity,
                ImportTrigger.Manual,
                FlexFixtures.SyntheticFrom,
                FlexFixtures.SyntheticTo,
                FlexActivityParser.ParserVersion,
                FlexTestConfiguration.Now));
        }

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    [Trait("AC", "17")]
    public async Task Endpoint_is_not_mapped_outside_development()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql, environment: "Production");
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ibkr/flex/sync", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(host.Handler.Calls);
    }

    [Fact]
    public async Task Endpoint_returns_batch_id_and_counts()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        host.Handler.ServeStatement(Synthetic);
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ibkr/flex/sync", new { fromDate = "2026-03-01", toDate = "2026-11-16" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Succeeded", body.RootElement.GetProperty("status").GetString());
        Assert.True(body.RootElement.GetProperty("batchId").TryGetGuid(out _));
        Assert.Equal(2, body.RootElement.GetProperty("counts").GetProperty("ibkr_trade_execution").GetProperty("inserted").GetInt32());
    }

    [Theory]
    [InlineData("2026-11-18", "2026-11-18")]
    [InlineData("2026-06-01", "2026-05-01")]
    [InlineData("17/11/2026", null)]
    public async Task Invalid_windows_are_rejected_before_any_flex_call(string fromDate, string? toDate)
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ibkr/flex/sync", new { fromDate, toDate });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(host.Handler.Calls);
    }

    [Fact]
    public async Task Default_window_runs_from_history_start_to_today()
    {
        await using var host = await IbkrSyncHost.StartAsync(sql);
        host.Handler.ServeStatement(FlexFixtures.WithWindow(Synthetic, new DateOnly(2026, 3, 1), new DateOnly(2026, 11, 17)));

        var result = await host.SyncAsync(null, null);

        Assert.Equal(ImportBatchStatus.Succeeded, result.Status);
        var send = Assert.Single(host.Handler.CallsTo("SendRequest"));
        Assert.Equal(("20260301", "20261117"), (send.Query("fd"), send.Query("td")));
    }
}
