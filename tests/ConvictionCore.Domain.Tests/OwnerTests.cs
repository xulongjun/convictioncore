using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Domain.SharedKernel;

namespace ConvictionCore.Domain.Tests;

public class OwnerTests
{
    [Fact]
    public void Register_creates_owner_with_zero_version()
    {
        var now = DateTimeOffset.Parse("2026-11-15T12:00:00Z");
        var owner = Owner.Register(OwnerId.New(), "Personal", OwnerKind.Personal, now);

        Assert.Equal("Personal", owner.DisplayName);
        Assert.Equal(OwnerKind.Personal, owner.Kind);
        Assert.Equal(0, owner.Version);
        Assert.Equal(now, owner.CreatedAt);
    }

    [Fact]
    public void Register_rejects_blank_display_name()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            Owner.Register(OwnerId.New(), "  ", OwnerKind.Personal, DateTimeOffset.UtcNow));

        Assert.Equal("displayName", exception.Field);
    }
}
