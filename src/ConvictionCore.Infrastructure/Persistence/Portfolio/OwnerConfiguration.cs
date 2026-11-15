using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Portfolio;

internal sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("owner", "portfolio");
        builder.HasKey(owner => owner.Id);
        builder.Property(owner => owner.Id)
            .HasColumnName("owner_id")
            .HasConversion(id => id.Value, value => new OwnerId(value));
        builder.Property(owner => owner.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(owner => owner.Kind)
            .HasColumnName("owner_kind")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsUnicode(false);
        builder.Property(owner => owner.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(owner => owner.CreatedAt).HasColumnName("created_at");
        builder.Property(owner => owner.UpdatedAt).HasColumnName("updated_at");
    }
}
