using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>An item a supply crate puts in the bag, and how many.</summary>
public readonly record struct CrateItem(int Item, int Count);

/// <summary>A lottery's range of survival points, both ends included.</summary>
public readonly record struct PointRange(int Least, int Most);

/// <summary>What buying a reward pays out at once: lottery points, supply crate items, and the free bans of a reward that can be bought again.</summary>
public static class Payouts
{
    public static IReadOnlyList<PointRange> LotteriesOf(IEnumerable<MrGrant> grants, int rewardId) =>
        Given(grants, rewardId, RewardPlan.Lottery).Select(Range).ToList();

    /// <summary>Every item of every crate the reward gives. An item with no count, or a count below 1, comes once.</summary>
    public static IReadOnlyList<CrateItem> CratesOf(IEnumerable<MrGrant> grants, int rewardId) =>
        Given(grants, rewardId, RewardPlan.SupplyCrate).SelectMany(Items).ToList();

    /// <summary>How many free bans the reward gives.</summary>
    public static int BanTokensOf(IEnumerable<MrGrant> grants, int rewardId) =>
        Given(grants, rewardId, RewardPlan.BanTokens).Sum(grant => Math.Max(Perks.Whole(grant.Fields, RewardPlan.CountField), 0));

    /// <summary>A whole number from the range, both ends included; <paramref name="next"/> is Random.Next(min, maxExclusive).</summary>
    public static int Draw(PointRange range, Func<int, int, int> next) => next(range.Least, range.Most + 1);

    private static IEnumerable<MrGrant> Given(IEnumerable<MrGrant> grants, int rewardId, string effect) =>
        grants.Where(grant => grant.RewardId == rewardId && grant.Effect == effect);

    private static PointRange Range(MrGrant grant)
    {
        var least = Math.Max(Perks.Whole(grant.Fields, RewardPlan.LeastField), 0);

        return new PointRange(least, Math.Max(Perks.Whole(grant.Fields, RewardPlan.MostField), least));
    }

    private static IEnumerable<CrateItem> Items(MrGrant grant)
    {
        var items = Numbers(grant.Fields, RewardPlan.ItemsField);
        var counts = Numbers(grant.Fields, RewardPlan.CountsField);

        return items.Select((item, k) => new CrateItem(item, k < counts.Count ? Math.Max(counts[k], 1) : 1)).Where(crate => crate.Item > 0);
    }

    private static IReadOnlyList<int> Numbers(IReadOnlyList<FieldSet> fields, string field) =>
        fields.LastOrDefault(set => set.Field == field)?.Value is FieldValue.Numbers numbers ? numbers.Values : Array.Empty<int>();
}
