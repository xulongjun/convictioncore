using ConvictionCore.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ConvictionCore.Architecture.Tests;

public class DeveloperIdentityTests
{
    [Fact]
    public void Production_refuses_configured_developer_principal()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DeveloperIdentity:PrincipalId"] = "must-not-authenticate"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ConfiguredCurrentUserContext(configuration, new StubHostEnvironment(Environments.Production)));

        Assert.Contains("DeveloperIdentity", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_accepts_configured_developer_principal()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DeveloperIdentity:PrincipalId"] = "local-dev"
            })
            .Build();

        var context = new ConfiguredCurrentUserContext(configuration, new StubHostEnvironment(Environments.Development));

        Assert.True(context.IsAuthenticated);
        Assert.Equal("local-dev", context.PrincipalId);
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "ConvictionCore.Tests";

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
