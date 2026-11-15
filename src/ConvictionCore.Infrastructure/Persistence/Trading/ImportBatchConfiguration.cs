using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using ConvictionCore.Domain.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading;

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("import_batch", "trading");
        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.Id)
            .HasColumnName("import_batch_id")
            .HasConversion(id => id.Value, value => new ImportBatchId(value));
        builder.Property(batch => batch.BrokerAccountId)
            .HasColumnName("broker_account_id")
            .HasConversion(id => id.Value, value => new BrokerAccountId(value));
        builder.Property(batch => batch.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
        builder.Property(batch => batch.ParserVersion).HasColumnName("parser_version").HasMaxLength(64).IsRequired();
        builder.Property(batch => batch.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsUnicode(false);
        builder.Property(batch => batch.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(batch => batch.CreatedAt).HasColumnName("created_at");
        builder.Property(batch => batch.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(batch => new { batch.BrokerAccountId, batch.ContentHash }).IsUnique();
        builder.HasOne<BrokerAccount>()
            .WithMany()
            .HasForeignKey(batch => batch.BrokerAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
