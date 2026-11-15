using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConvictionCore.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ConvictionCore")
            ?? throw new InvalidOperationException("Connection string ConvictionCore is required.");
        SqlConnectionGuard.RejectReservedCatalogs(connectionString);
        services.AddDbContext<ConvictionCoreDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
}
