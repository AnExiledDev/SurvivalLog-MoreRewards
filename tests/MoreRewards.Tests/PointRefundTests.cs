using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PointRefundTests
{
    /// <summary>The game's add-points call: the points count as gained.</summary>
    private static RunPoints Add(RunPoints points, int added) => points with { Total = points.Total + added };

    /// <summary>The game's spend call: the points count as spent.</summary>
    private static RunPoints Spend(RunPoints points, int spent) => points with { Spent = points.Spent + spent };

    [Fact]
    public void A_full_refund_after_a_ban_leaves_the_run_totals_where_they_started()
    {
        var start = new RunPoints(500, 120);
        var points = start;

        for (var loop = 0; loop < 10; loop++)
        {
            points = Spend(points, 30);
            points = PointRefund.Undo(Add(points, 30), 30);
        }

        Assert.Equal(start, points);
    }

    [Fact]
    public void A_part_refund_leaves_only_the_kept_part_counted_as_spent()
    {
        var points = Spend(new RunPoints(500, 0), 40);

        points = PointRefund.Undo(Add(points, 20), 20);

        Assert.Equal(new RunPoints(500, 20), points);
    }

    [Fact]
    public void Totals_never_go_below_zero()
    {
        Assert.Equal(new RunPoints(0, 0), PointRefund.Undo(new RunPoints(10, 5), 30));
    }

    [Fact]
    public void Nothing_paid_back_changes_nothing()
    {
        Assert.Equal(new RunPoints(10, 5), PointRefund.Undo(new RunPoints(10, 5), 0));
    }
}
