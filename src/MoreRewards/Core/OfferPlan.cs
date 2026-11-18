using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>What shapes an offer beyond the game's draw.</summary>
/// <param name="Count">How many cards the player asked for.</param>
/// <param name="GamePin">The game's locked card, 0 for none, and <paramref name="GamePinSlot"/> its slot.</param>
/// <param name="Extras">The mod's extra locks, all still worth keeping.</param>
/// <param name="IsUnwanted">True for a card the player banned.</param>
/// <param name="NoRepeat">A refresh never brings back a card that was just on screen.</param>
/// <param name="Always">The "alwaysOffered" rewards the game would offer now, in file order; null for none.</param>
public sealed record OfferAsk(int Count, int GamePin, int GamePinSlot, IReadOnlyList<RewardLock> Extras, Func<int, bool> IsUnwanted, bool NoRepeat = false, IReadOnlyList<int>? Always = null)
{
    public IReadOnlyList<int> AlwaysOffered => Always ?? Array.Empty<int>();
}

/// <summary>
/// Builds the end-of-day offer from the game's: banned cards come out, the day's groups fill the gaps,
/// and every locked card (the game's one and the mod's extras) sits in its own slot. The game's pick is kept
/// where it can be, so with nothing locked or passed on this is <see cref="OfferFill"/> alone. An "alwaysOffered"
/// reward takes the first free slots, ahead of the drawn cards, and always leaves room for at least one drawn card.
/// </summary>
public static class OfferPlan
{
    /// <summary>How many unwanted cards one draw may turn back before the group counts as spent.</summary>
    public const int RedrawBudget = 10;

    /// <param name="groups">The day's groups; null on a day the game gives a fixed list, where only the extra locks are added.</param>
    public static IReadOnlyList<int> Day(IReadOnlyList<int> gameOffer, OfferAsk ask, IReadOnlyList<RewardGroup>? groups, RewardDraw draw)
    {
        var locks = Locks(gameOffer, ask);

        if (groups == null) return Place(Free(gameOffer, Ids(locks), _ => false), locks);

        var always = Always(ask, locks);
        var held = Ids(locks).Concat(always).ToList();
        var target = Math.Max(0, ask.Count - held.Count);
        var free = Free(gameOffer, held, ask.IsUnwanted).Take(target).ToList();

        free.AddRange(OfferFill.TopUp(free, target, groups, Wanted(draw, held, ask.IsUnwanted)));

        return Place(always.Concat(free).ToList(), locks);
    }

    /// <summary>
    /// A refresh: as <see cref="OfferFill.TopUpRefresh"/>, and when nothing new turns up the game's answer for that
    /// case is kept (nothing, or only its locked card), which the game treats as a refresh that costs nothing. The
    /// "alwaysOffered" cards come back on a refresh too, but they are not new, so they alone never make it count.
    /// </summary>
    public static IReadOnlyList<int> Refresh(IReadOnlyList<int> gameOffer, OfferAsk ask, IReadOnlyList<RewardGroup>? groups, RewardDraw unseen, RewardDraw seenBefore)
    {
        var locks = Locks(gameOffer, ask);
        var nothingNew = gameOffer.Where(id => id == ask.GamePin).ToList();

        if (groups == null) return Place(Free(gameOffer, Ids(locks), _ => false), locks);

        var always = Always(ask, locks);
        var held = Ids(locks).Concat(always).ToList();
        var target = Math.Max(0, ask.Count - held.Count);
        var free = Free(gameOffer, held, ask.IsUnwanted).Take(target).ToList();
        RewardDraw again = ask.NoRepeat ? (_, _) => null : Wanted(seenBefore, held, ask.IsUnwanted);

        free.AddRange(OfferFill.TopUpRefresh(free, target, groups, Wanted(unseen, held, ask.IsUnwanted), again));

        return free.Count == 0 ? nothingNew : Place(always.Concat(free).ToList(), locks);
    }

    /// <summary>
    /// Whether a refresh that turned up nothing new leaves the screen as it was. The game does that itself while its own
    /// lock is held (it calls the refresh failed, at no cost); with no lock of its own it shows an empty screen, which
    /// would hide the mod's extra locks and the "alwaysOffered" cards until the next day.
    /// </summary>
    /// <param name="refreshed">What <see cref="Refresh"/> handed back to the game.</param>
    public static bool KeepsScreen(IReadOnlyList<int> refreshed, OfferAsk ask) => refreshed.Count == 0 && ask.GamePin <= 0 && (ask.Extras.Count > 0 || ask.AlwaysOffered.Count > 0);

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

        var wanted = Wanted(draw, Ids(Locks(current, ask)), ask.IsUnwanted);
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

    /// <summary>
    /// The "alwaysOffered" cards that get a slot: not banned, not locked already, and few enough to leave at least one
    /// slot for a drawn card.
    /// </summary>
    private static List<int> Always(OfferAsk ask, IReadOnlyList<RewardLock> locks) =>
        ask.AlwaysOffered.Where(id => locks.All(held => held.Id != id) && !ask.IsUnwanted(id)).Distinct().Take(Math.Max(0, ask.Count - locks.Count - 1)).ToList();

    private static List<int> Ids(IReadOnlyList<RewardLock> locks) => locks.Select(held => held.Id).ToList();

    /// <summary>The game's cards that stay: none already held in a slot of its own, and none the player passed on.</summary>
    private static List<int> Free(IReadOnlyList<int> gameOffer, IReadOnlyList<int> held, Func<int, bool> isUnwanted) =>
        gameOffer.Where(id => !held.Contains(id) && !isUnwanted(id)).Distinct().ToList();

    /// <summary>Each locked card goes back to its slot, lowest slot first, or to the end when the offer is shorter.</summary>
    private static IReadOnlyList<int> Place(List<int> free, IReadOnlyList<RewardLock> locks)
    {
        foreach (var held in locks.OrderBy(held => held.Slot)) free.Insert(Math.Clamp(held.Slot, 0, free.Count), held.Id);

        return free;
    }

    /// <summary>A draw that never gives a held card or one the player passed on; it asks again a few times before giving up.</summary>
    private static RewardDraw Wanted(RewardDraw draw, IReadOnlyList<int> held, Func<int, bool> isUnwanted) => (group, offered) =>
    {
        var avoid = offered.Concat(held).ToList();

        for (var tries = 0; tries < RedrawBudget; tries++)
        {
            if (draw(group, avoid) is not int id || avoid.Contains(id)) return null;

            if (!isUnwanted(id)) return id;

            avoid.Add(id);
        }

        return null;
    };
}
