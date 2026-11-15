using ConvictionCore.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ConvictionCore.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUserContext, ConfiguredCurrentUserContext>();
        return services;
    }
}
