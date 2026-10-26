using System;
using System.Collections.Generic;

namespace MoreRewards.Core;

/// <summary>
/// A long piece of work done a slice at a time, so no single frame pays for all of it. Each step yields true when the
/// job may go on in the same frame if the frame's time allows, or false to stop for this frame.
/// </summary>
public sealed class SlicedJob
{
    private readonly IEnumerator<bool> _steps;

    public SlicedJob(IEnumerable<bool> steps) => _steps = steps.GetEnumerator();

    public bool IsDone { get; private set; }

    /// <summary>Runs steps until the job ends, a step asks to stop, or <paramref name="isSliceSpent"/> says the frame's time is up.</summary>
    /// <returns>Whether the job is done.</returns>
    public bool Advance(Func<bool> isSliceSpent)
    {
        while (!IsDone)
        {
            if (!_steps.MoveNext())
            {
                IsDone = true;

                break;
            }

            if (!_steps.Current || isSliceSpent()) break;
        }

        return IsDone;
    }
}
