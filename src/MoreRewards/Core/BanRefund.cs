using System;

namespace MoreRewards.Core;

/// <summary>What removing a ban pays back: UnbanRefund percent of what the ban cost, rounded down.</summary>
public static class BanRefund
{
    /// <param name="banCost">The ban's price now, used for a ban made before the mod kept what each one cost.</param>
    public static int Of(BanGroup ban, int banCost, int percent) => Math.Max(ban.Paid ?? banCost, 0) * Math.Clamp(percent, 0, 100) / 100;
}
