using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>What shapes an offer beyond the game's own draw.</summary>
/// <param name="Count">How many cards the player asked for.</param>
/// <param name="GamePin">The game's locked card, 0 for none, and <paramref name="GamePinSlot"/> its slot.</param>
/// <param name="Extras">The mod's extra locks, all still worth keeping.</param>
/// <param name="IsUnwanted">True for a card the player banned.</param>
/// <param name="NoRepeat">A refresh never brings back a card that was just on screen.</param>
public sealed record OfferAsk(int Count, int GamePin, int GamePinSlot, IReadOnlyList<RewardLock> Extras, Func<int, bool> IsUnwanted, bool NoRepeat = false);

/// <summary>
/// Builds the end-of-day offer from the game's: banned cards come out, the day's groups fill the gaps,
/// and every locked card (the game's one and the mod's extras) sits in its own slot. The game's own pick is kept
/// where it can be, so with nothing locked or passed on this is <see cref="OfferFill"/> alone.
/// </summary>
public static class OfferPlan
{
    /// <summary>How many unwanted cards one draw may turn back before the group counts as spent.</summary>
    public const int RedrawBudget = 10;

    /// <param name="groups">The day's groups; null on a day the game gives a fixed list, where only the extra locks are added.</param>
    public static IReadOnlyList<int> Day(IReadOnlyList<int> gameOffer, OfferAsk ask, IReadOnlyList<RewardGroup>? groups, RewardDraw draw)
    {
        var locks = Locks(gameOffer, ask);

        if (groups == null) return Place(Free(gameOffer, locks, _ => false), locks);

        var target = Math.Max(0, ask.Count - locks.Count);
        var free = Free(gameOffer, locks, ask.IsUnwanted).Take(target).ToList();

        free.AddRange(OfferFill.TopUp(free, target, groups, Wanted(draw, locks, ask.IsUnwanted)));

        return Place(free, locks);
    }

    /// <summary>
    /// A refresh: as <see cref="OfferFill.TopUpRefresh"/>, and when nothing new turns up the game's answer for that
    /// case is kept (nothing, or only its locked card), which the game treats as a refresh that costs nothing.
    /// </summary>
    public static IReadOnlyList<int> Refresh(IReadOnlyList<int> gameOffer, OfferAsk ask, IReadOnlyList<RewardGroup>? groups, RewardDraw unseen, RewardDraw seenBefore)
    {
        var locks = Locks(gameOffer, ask);
        var nothingNew = gameOffer.Where(id => id == ask.GamePin).ToList();

        if (groups == null) return Place(Free(gameOffer, locks, _ => false), locks);

        var target = Math.Max(0, ask.Count - locks.Count);
        var free = Free(gameOffer, locks, ask.IsUnwanted).Take(target).ToList();
        RewardDraw again = ask.NoRepeat ? (_, _) => null : Wanted(seenBefore, locks, ask.IsUnwanted);

        free.AddRange(OfferFill.TopUpRefresh(free, target, groups, Wanted(unseen, locks, ask.IsUnwanted), again));

        return free.Count == 0 ? nothingNew : Place(free, locks);
    }

    /// <summary>
    /// Swaps out the card at <paramref name="slot"/> (the player banned it) for one wanted card from the
    /// day's groups, in the same slot. Nothing on screen or locked comes back. When the groups have nothing left, or the
    /// day has a fixed list, the card is only taken away.
    /// </summary>
    public static IReadOnlyList<int> Replace(IReadOnlyList<int> current, int slot, OfferAsk ask, IReadOnlyList<RewardGroup>? groups, RewardDraw draw)
    {
        if (slot < 0 || slot >= current.Count) return current;

        var offer = current.ToList();
        var onScreen = current.ToList();

        offer.RemoveAt(slot);

        if (groups == null) return offer;

        var wanted = Wanted(draw, Locks(current, ask), ask.IsUnwanted);
        var rotation = groups.Where(group => group.Amount > 0).Select(group => group.Id).Distinct().ToList();

        for (var turn = 0; turn < rotation.Count; turn++)
        {
            if (wanted(rotation[(slot + turn) % rotation.Count], onScreen) is not int id) continue;

            offer.Insert(slot, id);

            break;
        }

        return offer;
    }

    /// <summary>The game's lock counts only when the game kept it in its offer; it decides whether the card can stay.</summary>
    private static IReadOnlyList<RewardLock> Locks(IReadOnlyList<int> gameOffer, OfferAsk ask)
    {
        var locks = ask.Extras.Where(extra => extra.Id != ask.GamePin).ToList();

        if (ask.GamePin > 0 && gameOffer.Contains(ask.GamePin)) locks.Add(new RewardLock(ask.GamePin, ask.GamePinSlot));

        return locks;
    }

    private static List<int> Free(IReadOnlyList<int> gameOffer, IReadOnlyList<RewardLock> locks, Func<int, bool> isUnwanted) =>
        gameOffer.Where(id => locks.All(held => held.Id != id) && !isUnwanted(id)).Distinct().ToList();

    /// <summary>Each locked card goes back to its slot, lowest slot first, or to the end when the offer is shorter.</summary>
    private static IReadOnlyList<int> Place(List<int> free, IReadOnlyList<RewardLock> locks)
    {
        foreach (var held in locks.OrderBy(held => held.Slot)) free.Insert(Math.Clamp(held.Slot, 0, free.Count), held.Id);

        return free;
    }

    /// <summary>A draw that never gives a locked card or one the player passed on; it asks again a few times before giving up.</summary>
    private static RewardDraw Wanted(RewardDraw draw, IReadOnlyList<RewardLock> locks, Func<int, bool> isUnwanted) => (group, offered) =>
    {
        var avoid = offered.Concat(locks.Select(held => held.Id)).ToList();

        for (var tries = 0; tries < RedrawBudget; tries++)
        {
            if (draw(group, avoid) is not int id || avoid.Contains(id)) return null;

            if (!isUnwanted(id)) return id;

            avoid.Add(id);
        }

        return null;
    };
}
