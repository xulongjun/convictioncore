using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioRoot = ConvictionCore.Domain.Portfolio.Portfolio;

namespace ConvictionCore.Infrastructure.Persistence.Portfolio;

internal sealed class PortfolioConfiguration : IEntityTypeConfiguration<PortfolioRoot>
{
    public void Configure(EntityTypeBuilder<PortfolioRoot> builder)
    {
        builder.ToTable("portfolio", "portfolio");
        builder.HasKey(portfolio => portfolio.Id);
        builder.Property(portfolio => portfolio.Id)
            .HasColumnName("portfolio_id")
            .HasConversion(id => id.Value, value => new PortfolioId(value));
        builder.Property(portfolio => portfolio.OwnerId)
            .HasColumnName("owner_id")
            .HasConversion(id => id.Value, value => new OwnerId(value));
        builder.Property(portfolio => portfolio.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(portfolio => portfolio.ReportingCurrency)
            .HasColumnName("reporting_currency")
            .HasConversion(currency => currency.Value, value => CurrencyCode.Parse(value))
            .HasColumnType("char(3)")
            .IsFixedLength()
            .IsUnicode(false);
        builder.Property(portfolio => portfolio.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(portfolio => portfolio.CreatedAt).HasColumnName("created_at");
        builder.Property(portfolio => portfolio.UpdatedAt).HasColumnName("updated_at");
        builder.HasAlternateKey(portfolio => new { portfolio.Id, portfolio.OwnerId });
        builder.HasOne<Owner>().WithMany().HasForeignKey(portfolio => portfolio.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}
