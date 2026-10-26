using System;
using System.Diagnostics;

namespace MoreRewards.Core;

/// <summary>
/// How long a frame may spend on sliced work (<see cref="SlicedJob"/>). Every job started in the same frame shares the
/// one slice, so two jobs running at once still cost a frame no more than <see cref="SliceMilliseconds"/>.
/// </summary>
public sealed class FrameBudget
{
    public const double SliceMilliseconds = 2.0;

    private readonly Func<long> _clock;
    private readonly long _sliceTicks;
    private float _frame = float.NaN;
    private long _deadline;

    public FrameBudget() : this(Stopwatch.GetTimestamp, (long)(SliceMilliseconds * Stopwatch.Frequency / 1000.0))
    {
    }

    /// <param name="clock">The time now, in ticks.</param>
    /// <param name="sliceTicks">How many of those ticks a frame may spend.</param>
    internal FrameBudget(Func<long> clock, long sliceTicks)
    {
        _clock = clock;
        _sliceTicks = sliceTicks;
        IsSpent = () => _clock() >= _deadline;
    }

    /// <summary>Whether this frame's slice is used up; made once, so asking costs nothing to allocate.</summary>
    public Func<bool> IsSpent { get; }

    /// <summary>Starts the slice for the frame at <paramref name="now"/>; a later start in the same frame keeps the slice already running.</summary>
    public void Start(float now)
    {
        if (now == _frame) return;

        _frame = now;
        _deadline = _clock() + _sliceTicks;
    }
}
