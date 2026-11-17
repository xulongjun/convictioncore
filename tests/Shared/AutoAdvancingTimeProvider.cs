using Microsoft.Extensions.Time.Testing;

namespace ConvictionCore.Tests.Shared;

/// <summary>
/// A <see cref="FakeTimeProvider"/> that jumps virtual time forward by each timer's due time
/// as soon as the timer is created, so delays complete instantly and deterministically.
/// </summary>
public sealed class AutoAdvancingTimeProvider(DateTimeOffset start) : FakeTimeProvider(start)
{
    private readonly List<TimeSpan> _delays = [];

    public IReadOnlyList<TimeSpan> Delays
    {
        get
        {
            lock (_delays)
            {
                return [.. _delays];
            }
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        if (dueTime > TimeSpan.Zero && dueTime != Timeout.InfiniteTimeSpan)
        {
            lock (_delays)
            {
                _delays.Add(dueTime);
            }

            _ = Task.Run(() => Advance(dueTime));
        }

        return timer;
    }
}
