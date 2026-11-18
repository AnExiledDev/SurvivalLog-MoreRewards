using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>
/// What the rewards the player owns add to the rewards screen and the interest: more cards, more locks, free refreshes
/// a day, free bans, and a higher interest rate and limit. Each is the sum over every owned reward that gives it.
/// </summary>
public sealed record Perks(int MoreCards, int MoreLocks, int FreeRefreshes, int BanTokens, int RatePercent, int CapAdd)
{
    public static readonly Perks None = new(0, 0, 0, 0, 0, 0);

    public static Perks Of(IEnumerable<MrGrant> grants, Func<int, bool> isOwned)
    {
        var owned = grants.Where(grant => isOwned(grant.RewardId)).ToList();

        int Sum(string effect, string field) => owned.Where(grant => grant.Effect == effect).Sum(grant => Whole(grant.Fields, field));

        return new Perks(
            Sum(RewardPlan.MoreCards, RewardPlan.CountField),
            Sum(RewardPlan.MoreLocks, RewardPlan.CountField),
            Sum(RewardPlan.FreeRefresh, RewardPlan.CountField),
            Sum(RewardPlan.BanTokens, RewardPlan.CountField),
            Sum(RewardPlan.Investor, RewardPlan.RateField),
            Sum(RewardPlan.Investor, RewardPlan.CapField));
    }

    /// <summary>The last value the reward sets for <paramref name="field"/>, rounded to a whole number; 0 when it sets none.</summary>
    public static int Whole(IReadOnlyList<FieldSet> fields, string field) =>
        fields.LastOrDefault(set => set.Field == field)?.Value is FieldValue.Number number ? (int)Math.Round(number.Value) : 0;
}
