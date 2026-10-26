using System;

namespace MoreRewards.Core;

/// <summary>A run's survival point totals: all points gained, and all points spent.</summary>
public readonly record struct RunPoints(int Total, int Spent);

/// <summary>
/// Paying points back is the game's add-points call, which also counts them as gained. A refund undoes a spend
/// instead, so the run's totals read as if the points had never left: gained goes back down and spent goes down by as
/// much. Without this, banning and unbanning again and again would raise both totals, and the achievements that count them.
/// </summary>
public static class PointRefund
{
    /// <param name="afterAdd">The totals after the game's add-points call paid <paramref name="points"/> back.</param>
    public static RunPoints Undo(RunPoints afterAdd, int points)
    {
        var back = Math.Max(points, 0);

        return new RunPoints(Math.Max(afterAdd.Total - back, 0), Math.Max(afterAdd.Spent - back, 0));
    }
}
