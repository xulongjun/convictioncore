using System.Reflection;
using ConvictionCore.Application.Abstractions;
using ConvictionCore.Domain.Portfolio;
using ConvictionCore.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace ConvictionCore.Architecture.Tests;

public class GuardrailTests
{
    private static readonly Assembly DomainAssembly = typeof(Owner).Assembly;

    [Fact]
    public void Domain_does_not_reference_http_xml_aspnet_or_application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "System.Net.Http",
                "System.Xml",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.Http",
                "ConvictionCore.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Application_does_not_reference_http_or_aspnet()
    {
        var result = Types.InAssembly(typeof(ICurrentUserContext).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "System.Net.Http",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.Http")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Infrastructure_does_not_reference_api()
    {
        var result = Types.InAssembly(typeof(ConvictionCoreDbContext).Assembly)
            .Should()
            .NotHaveDependencyOn("ConvictionCore.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Domain_has_no_binary_floating_point_members()
    {
        const BindingFlags AllDeclared =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        var offenders = new List<string>();
        foreach (var type in DomainAssembly.GetTypes())
        {
            offenders.AddRange(type.GetFields(AllDeclared)
                .Where(f => IsBinaryFloatingPoint(f.FieldType))
                .Select(f => $"{type.FullName}.{f.Name}"));
            offenders.AddRange(type.GetProperties(AllDeclared)
                .Where(p => IsBinaryFloatingPoint(p.PropertyType))
                .Select(p => $"{type.FullName}.{p.Name}"));
            offenders.AddRange(type.GetMethods(AllDeclared)
                .Where(m => !m.IsSpecialName)
                .Where(m => IsBinaryFloatingPoint(m.ReturnType) || m.GetParameters().Any(p => IsBinaryFloatingPoint(p.ParameterType)))
                .Select(m => $"{type.FullName}.{m.Name}()"));
            offenders.AddRange(type.GetConstructors(AllDeclared)
                .Where(c => c.GetParameters().Any(p => IsBinaryFloatingPoint(p.ParameterType)))
                .Select(c => $"{type.FullName}..ctor"));
        }

        Assert.True(offenders.Count == 0, "Use decimal for money, quantity, price and FX rate: " + string.Join(", ", offenders.Distinct()));
    }

    private static bool IsBinaryFloatingPoint(Type type)
    {
        if (type.HasElementType)
        {
            return IsBinaryFloatingPoint(type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments().Any(IsBinaryFloatingPoint);
        }

        return type == typeof(double) || type == typeof(float) || type == typeof(Half);
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypeNames is null ? "Architecture rule failed." : string.Join(", ", result.FailingTypeNames);
}
