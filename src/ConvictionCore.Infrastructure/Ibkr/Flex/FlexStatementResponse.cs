using System.Xml;
using System.Xml.Linq;

namespace ConvictionCore.Infrastructure.Ibkr.Flex;

/// <summary>The small <c>FlexStatementResponse</c> envelope used for SendRequest results and errors.</summary>
internal sealed record FlexStatementResponse(bool IsSuccess, string? ReferenceCode, string? ErrorCode, string? ErrorMessage)
{
    public const string RootName = "FlexStatementResponse";
    public const string StatementRootName = "FlexQueryResponse";

    public static XmlReaderSettings ReaderSettings { get; } = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true
    };

    /// <summary>Local name of the document element, or null when the body is not XML.</summary>
    public static string? ReadRootName(byte[] body)
    {
        try
        {
            using var stream = new MemoryStream(body, writable: false);
            using var reader = XmlReader.Create(stream, ReaderSettings);
            return reader.MoveToContent() == XmlNodeType.Element ? reader.LocalName : null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    public static FlexStatementResponse? TryParse(byte[] body)
    {
        try
        {
            using var stream = new MemoryStream(body, writable: false);
            using var reader = XmlReader.Create(stream, ReaderSettings);
            var root = XDocument.Load(reader).Root;
            if (root is null || root.Name.LocalName != RootName)
            {
                return null;
            }

            var status = root.Element("Status")?.Value.Trim();
            return new FlexStatementResponse(
                string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase),
                NullIfEmpty(root.Element("ReferenceCode")?.Value),
                NullIfEmpty(root.Element("ErrorCode")?.Value),
                NullIfEmpty(root.Element("ErrorMessage")?.Value));
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
