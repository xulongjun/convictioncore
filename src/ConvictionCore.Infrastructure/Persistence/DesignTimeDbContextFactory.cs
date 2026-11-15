using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConvictionCore.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> when no startup project connection string is set.
/// Prefer: <c>dotnet ef database update --project src/ConvictionCore.Infrastructure --startup-project src/ConvictionCore.Api</c>
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConvictionCoreDbContext>
{
    public ConvictionCoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ConvictionCore")
            ?? "Server=localhost,1433;Database=ConvictionCore;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=True";

        var options = new DbContextOptionsBuilder<ConvictionCoreDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ConvictionCoreDbContext(options);
    }
}
