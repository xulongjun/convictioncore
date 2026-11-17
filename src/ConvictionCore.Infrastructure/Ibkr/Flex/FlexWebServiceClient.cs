using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConvictionCore.Infrastructure.Ibkr.Flex;

internal sealed record FlexFetchedStatement(byte[] Payload, string ReferenceCode, DateTimeOffset FetchedAt);

/// <summary>
/// Flex Web Service v3: SendRequest, then poll GetStatement until the statement is ready.
/// Every HTTP call goes through <see cref="FlexRateLimiter"/>; all waits use the injected
/// <see cref="TimeProvider"/>.
/// </summary>
internal sealed partial class FlexWebServiceClient(
    HttpClient http,
    FlexRateLimiter limiter,
    IOptions<IbkrOptions> options,
    TimeProvider time,
    ILogger<FlexWebServiceClient> logger)
{
    public const string HttpClientName = "IbkrFlexWebService";

    private const int MaxConsecutiveRateLimits = 3;
    private static readonly TimeSpan RateLimitPause = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan[] HttpRetryDelays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)];

    private readonly FlexWebServiceOptions _flex = options.Value.FlexWebService;
    private readonly string? _token = options.Value.FlexToken;

    public async Task<FlexFetchedStatement> FetchAsync(string queryId, FlexDateWindow window, CancellationToken cancellationToken)
    {
        var token = string.IsNullOrWhiteSpace(_token)
            ? throw new InvalidOperationException("Ibkr:FlexToken missing; set via dotnet user-secrets")
            : _token;
        var state = new CallState();
        var renewedReference = false;
        while (true)
        {
            var referenceCode = await SendRequestAsync(token, queryId, window, state, cancellationToken).ConfigureAwait(false);
            var payload = await PollAsync(token, referenceCode, state, cancellationToken).ConfigureAwait(false);
            if (payload is not null)
            {
                return new FlexFetchedStatement(payload, referenceCode, time.GetUtcNow());
            }

            if (renewedReference)
            {
                throw new FlexServiceException("1017", "Reference code invalid twice (1017); giving up after one new SendRequest.");
            }

            renewedReference = true;
        }
    }

    private async Task<string> SendRequestAsync(
        string token,
        string queryId,
        FlexDateWindow window,
        CallState state,
        CancellationToken cancellationToken)
    {
        var uri = BuildUri(
            "SendRequest",
            ("t", token),
            ("q", queryId),
            ("fd", FlexTimestamp.FormatDate(window.From)),
            ("td", FlexTimestamp.FormatDate(window.To)),
            ("v", _flex.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var started = time.GetUtcNow();
        var backoff = TimeSpan.FromSeconds(_flex.PollInitialDelaySeconds);
        while (true)
        {
            var body = await GetAsync(uri, token, cancellationToken).ConfigureAwait(false);
            var response = FlexStatementResponse.TryParse(body)
                ?? throw new FlexServiceException(FlexErrorCodes.UnexpectedResponse, "SendRequest returned an unrecognized body.");
            if (response.IsSuccess && response.ReferenceCode is { } referenceCode)
            {
                state.ConsecutiveRateLimits = 0;
                return referenceCode;
            }

            var code = response.ErrorCode ?? FlexErrorCodes.UnexpectedResponse;
            switch (FlexErrorCodes.Classify(code))
            {
                case FlexErrorHandling.RateLimited:
                    await PauseForRateLimitAsync(state, response, token, cancellationToken).ConfigureAwait(false);
                    break;
                case FlexErrorHandling.Poll:
                    state.ConsecutiveRateLimits = 0;
                    await BackoffAsync(started, backoff, code, cancellationToken).ConfigureAwait(false);
                    backoff = NextBackoff(backoff);
                    break;
                default:
                    throw Fatal(code, response, token);
            }
        }
    }

    /// <returns>The statement bytes, or null when the reference code must be renewed (1017).</returns>
    private async Task<byte[]?> PollAsync(string token, string referenceCode, CallState state, CancellationToken cancellationToken)
    {
        var uri = BuildUri(
            "GetStatement",
            ("t", token),
            ("q", referenceCode),
            ("v", _flex.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var started = time.GetUtcNow();
        var backoff = TimeSpan.FromSeconds(_flex.PollInitialDelaySeconds);
        await Task.Delay(backoff, time, cancellationToken).ConfigureAwait(false);
        while (true)
        {
            var body = await GetAsync(uri, token, cancellationToken).ConfigureAwait(false);
            if (FlexStatementResponse.ReadRootName(body) == FlexStatementResponse.StatementRootName)
            {
                state.ConsecutiveRateLimits = 0;
                return body;
            }

            var response = FlexStatementResponse.TryParse(body);
            if (response is null || response.IsSuccess)
            {
                throw new FlexServiceException(FlexErrorCodes.UnexpectedResponse, "GetStatement returned an unrecognized body.");
            }

            var code = response.ErrorCode ?? FlexErrorCodes.UnexpectedResponse;
            switch (FlexErrorCodes.Classify(code))
            {
                case FlexErrorHandling.Poll:
                    state.ConsecutiveRateLimits = 0;
                    await BackoffAsync(started, backoff, code, cancellationToken).ConfigureAwait(false);
                    backoff = NextBackoff(backoff);
                    break;
                case FlexErrorHandling.RateLimited:
                    EnsureWithinPollTimeout(started, RateLimitPause, code);
                    await PauseForRateLimitAsync(state, response, token, cancellationToken).ConfigureAwait(false);
                    break;
                case FlexErrorHandling.RenewReference:
                    LogRenewingReference(logger);
                    return null;
                default:
                    throw Fatal(code, response, token);
            }
        }
    }

    private async Task PauseForRateLimitAsync(
        CallState state,
        FlexStatementResponse response,
        string token,
        CancellationToken cancellationToken)
    {
        state.ConsecutiveRateLimits++;
        if (state.ConsecutiveRateLimits >= MaxConsecutiveRateLimits)
        {
            throw new FlexServiceException(
                FlexErrorCodes.RateLimitExhausted,
                $"Flex rate limit (1018) returned {MaxConsecutiveRateLimits} times in a row: "
                + FlexRedaction.Redact(response.ErrorMessage ?? string.Empty, token));
        }

        LogRateLimited(logger, RateLimitPause.TotalSeconds);
        await Task.Delay(RateLimitPause, time, cancellationToken).ConfigureAwait(false);
    }

    private async Task BackoffAsync(DateTimeOffset started, TimeSpan delay, string code, CancellationToken cancellationToken)
    {
        EnsureWithinPollTimeout(started, delay, code);
        await Task.Delay(delay, time, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureWithinPollTimeout(DateTimeOffset started, TimeSpan nextDelay, string lastCode)
    {
        var elapsed = time.GetUtcNow() - started;
        if (elapsed + nextDelay > TimeSpan.FromSeconds(_flex.PollTimeoutSeconds))
        {
            throw new FlexServiceException(
                FlexErrorCodes.PollTimeout,
                $"Statement not ready after {elapsed.TotalSeconds:0} s (limit {_flex.PollTimeoutSeconds} s, last code {lastCode}).");
        }
    }

    private TimeSpan NextBackoff(TimeSpan current)
    {
        var doubled = current * 2;
        var max = TimeSpan.FromSeconds(_flex.PollMaxDelaySeconds);
        return doubled > max ? max : doubled;
    }

    private static FlexServiceException Fatal(string code, FlexStatementResponse response, string token)
    {
        var message = FlexRedaction.Redact(response.ErrorMessage ?? "no message", token);
        return new FlexServiceException(code, $"Flex error {code}: {message} {FlexErrorCodes.Hint(code)}");
    }

    private async Task<byte[]> GetAsync(Uri uri, string token, CancellationToken cancellationToken)
    {
        var redactedUri = FlexRedaction.Redact(uri, token);
        for (var attempt = 0; ; attempt++)
        {
            var canRetry = attempt < HttpRetryDelays.Length;
            try
            {
                var (status, body) = await limiter.RunAsync(
                    async callToken =>
                    {
                        using var response = await http.GetAsync(uri, callToken).ConfigureAwait(false);
                        var bytes = await response.Content.ReadAsByteArrayAsync(callToken).ConfigureAwait(false);
                        return (response.StatusCode, bytes);
                    },
                    cancellationToken).ConfigureAwait(false);
                LogResponse(logger, redactedUri, (int)status);

                if ((int)status >= 500 && canRetry)
                {
                    await Task.Delay(HttpRetryDelays[attempt], time, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (status != HttpStatusCode.OK && FlexStatementResponse.TryParse(body) is null)
                {
                    throw new FlexServiceException($"Http{(int)status}", $"Flex HTTP {(int)status} from {redactedUri}.");
                }

                return body;
            }
            catch (Exception exception) when (IsTransient(exception, cancellationToken))
            {
                if (!canRetry)
                {
                    var code = exception is TaskCanceledException ? FlexErrorCodes.HttpTimeout : "HttpError";
                    throw new FlexServiceException(
                        code,
                        $"Flex request to {redactedUri} failed after {HttpRetryDelays.Length + 1} attempts: "
                        + FlexRedaction.Redact(exception.Message, token));
                }

                LogTransientFailure(logger, redactedUri, attempt + 1);
                await Task.Delay(HttpRetryDelays[attempt], time, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private Uri BuildUri(string endpoint, params (string Name, string Value)[] query)
    {
        var parameters = string.Join('&', query.Select(pair => $"{pair.Name}={Uri.EscapeDataString(pair.Value)}"));
        return new Uri($"{_flex.BaseUrl.TrimEnd('/')}/{endpoint}?{parameters}");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Flex GET {Uri} -> {StatusCode}")]
    private static partial void LogResponse(ILogger logger, string uri, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Flex GET {Uri} failed (attempt {Attempt}); retrying")]
    private static partial void LogTransientFailure(ILogger logger, string uri, int attempt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Flex rate limit (1018); pausing {Seconds} s")]
    private static partial void LogRateLimited(ILogger logger, double seconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Flex reference code invalid (1017); sending a new request")]
    private static partial void LogRenewingReference(ILogger logger);

    private sealed class CallState
    {
        public int ConsecutiveRateLimits { get; set; }
    }
}
