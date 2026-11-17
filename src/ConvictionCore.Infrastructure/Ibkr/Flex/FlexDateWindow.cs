namespace ConvictionCore.Infrastructure.Ibkr.Flex;

internal readonly record struct FlexDateWindow(DateOnly From, DateOnly To)
{
    public int Days => To.DayNumber - From.DayNumber + 1;

    public override string ToString() => $"{From:yyyy-MM-dd}..{To:yyyy-MM-dd}";

    /// <summary>Consecutive, non-overlapping windows of at most <paramref name="maxDays"/> days (inclusive).</summary>
    public static IReadOnlyList<FlexDateWindow> Split(DateOnly from, DateOnly to, int maxDays)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDays, 1);
        if (from > to)
        {
            throw new ArgumentException("Window start must not be after its end.", nameof(from));
        }

        var windows = new List<FlexDateWindow>();
        var start = from;
        while (start <= to)
        {
            var end = start.AddDays(maxDays - 1);
            if (end > to)
            {
                end = to;
            }

            windows.Add(new FlexDateWindow(start, end));
            start = end.AddDays(1);
        }

        return windows;
    }
}
