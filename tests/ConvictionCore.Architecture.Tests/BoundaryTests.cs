using ConvictionCore.Application.Abstractions;
using ConvictionCore.Domain.Portfolio;
using Microsoft.AspNetCore.Mvc.Testing;
using NetArchTest.Rules;

namespace ConvictionCore.Architecture.Tests;

public class BoundaryTests
{
    [Fact]
    public void Domain_does_not_reference_entity_framework()
    {
        var result = Types.InAssembly(typeof(Owner).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Data.SqlClient",
                "ConvictionCore.Infrastructure",
                "ConvictionCore.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Application_does_not_reference_entity_framework_or_infrastructure()
    {
        var result = Types.InAssembly(typeof(ICurrentUserContext).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Data.SqlClient",
                "ConvictionCore.Infrastructure",
                "ConvictionCore.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Host_refuses_reserved_production_catalog()
    {
        using (OverrideConnection(
            "Server=localhost,1433;Database=ConvictionCore_Prod;User Id=sa;Password=x;TrustServerCertificate=True;Encrypt=True"))
        using (var factory = new WebApplicationFactory<Program>())
        {
            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
            Assert.Contains("ConvictionCore_Prod", exception.ToString(), StringComparison.Ordinal);
        }
    }

    private static EnvironmentOverride OverrideConnection(string connectionString) => new(connectionString);

    private static string FormatFailures(TestResult result) =>
        result.FailingTypeNames is null ? "Architecture rule failed." : string.Join(", ", result.FailingTypeNames);

    private sealed class EnvironmentOverride : IDisposable
    {
        private const string ConnectionVariable = "ConnectionStrings__ConvictionCore";
        private readonly string? _previousConnection;

        public EnvironmentOverride(string connectionString)
        {
            _previousConnection = Environment.GetEnvironmentVariable(ConnectionVariable);
            Environment.SetEnvironmentVariable(ConnectionVariable, connectionString);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(ConnectionVariable, _previousConnection);
    }
}
