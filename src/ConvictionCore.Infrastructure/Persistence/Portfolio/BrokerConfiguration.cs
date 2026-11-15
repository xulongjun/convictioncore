using ConvictionCore.Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Portfolio;

internal sealed class BrokerConfiguration : IEntityTypeConfiguration<Broker>
{
    public void Configure(EntityTypeBuilder<Broker> builder)
    {
        builder.ToTable("broker", "portfolio");
        builder.HasKey(broker => broker.Id);
        builder.Property(broker => broker.Id)
            .HasColumnName("broker_id")
            .HasConversion(id => id.Value, value => new BrokerId(value));
        builder.Property(broker => broker.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.HasIndex(broker => broker.Code).IsUnique();
        builder.Property(broker => broker.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.HasData(
            Broker.Seed(BrokerSeed.FirstradeId, "firstrade", "Firstrade"),
            Broker.Seed(BrokerSeed.IbkrId, "ibkr", "Interactive Brokers"));
    }
}
