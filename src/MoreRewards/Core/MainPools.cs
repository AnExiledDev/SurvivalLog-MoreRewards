using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>
/// The pool each day draws most of its rewards from. Every day draws from a few pools: one big one with most of the
/// rewards, a small one or two (often a single reward), and pool 1005 last, which tops the list up. The big one is the
/// one with the most rewards in it, so that is the day's main pool. "anyDay" puts a reward in every day's main pool, so
/// it can come up on any day. The Rewards tab works this out the same way.
/// </summary>
public static class MainPools
{
    /// <summary>The main pool of each day, each pool once, in the order the days first use them.</summary>
    /// <param name="size">How many rewards a pool holds.</param>
    public static IReadOnlyList<int> Of(IEnumerable<DayEntry> days, Func<int, int> size)
    {
        var main = new List<int>();

        foreach (var day in days)
        {
            var pool = MainOf(day, size);

            if (pool != 0 && !main.Contains(pool)) main.Add(pool);
        }

        return main;
    }

    /// <returns>The day's pool with the most rewards that it draws at least one from, the first on a tie; 0 when it draws from none.</returns>
    public static int MainOf(DayEntry day, Func<int, int> size)
    {
        var best = 0;
        var bestSize = -1;

        foreach (var pool in day.Pools.Where(pool => pool.Amount > 0))
        {
            var poolSize = size(pool.Pool);

            if (poolSize <= bestSize) continue;

            best = pool.Pool;
            bestSize = poolSize;
        }

        return best;
    }

    /// <summary>The days as they draw once the file's day changes are in: a changed day draws from its new pools.</summary>
    public static IEnumerable<DayEntry> Days(IReadOnlyList<DayEntry> gameDays, IReadOnlyList<DayEdit> fileDays) =>
        gameDays.Select(day => fileDays.FirstOrDefault(edit => edit.Day == day.Day) is { } edit ? new DayEntry(day.Day, edit.Pools.Zip(edit.Amounts, (pool, amount) => new DayPool(pool, amount)).ToList()) : day);
}
