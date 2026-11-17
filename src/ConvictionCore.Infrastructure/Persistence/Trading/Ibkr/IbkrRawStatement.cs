using ConvictionCore.Domain.SharedKernel;
using ConvictionCore.Domain.Trading;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

/// <summary>
/// One row per successful Flex fetch. Immutable: never updated or deleted.
/// <see cref="Payload"/> is the gzip of the original bytes; <see cref="Sha256"/> hashes the
/// uncompressed bytes.
/// </summary>
public sealed class IbkrRawStatement
{
    public Guid Id { get; set; }

    public ImportBatchId ImportBatchId { get; set; }

    public BrokerAccountId BrokerAccountId { get; set; }

    public IbkrFlexQueryKind QueryKind { get; set; }

    public string QueryId { get; set; } = string.Empty;

    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public string? ReferenceCode { get; set; }

    public DateTimeOffset? WhenGeneratedUtc { get; set; }

    public string? WhenGeneratedIbkrEt { get; set; }

    public DateTimeOffset FetchedAt { get; set; }

    public int ByteLength { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public string PayloadEncoding { get; set; } = "gzip";

    public byte[] Payload { get; set; } = [];
}

public enum IbkrFlexQueryKind
{
    Activity = 0,
    TradeConfirmation = 1
}
