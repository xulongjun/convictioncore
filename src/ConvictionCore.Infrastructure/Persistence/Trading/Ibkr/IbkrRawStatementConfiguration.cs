using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using ConvictionCore.Domain.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrRawStatementConfiguration : IEntityTypeConfiguration<IbkrRawStatement>
{
    public void Configure(EntityTypeBuilder<IbkrRawStatement> builder)
    {
        builder.ToTable("ibkr_raw_statement", "trading");
        builder.HasKey(raw => raw.Id);
        builder.Property(raw => raw.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(raw => raw.ImportBatchId)
            .HasColumnName("import_batch_id")
            .HasConversion(id => id.Value, value => new ImportBatchId(value));
        builder.Property(raw => raw.BrokerAccountId)
            .HasColumnName("broker_account_id")
            .HasConversion(id => id.Value, value => new BrokerAccountId(value));
        builder.Property(raw => raw.QueryKind)
            .HasColumnName("query_kind")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false);
        builder.Property(raw => raw.QueryId).HasColumnName("query_id").HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(raw => raw.FromDate).HasColumnName("from_date");
        builder.Property(raw => raw.ToDate).HasColumnName("to_date");
        builder.Property(raw => raw.ReferenceCode).HasColumnName("reference_code").HasMaxLength(64).IsUnicode(false);
        builder.Property(raw => raw.WhenGeneratedUtc).HasColumnName("when_generated_utc");
        builder.Property(raw => raw.WhenGeneratedIbkrEt).HasColumnName("when_generated_ibkr_et").HasMaxLength(32).IsUnicode(false);
        builder.Property(raw => raw.FetchedAt).HasColumnName("fetched_at");
        builder.Property(raw => raw.ByteLength).HasColumnName("byte_length");
        builder.Property(raw => raw.Sha256)
            .HasColumnName("sha256")
            .HasMaxLength(64)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.Property(raw => raw.PayloadEncoding)
            .HasColumnName("payload_encoding")
            .HasMaxLength(16)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(raw => raw.Payload).HasColumnName("payload").HasColumnType("varbinary(max)").IsRequired();
        builder.HasIndex(raw => new { raw.BrokerAccountId, raw.FetchedAt });
        builder.HasIndex(raw => raw.Sha256);
        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(raw => raw.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BrokerAccount>()
            .WithMany()
            .HasForeignKey(raw => raw.BrokerAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
