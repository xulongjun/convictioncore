using Microsoft.Extensions.Options;

namespace ConvictionCore.Infrastructure.Ibkr.Flex;

/// <summary>
/// Process-wide limiter for the single Flex token: calls are serialized, spaced by at least
/// <see cref="FlexWebServiceOptions.MinRequestIntervalMs"/>, and at most
/// <see cref="FlexWebServiceOptions.MaxRequestsPerMinute"/> start in any 60 s window.
/// </summary>
internal sealed class FlexRateLimiter : IDisposable
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Queue<DateTimeOffset> _recent = new();
    private readonly TimeProvider _time;
    private readonly TimeSpan _minInterval;
    private readonly int _maxPerWindow;
    private DateTimeOffset? _last;

    public FlexRateLimiter(IOptions<IbkrOptions> options, TimeProvider time)
    {
        var flex = options.Value.FlexWebService;
        _time = time;
        _minInterval = TimeSpan.FromMilliseconds(Math.Max(0, flex.MinRequestIntervalMs));
        _maxPerWindow = Math.Max(1, flex.MaxRequestsPerMinute);
    }

    /// <summary>Waits for a slot, then runs <paramref name="call"/> while holding it.</summary>
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> call, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WaitForSlotAsync(cancellationToken).ConfigureAwait(false);
            return await call(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private async Task WaitForSlotAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var now = _time.GetUtcNow();
            while (_recent.Count > 0 && now - _recent.Peek() >= Window)
            {
                _recent.Dequeue();
            }

            var wait = TimeSpan.Zero;
            if (_last is { } last && now - last < _minInterval)
            {
                wait = _minInterval - (now - last);
            }

            if (_recent.Count >= _maxPerWindow)
            {
                var windowWait = Window - (now - _recent.Peek());
                if (windowWait > wait)
                {
                    wait = windowWait;
                }
            }

            if (wait <= TimeSpan.Zero)
            {
                _last = now;
                _recent.Enqueue(now);
                return;
            }

            await Task.Delay(wait, _time, cancellationToken).ConfigureAwait(false);
        }
    }
}
