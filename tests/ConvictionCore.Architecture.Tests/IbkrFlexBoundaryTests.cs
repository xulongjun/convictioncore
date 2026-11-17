using ConvictionCore.Application.Ibkr;
using ConvictionCore.Infrastructure.Persistence;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;
using NetArchTest.Rules;

namespace ConvictionCore.Architecture.Tests;

public class IbkrFlexBoundaryTests
{
    [Fact]
    public void Application_flex_port_does_not_reference_xml_or_infrastructure()
    {
        var result = Types.InAssembly(typeof(IIbkrFlexSync).Assembly)
            .Should()
            .NotHaveDependencyOnAny("System.Xml", "System.Net.Http", "ConvictionCore.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Api_reaches_the_flex_importer_only_through_the_application_port()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .Should()
            .NotHaveDependencyOnAny("ConvictionCore.Infrastructure.Ibkr.Flex", "ConvictionCore.Infrastructure.Ibkr.Sync", "System.Xml")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Flex_client_and_parser_live_in_infrastructure()
    {
        var flexTypes = Types.InAssembly(typeof(ConvictionCoreDbContext).Assembly)
            .That()
            .ResideInNamespaceStartingWith("ConvictionCore.Infrastructure.Ibkr.Flex")
            .GetTypes()
            .Select(type => type.Name)
            .ToList();

        Assert.Contains("FlexWebServiceClient", flexTypes);
        Assert.Contains("FlexActivityParser", flexTypes);
    }

    [Fact]
    public void Ibkr_fact_rows_use_decimal_not_binary_floating_point()
    {
        var offenders = typeof(IbkrFactRow).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(IbkrFactRow).Namespace)
            .SelectMany(type => type.GetProperties().Select(property => (type, property)))
            .Where(pair => (Nullable.GetUnderlyingType(pair.property.PropertyType) ?? pair.property.PropertyType) is var clr
                && (clr == typeof(double) || clr == typeof(float)))
            .Select(pair => $"{pair.type.Name}.{pair.property.Name}")
            .ToList();

        Assert.Empty(offenders);
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypeNames is null ? "Architecture rule failed." : string.Join(", ", result.FailingTypeNames);
}
