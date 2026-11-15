using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Domain.Trading;

public sealed class ImportBatch
{
    private ImportBatch()
    {
    }

    public ImportBatchId Id { get; private set; }

    public BrokerAccountId BrokerAccountId { get; private set; }

    public string ContentHash { get; private set; } = string.Empty;

    public string ParserVersion { get; private set; } = string.Empty;

    public ImportBatchStatus Status { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ImportBatch Accept(
        ImportBatchId id,
        BrokerAccountId brokerAccountId,
        string contentHash,
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

        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new InvalidOperationException("Content hash is required.");
        }

        if (string.IsNullOrWhiteSpace(parserVersion))
        {
            throw new InvalidOperationException("Parser version is required.");
        }

        return new ImportBatch
        {
            Id = id,
            BrokerAccountId = brokerAccountId,
            ContentHash = contentHash.Trim(),
            ParserVersion = parserVersion.Trim(),
            Status = ImportBatchStatus.Accepted,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
