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
        builder.Property(batch => batch.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false);
        builder.Property(batch => batch.Trigger)
            .HasColumnName("trigger")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsUnicode(false);
        builder.Property(batch => batch.RequestedFrom).HasColumnName("requested_from");
        builder.Property(batch => batch.RequestedTo).HasColumnName("requested_to");
        builder.Property(batch => batch.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsUnicode(false);
        builder.Property(batch => batch.ParserVersion).HasColumnName("parser_version").HasMaxLength(64).IsRequired();
        builder.Property(batch => batch.InsertedCount).HasColumnName("inserted_count");
        builder.Property(batch => batch.CorrectedCount).HasColumnName("corrected_count");
        builder.Property(batch => batch.UnchangedCount).HasColumnName("unchanged_count");
        builder.Property(batch => batch.CountsJson).HasColumnName("counts_json");
        builder.Property(batch => batch.WarningsJson).HasColumnName("warnings_json");
        builder.Property(batch => batch.ErrorCode).HasColumnName("error_code").HasMaxLength(64).IsUnicode(false);
        builder.Property(batch => batch.ErrorMessage).HasColumnName("error_message").HasMaxLength(ImportBatch.ErrorMessageMaxLength);
        builder.Property(batch => batch.StartedAt).HasColumnName("started_at");
        builder.Property(batch => batch.FinishedAt).HasColumnName("finished_at");
        builder.Property(batch => batch.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(batch => batch.CreatedAt).HasColumnName("created_at");
        builder.Property(batch => batch.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(batch => batch.BrokerAccountId)
            .IsUnique()
            .HasFilter("[status] = 'Running'")
            .HasDatabaseName("ux_import_batch_one_running_per_account");
        builder.HasOne<BrokerAccount>()
            .WithMany()
            .HasForeignKey(batch => batch.BrokerAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
