using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

namespace ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;

internal sealed class FlexActivityStatement
{
    public List<IbkrTradeExecution> Executions { get; } = [];

    public List<IbkrClosedLot> ClosedLots { get; } = [];

    public List<IbkrOptionEae> OptionEaes { get; } = [];

    public List<IbkrOpenPositionLot> OpenPositionLots { get; } = [];

    public List<IbkrSecurity> Securities { get; } = [];

    public List<IbkrCorporateAction> CorporateActions { get; } = [];

    public IbkrAccountInfo? AccountInfo { get; set; }

    public List<string> Warnings { get; } = [];
}

/// <summary>
/// Parses an Activity Flex statement into fact rows. Every mapper reads a fixed attribute
/// whitelist, so personal fields (primaryEmail, dateOfBirth, names ...) are never read.
/// Rows come back without ids, hashes or audit columns; the upserter owns those.
/// </summary>
internal static class FlexActivityParser
{
    public const string ParserVersion = "ibkr-flex-activity/1";

    private const string Execution = "EXECUTION";
    private const string ClosedLot = "CLOSED_LOT";
    private const string Lot = "LOT";
    private const string Summary = "SUMMARY";

    public static FlexActivityStatement Parse(byte[] payload)
    {
        XDocument document;
        try
        {
            using var stream = new MemoryStream(payload, writable: false);
            using var reader = XmlReader.Create(stream, FlexStatementResponse.ReaderSettings);
            document = XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new FlexParseException($"Statement is not well-formed XML: {exception.Message}");
        }

        var statementElement = document.Root?.Element("FlexStatements")?.Element("FlexStatement")
            ?? throw new FlexParseException("FlexStatement element is missing.");

        var statement = new FlexActivityStatement();
        var ignored = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var section in statementElement.Elements())
        {
            switch (section.Name.LocalName)
            {
                case "AccountInformation":
                    statement.AccountInfo = ParseAccountInfo(section);
                    break;
                case "Trades":
                    ParseTrades(section, statement, ignored);
                    break;
                case "OptionEAE":
                    statement.OptionEaes.AddRange(Rows(section, "OptionEAE", ignored).Select(ParseOptionEae));
                    break;
                case "OpenPositions":
                    ParseOpenPositions(section, statement, ignored);
                    break;
                case "SecuritiesInfo":
                    statement.Securities.AddRange(Rows(section, "SecurityInfo", ignored).Select(ParseSecurity));
                    break;
                case "CorporateActions":
                    ParseCorporateActions(section, statement, ignored);
                    break;
                default:
                    Count(ignored, $"section {section.Name.LocalName}");
                    break;
            }
        }

        RejectDuplicates(statement.Executions, row => row.TransactionId.ToString(CultureInfo.InvariantCulture), "Trade[EXECUTION] transactionID");
        RejectDuplicates(statement.ClosedLots, row => $"{row.CloseTransactionId}/{row.OpenTransactionId}", "Lot[CLOSED_LOT] parent/transactionID");
        RejectDuplicates(statement.OptionEaes, row => $"{row.TradeId}/{row.Conid}/{row.TransactionType}", "OptionEAE tradeID/conid/transactionType");
        RejectDuplicates(statement.OpenPositionLots, row => $"{row.ReportDate}/{row.Conid}/{row.OriginatingTransactionId}", "OpenPosition[LOT] reportDate/conid/originatingTransactionID");
        RejectDuplicates(statement.Securities, row => row.Conid.ToString(CultureInfo.InvariantCulture), "SecurityInfo conid");
        RejectDuplicates(statement.CorporateActions, CorporateActionKey, "CorporateAction key");

        foreach (var (what, count) in ignored)
        {
            statement.Warnings.Add($"Ignored {count} unsupported {what}.");
        }

        return statement;
    }

    public static string CorporateActionKey(IbkrCorporateAction row) =>
        row.TransactionId is { } transactionId
            ? $"t:{transactionId}"
            : $"a:{row.ActionId}/{row.Conid}";

    private static IEnumerable<XElement> Rows(XElement section, string rowName, IDictionary<string, int> ignored)
    {
        foreach (var element in section.Elements())
        {
            if (element.Name.LocalName == rowName)
            {
                yield return element;
            }
            else
            {
                Count(ignored, $"{section.Name.LocalName}/{element.Name.LocalName} rows");
            }
        }
    }

    private static void ParseTrades(XElement section, FlexActivityStatement statement, IDictionary<string, int> ignored)
    {
        XElement? previousTrade = null;
        foreach (var element in section.Elements())
        {
            var level = element.Text("levelOfDetail");
            switch (element.Name.LocalName)
            {
                case "Trade":
                    previousTrade = element;
                    if (level == Execution)
                    {
                        statement.Executions.Add(ParseExecution(element));
                    }
                    else
                    {
                        Count(ignored, $"Trade rows with levelOfDetail={level}");
                    }

                    break;
                case "Lot" when level == ClosedLot:
                    if (previousTrade is null || previousTrade.Text("levelOfDetail") != Execution)
                    {
                        throw new FlexParseException("Lot[CLOSED_LOT] has no preceding Trade[EXECUTION] to attach to.");
                    }

                    statement.ClosedLots.Add(ParseClosedLot(element, previousTrade.RequiredLong("transactionID")));
                    break;
                case "Lot":
                    Count(ignored, $"Lot rows with levelOfDetail={level}");
                    break;
                default:
                    Count(ignored, $"Trades/{element.Name.LocalName} rows");
                    break;
            }
        }
    }

    private static void ParseOpenPositions(XElement section, FlexActivityStatement statement, IDictionary<string, int> ignored)
    {
        var summaries = new List<(DateOnly ReportDate, long Conid, decimal Position)>();
        foreach (var element in Rows(section, "OpenPosition", ignored))
        {
            switch (element.Text("levelOfDetail"))
            {
                case Lot:
                    statement.OpenPositionLots.Add(ParseOpenPositionLot(element));
                    break;
                case Summary:
                    summaries.Add((element.RequiredDate("reportDate"), element.RequiredLong("conid"), element.Decimal("position") ?? 0m));
                    break;
                default:
                    Count(ignored, $"OpenPosition rows with levelOfDetail={element.Text("levelOfDetail")}");
                    break;
            }
        }

        foreach (var summary in summaries)
        {
            var lots = statement.OpenPositionLots
                .Where(lot => lot.ReportDate == summary.ReportDate && lot.Conid == summary.Conid)
                .Sum(lot => lot.Position ?? 0m);
            if (lots != summary.Position)
            {
                statement.Warnings.Add(
                    $"OpenPositions conid {summary.Conid} on {summary.ReportDate:yyyy-MM-dd}: lots sum to {lots}, summary position is {summary.Position}.");
            }
        }
    }

    private static void ParseCorporateActions(XElement section, FlexActivityStatement statement, IDictionary<string, int> ignored)
    {
        foreach (var element in Rows(section, "CorporateAction", ignored))
        {
            var level = element.Text("levelOfDetail");
            if (level is not null && level != "DETAIL")
            {
                Count(ignored, $"CorporateAction rows with levelOfDetail={level}");
                continue;
            }

            var row = ParseCorporateAction(element);
            if (row.TransactionId is null && row.ActionId is null)
            {
                throw new FlexParseException("CorporateAction has neither transactionID nor actionID.");
            }

            statement.CorporateActions.Add(row);
        }
    }

    private static IbkrAccountInfo ParseAccountInfo(XElement element) => new()
    {
        Currency = element.Text("currency"),
        AccountType = element.Text("accountType"),
        CustomerType = element.Text("customerType"),
        DateOpened = element.Date("dateOpened"),
        DateFunded = element.Date("dateFunded")
    };

    private static IbkrTradeExecution ParseExecution(XElement element)
    {
        var dateTime = element.Timestamp("dateTime");
        var openDateTime = element.Timestamp("openDateTime");
        var holdingPeriod = element.Timestamp("holdingPeriodDateTime");
        return new IbkrTradeExecution
        {
            TransactionId = element.RequiredLong("transactionID"),
            TradeId = element.Long("tradeID"),
            IbExecId = element.Text("ibExecID"),
            IbOrderId = element.Long("ibOrderID"),
            OrigTradeId = element.Long("origTradeID"),
            OrigTransactionId = element.Long("origTransactionID"),
            RelatedTransactionId = element.Long("relatedTransactionID"),
            TransactionType = element.Text("transactionType"),
            AssetCategory = element.Text("assetCategory"),
            Conid = element.RequiredLong("conid"),
            UnderlyingConid = element.Long("underlyingConid"),
            Symbol = element.Text("symbol"),
            Description = element.Text("description"),
            UnderlyingSymbol = element.Text("underlyingSymbol"),
            PutCall = element.Text("putCall"),
            Strike = element.Decimal("strike"),
            Expiry = element.Date("expiry"),
            Multiplier = element.Decimal("multiplier"),
            Currency = element.Text("currency"),
            FxRateToBase = element.Decimal("fxRateToBase"),
            DateTimeUtc = dateTime.Utc,
            DateTimeIbkrEt = dateTime.IbkrEt,
            TradeDate = element.Date("tradeDate"),
            SettleDateTarget = element.Date("settleDateTarget"),
            BuySell = element.Text("buySell"),
            Quantity = element.Decimal("quantity"),
            TradePrice = element.Decimal("tradePrice"),
            Proceeds = element.Decimal("proceeds"),
            IbCommission = element.Decimal("ibCommission"),
            IbCommissionCurrency = element.Text("ibCommissionCurrency"),
            Taxes = element.Decimal("taxes"),
            NetCash = element.Decimal("netCash"),
            Cost = element.Decimal("cost"),
            FifoPnlRealized = element.Decimal("fifoPnlRealized"),
            MtmPnl = element.Decimal("mtmPnl"),
            OpenCloseIndicator = element.Text("openCloseIndicator"),
            Notes = element.Text("notes"),
            OpenDateTimeUtc = openDateTime.Utc,
            OpenDateTimeIbkrEt = openDateTime.IbkrEt,
            HoldingPeriodDateTimeUtc = holdingPeriod.Utc,
            HoldingPeriodDateTimeIbkrEt = holdingPeriod.IbkrEt,
            LevelOfDetail = Execution
        };
    }

    private static IbkrClosedLot ParseClosedLot(XElement element, long closeTransactionId)
    {
        var dateTime = element.Timestamp("dateTime");
        var openDateTime = element.Timestamp("openDateTime");
        var holdingPeriod = element.Timestamp("holdingPeriodDateTime");
        return new IbkrClosedLot
        {
            CloseTransactionId = closeTransactionId,
            OpenTransactionId = element.RequiredLong("transactionID"),
            Conid = element.RequiredLong("conid"),
            AssetCategory = element.Text("assetCategory"),
            Symbol = element.Text("symbol"),
            Currency = element.Text("currency"),
            Multiplier = element.Decimal("multiplier"),
            Quantity = element.Decimal("quantity"),
            TradePrice = element.Decimal("tradePrice"),
            Cost = element.Decimal("cost"),
            FifoPnlRealized = element.Decimal("fifoPnlRealized"),
            DateTimeUtc = dateTime.Utc,
            DateTimeIbkrEt = dateTime.IbkrEt,
            TradeDate = element.Date("tradeDate"),
            OpenDateTimeUtc = openDateTime.Utc,
            OpenDateTimeIbkrEt = openDateTime.IbkrEt,
            HoldingPeriodDateTimeUtc = holdingPeriod.Utc,
            HoldingPeriodDateTimeIbkrEt = holdingPeriod.IbkrEt,
            BuySell = element.Text("buySell"),
            Notes = element.Text("notes")
        };
    }

    private static IbkrOptionEae ParseOptionEae(XElement element) => new()
    {
        TradeId = element.RequiredLong("tradeID"),
        Conid = element.RequiredLong("conid"),
        TransactionType = element.RequiredText("transactionType"),
        Date = element.Date("date"),
        AssetCategory = element.Text("assetCategory"),
        Symbol = element.Text("symbol"),
        UnderlyingConid = element.Long("underlyingConid"),
        UnderlyingSymbol = element.Text("underlyingSymbol"),
        PutCall = element.Text("putCall"),
        Strike = element.Decimal("strike"),
        Expiry = element.Date("expiry"),
        Multiplier = element.Decimal("multiplier"),
        Currency = element.Text("currency"),
        Quantity = element.Decimal("quantity"),
        TradePrice = element.Decimal("tradePrice"),
        MarkPrice = element.Decimal("markPrice"),
        Proceeds = element.Decimal("proceeds"),
        CommisionsAndTax = element.Decimal("commisionsAndTax"),
        CostBasis = element.Decimal("costBasis"),
        RealizedPnl = element.Decimal("realizedPnl"),
        MtmPnl = element.Decimal("mtmPnl"),
        FxPnl = element.Decimal("fxPnl")
    };

    private static IbkrOpenPositionLot ParseOpenPositionLot(XElement element)
    {
        var openDateTime = element.Timestamp("openDateTime");
        var holdingPeriod = element.Timestamp("holdingPeriodDateTime");
        return new IbkrOpenPositionLot
        {
            ReportDate = element.RequiredDate("reportDate"),
            Conid = element.RequiredLong("conid"),
            OriginatingTransactionId = element.RequiredLong("originatingTransactionID"),
            OriginatingOrderId = element.Long("originatingOrderID"),
            AssetCategory = element.Text("assetCategory"),
            Symbol = element.Text("symbol"),
            UnderlyingConid = element.Long("underlyingConid"),
            PutCall = element.Text("putCall"),
            Strike = element.Decimal("strike"),
            Expiry = element.Date("expiry"),
            Multiplier = element.Decimal("multiplier"),
            Currency = element.Text("currency"),
            FxRateToBase = element.Decimal("fxRateToBase"),
            Position = element.Decimal("position"),
            Side = element.Text("side"),
            MarkPrice = element.Decimal("markPrice"),
            PositionValue = element.Decimal("positionValue"),
            OpenPrice = element.Decimal("openPrice"),
            CostBasisPrice = element.Decimal("costBasisPrice"),
            CostBasisMoney = element.Decimal("costBasisMoney"),
            FifoPnlUnrealized = element.Decimal("fifoPnlUnrealized"),
            OpenDateTimeUtc = openDateTime.Utc,
            OpenDateTimeIbkrEt = openDateTime.IbkrEt,
            HoldingPeriodDateTimeUtc = holdingPeriod.Utc,
            HoldingPeriodDateTimeIbkrEt = holdingPeriod.IbkrEt
        };
    }

    private static IbkrSecurity ParseSecurity(XElement element) => new()
    {
        Conid = element.RequiredLong("conid"),
        AssetCategory = element.Text("assetCategory"),
        SubCategory = element.Text("subCategory"),
        Symbol = element.Text("symbol"),
        Description = element.Text("description"),
        UnderlyingConid = element.Long("underlyingConid"),
        UnderlyingSymbol = element.Text("underlyingSymbol"),
        UnderlyingCategory = element.Text("underlyingCategory"),
        Multiplier = element.Decimal("multiplier"),
        Strike = element.Decimal("strike"),
        Expiry = element.Date("expiry"),
        PutCall = element.Text("putCall"),
        ListingExchange = element.Text("listingExchange"),
        Currency = element.Text("currency"),
        Isin = element.Text("isin"),
        Cusip = element.Text("cusip"),
        Figi = element.Text("figi"),
        SettlementPolicyMethod = element.Text("settlementPolicyMethod")
    };

    private static IbkrCorporateAction ParseCorporateAction(XElement element)
    {
        var dateTime = element.Timestamp("dateTime");
        return new IbkrCorporateAction
        {
            TransactionId = element.Long("transactionID"),
            ActionId = element.Long("actionID"),
            Conid = element.RequiredLong("conid"),
            Type = element.Text("type"),
            ReportDate = element.Date("reportDate"),
            DateTimeUtc = dateTime.Utc,
            DateTimeIbkrEt = dateTime.IbkrEt,
            AssetCategory = element.Text("assetCategory"),
            Symbol = element.Text("symbol"),
            Currency = element.Text("currency"),
            Description = element.Text("description"),
            ActionDescription = element.Text("actionDescription"),
            Quantity = element.Decimal("quantity"),
            Amount = element.Decimal("amount"),
            Proceeds = element.Decimal("proceeds"),
            Value = element.Decimal("value"),
            FifoPnlRealized = element.Decimal("fifoPnlRealized")
        };
    }

    private static void RejectDuplicates<T>(IEnumerable<T> rows, Func<T, string> key, string what)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!seen.Add(key(row)))
            {
                throw new FlexParseException($"Duplicate {what} '{key(row)}' in one statement.");
            }
        }
    }

    private static void Count(IDictionary<string, int> counts, string what) =>
        counts[what] = counts.TryGetValue(what, out var count) ? count + 1 : 1;
}
