using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

internal sealed class IbkrSecurityConfiguration : IEntityTypeConfiguration<IbkrSecurity>
{
    public void Configure(EntityTypeBuilder<IbkrSecurity> builder)
    {
        builder.ConfigureFactTable("ibkr_security");
        builder.HasIndex(row => row.Conid)
            .IsUnique()
            .HasDatabaseName("ux_ibkr_security_conid");
    }
}
