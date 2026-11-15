using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConvictionCore.Infrastructure.Persistence;

/// <summary>
/// Explicit migrate helper for tests and ops tooling.
/// Do not call from Production API startup.
/// </summary>
public static class DatabaseMigration
{
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConvictionCoreDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
