using System.Reflection;
using ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;
using ConvictionCore.Tests.Shared;

namespace ConvictionCore.Infrastructure.Tests;

public class FlexActivityParserTests
{
    [Fact]
    [Trait("AC", "1")]
    public void V3_fixture_parses_expected_row_counts()
    {
        var statement = FlexActivityParser.Parse(FlexFixtures.Read(FlexFixtures.ActivityV3));

        Assert.Equal(138, statement.Executions.Count);
        Assert.Equal(76, statement.ClosedLots.Count);
        Assert.Equal(4, statement.OpenPositionLots.Count);
        Assert.Equal(11, statement.OptionEaes.Count);
        Assert.Equal(40, statement.Securities.Count);
        Assert.Empty(statement.CorporateActions);
        Assert.NotNull(statement.AccountInfo);
    }

    [Fact]
    [Trait("AC", "3")]
    public void Scaled_fifo_totals_match_for_lots_and_executions()
    {
        var statement = FlexActivityParser.Parse(FlexFixtures.Read(FlexFixtures.ActivityV3));

        Assert.Equal(8727.98938831m, statement.ClosedLots.Sum(lot => lot.FifoPnlRealized ?? 0m));
        Assert.Equal(8727.98938831m, statement.Executions.Sum(execution => execution.FifoPnlRealized ?? 0m));
    }

    [Fact]
    public void Each_closed_lot_belongs_to_the_trade_right_before_it()
    {
        var payload = FlexFixtures.Read(FlexFixtures.Synthetic);

        var statement = FlexActivityParser.Parse(payload);

        var lot = Assert.Single(statement.ClosedLots);
        Assert.Equal(900000302, lot.CloseTransactionId);
        Assert.Equal(900000301, lot.OpenTransactionId);
    }

    [Fact]
    public void V3_closed_lots_sum_to_their_parent_execution_pnl()
    {
        var statement = FlexActivityParser.Parse(FlexFixtures.Read(FlexFixtures.ActivityV3));
        var executions = statement.Executions.ToDictionary(execution => execution.TransactionId);

        foreach (var group in statement.ClosedLots.GroupBy(lot => lot.CloseTransactionId))
        {
            Assert.True(executions.TryGetValue(group.Key, out var parent), $"parent {group.Key} missing");
            Assert.Equal(parent.FifoPnlRealized, group.Sum(lot => lot.FifoPnlRealized ?? 0m));
        }
    }

    [Fact]
    [Trait("AC", "13")]
    public void Eastern_timestamps_convert_to_utc_across_daylight_saving_and_keep_the_original_string()
    {
        var statement = FlexActivityParser.Parse(FlexFixtures.Read(FlexFixtures.Synthetic));

        var summer = statement.Executions.Single(execution => execution.TransactionId == 900000301);
        Assert.Equal(new DateTimeOffset(2026, 3, 9, 13, 30, 0, TimeSpan.Zero), summer.DateTimeUtc);
        Assert.Equal(TimeSpan.Zero, summer.DateTimeUtc!.Value.Offset);
        Assert.Equal("20260309;093000", summer.DateTimeIbkrEt);

        var winter = statement.Executions.Single(execution => execution.TransactionId == 900000302);
        Assert.Equal(new DateTimeOffset(2026, 11, 13, 15, 0, 0, TimeSpan.Zero), winter.DateTimeUtc);
        Assert.Equal("20261113;100000", winter.DateTimeIbkrEt);
        Assert.Equal(new DateOnly(2026, 11, 13), winter.TradeDate);
    }

    [Fact]
    public void Empty_attributes_read_as_null_and_eae_keeps_ibkr_spelling()
    {
        var statement = FlexActivityParser.Parse(FlexFixtures.Read(FlexFixtures.ActivityV3));

        var bookTrade = statement.Executions.First(execution => execution.TransactionType == "BookTrade");
        Assert.Null(bookTrade.IbExecId);
        var stock = statement.Executions.First(execution => execution.AssetCategory == "STK");
        Assert.Null(stock.Strike);
        Assert.Null(stock.PutCall);

        Assert.All(statement.OptionEaes, eae => Assert.NotNull(eae.CommisionsAndTax));
        Assert.Equal(9, statement.OptionEaes.Count(eae => eae.TransactionType == "Expiration"));
    }

    [Fact]
    [Trait("AC", "12")]
    public void Account_information_keeps_only_whitelisted_fields_even_when_pii_is_present()
    {
        var payload = FlexFixtures.WithAccountInformationAttributes(
            FlexFixtures.Read(FlexFixtures.Synthetic),
            "primaryEmail=\"someone@example.invalid\" dateOfBirth=\"19700101\" name=\"Synthetic Person\"");

        var info = FlexActivityParser.Parse(payload).AccountInfo;

        Assert.NotNull(info);
        Assert.Equal("USD", info.Currency);
        Assert.Equal(new DateOnly(2026, 3, 2), info.DateOpened);
        var columns = typeof(IbkrAccountInfo)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(["AccountType", "Currency", "CustomerType", "DateFunded", "DateOpened"], columns);
    }

    [Fact]
    [Trait("AC", "10")]
    public void Lot_without_a_parent_trade_is_rejected()
    {
        var payload = FlexFixtures.WithOrphanLot(FlexFixtures.Read(FlexFixtures.Synthetic));

        var exception = Assert.Throws<FlexParseException>(() => FlexActivityParser.Parse(payload));

        Assert.Contains("no preceding Trade", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_natural_keys_in_one_payload_are_rejected()
    {
        var text = FlexFixtures.ReadText(FlexFixtures.Synthetic);
        var trade = text.Split('\n').First(line => line.StartsWith("<Trade ", StringComparison.Ordinal));
        var payload = FlexFixtures.Bytes(text.Replace(trade, trade + "\n" + trade, StringComparison.Ordinal));

        Assert.Throws<FlexParseException>(() => FlexActivityParser.Parse(payload));
    }

    [Fact]
    public void Unknown_sections_and_detail_levels_are_ignored_with_warnings()
    {
        var text = FlexFixtures.InsertAfterTradesOpen(
            FlexFixtures.ReadText(FlexFixtures.Synthetic),
            "<Trade transactionID=\"1\" conid=\"1\" levelOfDetail=\"ORDER\" />");
        text = text.Replace(
            "</FlexStatement>",
            "<CashTransactions><CashTransaction amount=\"1\" /></CashTransactions>\n</FlexStatement>",
            StringComparison.Ordinal);

        var statement = FlexActivityParser.Parse(FlexFixtures.Bytes(text));

        Assert.Equal(2, statement.Executions.Count);
        Assert.Contains(statement.Warnings, warning => warning.Contains("levelOfDetail=ORDER", StringComparison.Ordinal));
        Assert.Contains(statement.Warnings, warning => warning.Contains("CashTransactions", StringComparison.Ordinal));
    }

    [Fact]
    public void Open_position_summary_mismatch_is_a_warning()
    {
        var text = FlexFixtures.ReadText(FlexFixtures.ActivityV3)
            .Replace("position=\"100\" markPrice", "position=\"101\" markPrice", StringComparison.Ordinal);

        var statement = FlexActivityParser.Parse(FlexFixtures.Bytes(text));

        Assert.Equal(4, statement.OpenPositionLots.Count);
        Assert.Contains(statement.Warnings, warning => warning.Contains("summary position", StringComparison.Ordinal));
    }

    [Fact]
    public void V3_lots_and_summaries_reconcile_without_warnings()
    {
        var statement = FlexActivityParser.Parse(FlexFixtures.Read(FlexFixtures.ActivityV3));

        Assert.Empty(statement.Warnings);
    }

    [Fact]
    [Trait("AC", "9")]
    public void Envelope_reports_type_account_and_window_mismatches()
    {
        var window = new Ibkr.Flex.FlexDateWindow(FlexFixtures.SyntheticFrom, FlexFixtures.SyntheticTo);
        var valid = FlexEnvelope.Read(FlexFixtures.Read(FlexFixtures.Synthetic));
        Assert.Empty(valid.Validate(FlexFixtures.AccountId, window));

        var tcf = FlexEnvelope.Read(FlexFixtures.WithStatementType(FlexFixtures.Read(FlexFixtures.Synthetic), "TCF"));
        Assert.Single(tcf.Validate(FlexFixtures.AccountId, window));

        Assert.Single(valid.Validate("U0000002", window));
        Assert.Single(valid.Validate(FlexFixtures.AccountId, window with { To = new DateOnly(2026, 11, 15) }));
    }
}
