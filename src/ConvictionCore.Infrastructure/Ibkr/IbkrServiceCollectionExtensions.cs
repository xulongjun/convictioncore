using ConvictionCore.Application.Ibkr;
using ConvictionCore.Infrastructure.Ibkr.Flex;
using ConvictionCore.Infrastructure.Ibkr.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ConvictionCore.Infrastructure.Ibkr;

public static class IbkrServiceCollectionExtensions
{
    public static IServiceCollection AddIbkrFlex(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IbkrOptions>()
            .Bind(configuration.GetSection(IbkrOptions.SectionName))
            .Validate(
                options => !options.Queries.TradeConfirmation.Enabled,
                "Ibkr:Queries:TradeConfirmation is not supported yet (PBI-01 imports Activity only); set Enabled=false.")
            .Validate(
                options => Uri.TryCreate(options.FlexWebService.BaseUrl, UriKind.Absolute, out _),
                "Ibkr:FlexWebService:BaseUrl must be an absolute URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.FlexWebService.UserAgent),
                "Ibkr:FlexWebService:UserAgent is required by the Flex Web Service.")
            .Validate(
                options => options.FlexWebService.MaxWindowDays is >= 1 and <= 365,
                "Ibkr:FlexWebService:MaxWindowDays must be between 1 and 365.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<FlexRateLimiter>();
        services.AddHttpClient<FlexWebServiceClient>(
                FlexWebServiceClient.HttpClientName,
                (provider, client) =>
                {
                    var flex = provider.GetRequiredService<IOptions<IbkrOptions>>().Value.FlexWebService;
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(flex.UserAgent);
                    client.Timeout = TimeSpan.FromSeconds(60);
                })
            // The default HttpClient loggers write request URIs, which carry the token as t=.
            .RemoveAllLoggers();
        services.AddScoped<IIbkrFlexSync, IbkrFlexSyncService>();
        return services;
    }
}
