using System.Net;
using ConvictionCore.Infrastructure.Ibkr;
using ConvictionCore.Infrastructure.Ibkr.Flex;
using ConvictionCore.Tests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConvictionCore.Infrastructure.Tests;

public sealed class FlexWebServiceClientTests : IDisposable
{
    private static readonly FlexDateWindow Window = new(new DateOnly(2026, 3, 1), new DateOnly(2026, 11, 16));
    private static readonly byte[] Statement = FlexFixtures.Read(FlexFixtures.Synthetic);

    private readonly AutoAdvancingTimeProvider _time = new(FlexTestConfiguration.Now);
    private readonly CapturingLoggerProvider _logs = new();
    private readonly FakeFlexHandler _handler;
    private ServiceProvider? _services;

    public FlexWebServiceClientTests()
    {
        _handler = new FakeFlexHandler(_time).ServeStatement(Statement);
    }

    public void Dispose()
    {
        _services?.Dispose();
        _handler.Dispose();
    }

    [Fact]
    [Trait("AC", "6")]
    public async Task Polls_through_generation_in_progress_until_the_statement_is_ready()
    {
        _handler.EnqueueGetStatement(FakeFlexHandler.Error("1019")).EnqueueGetStatement(FakeFlexHandler.Error("1019"));

        var fetched = await Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None);

        Assert.Equal(Statement, fetched.Payload);
        Assert.Equal("1234567890", fetched.ReferenceCode);
        Assert.Single(_handler.CallsTo("SendRequest"));
        Assert.Equal(3, _handler.CallsTo("GetStatement").Count());
        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)], PollDelays());
    }

    [Theory]
    [Trait("AC", "6")]
    [InlineData("1015")]
    [InlineData("1012")]
    public async Task Token_errors_fail_without_retrying(string code)
    {
        _handler.EnqueueSendRequest(FakeFlexHandler.Error(code));

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Equal(code, exception.ErrorCode);
        Assert.Single(_handler.Calls);
    }

    [Fact]
    [Trait("AC", "6")]
    public async Task Statement_not_available_fails_while_polling()
    {
        _handler.EnqueueGetStatement(FakeFlexHandler.Error("1003"));

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Equal("1003", exception.ErrorCode);
        Assert.Single(_handler.CallsTo("GetStatement"));
    }

    [Fact]
    [Trait("AC", "6")]
    public async Task Rate_limit_backs_off_at_least_60_seconds_then_retries()
    {
        _handler.EnqueueGetStatement(FakeFlexHandler.Error("1018"));

        await Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None);

        var polls = _handler.CallsTo("GetStatement").ToList();
        Assert.Equal(2, polls.Count);
        Assert.True(polls[1].At - polls[0].At >= TimeSpan.FromSeconds(60));
    }

    [Fact]
    [Trait("AC", "6")]
    public async Task Three_rate_limits_in_a_row_fail()
    {
        _handler.DefaultGetStatement = FakeFlexHandler.Error("1018");

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Equal("1018", exception.ErrorCode);
        Assert.Equal(3, _handler.CallsTo("GetStatement").Count());
    }

    [Fact]
    [Trait("AC", "6")]
    public async Task Poll_timeout_fails_with_poll_timeout()
    {
        _handler.DefaultGetStatement = FakeFlexHandler.Error("1019");
        var started = _time.GetUtcNow();

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Equal(FlexErrorCodes.PollTimeout, exception.ErrorCode);
        Assert.True(_time.GetUtcNow() - started <= TimeSpan.FromSeconds(601));
        Assert.True(_handler.CallsTo("GetStatement").Count() > 5);
    }

    [Fact]
    public async Task Invalid_reference_code_sends_one_new_request()
    {
        _handler.EnqueueGetStatement(FakeFlexHandler.Error("1017"));

        await Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None);

        Assert.Equal(2, _handler.CallsTo("SendRequest").Count());
    }

    [Fact]
    public async Task Invalid_reference_code_twice_fails()
    {
        _handler.DefaultGetStatement = FakeFlexHandler.Error("1017");

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Equal("1017", exception.ErrorCode);
        Assert.Equal(2, _handler.CallsTo("SendRequest").Count());
    }

    [Fact]
    public async Task Server_errors_retry_three_times_with_backoff()
    {
        _handler.DefaultSendRequest = _ => FakeFlexHandler.Xml("busy", HttpStatusCode.ServiceUnavailable);

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Equal("Http503", exception.ErrorCode);
        Assert.Equal(4, _handler.Calls.Count);
        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)], _time.Delays.Where(delay => delay.TotalSeconds is 2 or 4 or 8));
    }

    [Fact]
    public async Task Client_errors_fail_immediately()
    {
        _handler.DefaultSendRequest = _ => FakeFlexHandler.Xml("nope", HttpStatusCode.NotFound);

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Equal("Http404", exception.ErrorCode);
        Assert.Single(_handler.Calls);
    }

    [Fact]
    [Trait("AC", "7")]
    public async Task Calls_respect_one_per_second_and_ten_per_minute_with_user_agent_and_v3()
    {
        var client = Client(new()
        {
            ["Ibkr:FlexWebService:PollInitialDelaySeconds"] = "0",
            ["Ibkr:FlexWebService:PollMaxDelaySeconds"] = "0"
        });
        for (var index = 0; index < 14; index++)
        {
            _handler.EnqueueGetStatement(FakeFlexHandler.Error("1019"));
        }

        await client.FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None);

        var calls = _handler.Calls;
        Assert.Equal(16, calls.Count);
        for (var index = 1; index < calls.Count; index++)
        {
            Assert.True(calls[index].At - calls[index - 1].At >= TimeSpan.FromSeconds(1), $"calls {index - 1} and {index} closer than 1 s");
        }

        for (var index = 10; index < calls.Count; index++)
        {
            Assert.True(calls[index].At - calls[index - 10].At >= TimeSpan.FromSeconds(60), $"11 calls within 60 s ending at {index}");
        }

        Assert.All(calls, call =>
        {
            Assert.False(string.IsNullOrWhiteSpace(call.UserAgent));
            Assert.Equal("3", call.Query("v"));
        });
        var send = Assert.Single(_handler.CallsTo("SendRequest"));
        Assert.Equal("20260301", send.Query("fd"));
        Assert.Equal("20261116", send.Query("td"));
        Assert.Equal(FlexTestConfiguration.ActivityQueryId, send.Query("q"));
    }

    [Fact]
    [Trait("AC", "11")]
    public async Task Missing_token_sends_no_request()
    {
        var client = Client(token: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Empty(_handler.Calls);
    }

    [Fact]
    [Trait("AC", "11")]
    public async Task Token_never_appears_in_logs_or_errors()
    {
        _handler.EnqueueSendRequest(_ => FakeFlexHandler.Xml("busy", HttpStatusCode.BadGateway));
        _handler.EnqueueSendRequest(_ => throw new HttpRequestException(
            $"connection reset for https://flex.example.test/SendRequest?t={FlexTestConfiguration.SentinelToken}&q=1"));
        _handler.EnqueueSendRequest(FakeFlexHandler.Error("1015"));

        var exception = await Assert.ThrowsAsync<FlexServiceException>(
            () => Client().FetchAsync(FlexTestConfiguration.ActivityQueryId, Window, CancellationToken.None));

        Assert.Contains(_handler.Calls, call => call.Uri.Query.Contains(FlexTestConfiguration.SentinelToken, StringComparison.Ordinal));
        Assert.DoesNotContain(FlexTestConfiguration.SentinelToken, exception.ToString(), StringComparison.Ordinal);
        Assert.NotEmpty(_logs.Messages);
        Assert.Contains(_logs.Messages, message => message.Contains("t=***", StringComparison.Ordinal));
        Assert.All(_logs.Messages, message => Assert.DoesNotContain(FlexTestConfiguration.SentinelToken, message, StringComparison.Ordinal));
    }

    private IEnumerable<TimeSpan> PollDelays() => _time.Delays.Where(delay => delay >= TimeSpan.FromSeconds(5));

    private FlexWebServiceClient Client(Dictionary<string, string?>? overrides = null, string? token = FlexTestConfiguration.SentinelToken)
    {
        var settings = FlexTestConfiguration.Settings(token);
        foreach (var (key, value) in overrides ?? [])
        {
            settings[key] = value;
        }

        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(_logs));
        services.AddSingleton<TimeProvider>(_time);
        services.AddIbkrFlex(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddHttpClient(FlexWebServiceClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => _handler);
        _services = services.BuildServiceProvider();
        return _services.GetRequiredService<FlexWebServiceClient>();
    }
}
