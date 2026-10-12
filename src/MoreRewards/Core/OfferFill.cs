using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>One of the day's reward groups and how many of the game's own rewards it gives.</summary>
public readonly record struct RewardGroup(int Id, int Amount);

/// <summary>
/// Draws one reward from a group, never one already in <paramref name="offeredSoFar"/>; null when the group has none
/// left to give.
/// </summary>
public delegate int? RewardDraw(int groupId, IReadOnlyList<int> offeredSoFar);

/// <summary>
/// Tops the game's end-of-day rewards up to the number the player asked for. The extra rewards come from the day's
/// own groups in the day's own mix: a group that gives two of the game's three gives two of every three extras. A
/// group with nothing left drops out and the others carry on.
/// </summary>
public static class OfferFill
{
    /// <returns>The rewards to add after <paramref name="offered"/>, in order; fewer when the groups run dry.</returns>
    public static IReadOnlyList<int> TopUp(IReadOnlyList<int> offered, int target, IReadOnlyList<RewardGroup> groups, RewardDraw draw)
    {
        var shown = offered.ToList();
        var added = new List<int>();

        Fill(shown, added, target, Rotation(groups), draw);

        return added;
    }

    /// <summary>
    /// A refresh first draws rewards the player has not been shown yet, then fills the rest with ones shown before. The
    /// game charges for a refresh only when it draws something (SurvivalResultsManager.OnRefreshSurvivalPlans, rva
    /// 0x2699b60), and its own draw is all new rewards. So when neither the game nor the first pass found a new one,
    /// the refresh is left empty and free, as in the game, instead of charging again for the rewards on screen.
    /// </summary>
    /// <param name="offered">The game's refresh, every one of them new.</param>
    public static IReadOnlyList<int> TopUpRefresh(IReadOnlyList<int> offered, int target, IReadOnlyList<RewardGroup> groups, RewardDraw unseen, RewardDraw seenBefore)
    {
        var shown = offered.ToList();
        var added = new List<int>();
        var rotation = Rotation(groups);

        Fill(shown, added, target, rotation, unseen);

        if (shown.Count == 0) return added;

        Fill(shown, added, target, rotation, seenBefore);

        return added;
    }

    private static void Fill(List<int> shown, List<int> added, int target, IReadOnlyList<int> rotation, RewardDraw draw)
    {
        var dry = new HashSet<int>();
        var groupCount = rotation.Distinct().Count();

        for (var turn = 0; shown.Count < target && dry.Count < groupCount; turn++)
        {
            var group = rotation[turn % rotation.Count];

            if (dry.Contains(group)) continue;

            if (draw(group, shown) is not int reward || shown.Contains(reward))
            {
                dry.Add(group);

                continue;
            }

            shown.Add(reward);
            added.Add(reward);
        }
    }

    /// <summary>Each group's id once per reward it gives, so walking the list round and round keeps the day's mix.</summary>
    private static IReadOnlyList<int> Rotation(IReadOnlyList<RewardGroup> groups) =>
        groups.Where(group => group.Amount > 0).SelectMany(group => Enumerable.Repeat(group.Id, group.Amount)).ToList();
}
