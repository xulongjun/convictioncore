using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioRoot = ConvictionCore.Domain.Portfolio.Portfolio;

namespace ConvictionCore.Infrastructure.Persistence.Portfolio;

internal sealed class BrokerAccountConfiguration : IEntityTypeConfiguration<BrokerAccount>
{
    public void Configure(EntityTypeBuilder<BrokerAccount> builder)
    {
        builder.ToTable("broker_account", "portfolio");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id)
            .HasColumnName("broker_account_id")
            .HasConversion(id => id.Value, value => new BrokerAccountId(value));
        builder.Property(account => account.PortfolioId)
            .HasColumnName("portfolio_id")
            .HasConversion(id => id.Value, value => new PortfolioId(value));
        builder.Property(account => account.OwnerId)
            .HasColumnName("owner_id")
            .HasConversion(id => id.Value, value => new OwnerId(value));
        builder.Property(account => account.BrokerId)
            .HasColumnName("broker_id")
            .HasConversion(id => id.Value, value => new BrokerId(value));
        builder.Property(account => account.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(account => account.ExternalReference).HasColumnName("external_reference").HasMaxLength(128);
        builder.Property(account => account.MaskedReference).HasColumnName("masked_reference").HasMaxLength(32).IsRequired();
        builder.Property(account => account.BaseCurrency)
            .HasColumnName("base_currency")
            .HasConversion(currency => currency.Value, value => CurrencyCode.Parse(value))
            .HasColumnType("char(3)")
            .IsFixedLength()
            .IsUnicode(false);
        builder.Property(account => account.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsUnicode(false);
        builder.Property(account => account.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(account => account.CreatedAt).HasColumnName("created_at");
        builder.Property(account => account.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(account => new { account.BrokerId, account.ExternalReference })
            .IsUnique()
            .HasFilter("[external_reference] IS NOT NULL");
        builder.HasOne<PortfolioRoot>()
            .WithMany()
            .HasForeignKey(account => new { account.PortfolioId, account.OwnerId })
            .HasPrincipalKey(portfolio => new { portfolio.Id, portfolio.OwnerId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Broker>().WithMany().HasForeignKey(account => account.BrokerId).OnDelete(DeleteBehavior.Restrict);
    }
}
