using ConvictionCore.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ConvictionCore.Infrastructure.Identity;

internal sealed class ConfiguredCurrentUserContext : ICurrentUserContext
{
    public ConfiguredCurrentUserContext(IConfiguration configuration, IHostEnvironment environment)
    {
        var principalId = configuration["DeveloperIdentity:PrincipalId"];
        var isDevelopment = environment.IsDevelopment()
            || string.Equals(environment.EnvironmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);

        if (isDevelopment)
        {
            PrincipalId = string.IsNullOrWhiteSpace(principalId) ? null : principalId.Trim();
            return;
        }

        if (!string.IsNullOrWhiteSpace(principalId))
        {
            throw new InvalidOperationException(
                "DeveloperIdentity:PrincipalId must not be configured outside Development.");
        }

        PrincipalId = null;
    }

    public string? PrincipalId { get; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(PrincipalId);
}
