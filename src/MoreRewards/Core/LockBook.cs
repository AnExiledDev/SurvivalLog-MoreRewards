using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>What a click on a card's lock does.</summary>
public enum LockMove
{
    /// <summary>Unlock the game's own locked card.</summary>
    UnpinGame,

    /// <summary>Unlock one of the mod's extra locks.</summary>
    RemoveExtra,

    /// <summary>Lock the card with the game's own lock, which is free.</summary>
    PinGame,

    /// <summary>Lock the card with one of the mod's extra locks.</summary>
    AddExtra,

    /// <summary>Every lock is in use.</summary>
    Full,
}

/// <summary>
/// The game has one lock (the pinned plan) and keeps it itself; the mod adds the rest. The game's lock is always used
/// first, so with one lock in use the game works exactly as it does without the mod.
/// </summary>
public static class LockBook
{
    /// <param name="gamePin">The game's locked card, 0 for none.</param>
    public static LockMove Click(int id, int gamePin, IReadOnlyList<RewardLock> extras, int maxLocked)
    {
        if (gamePin > 0 && id == gamePin) return LockMove.UnpinGame;

        if (extras.Any(extra => extra.Id == id)) return LockMove.RemoveExtra;

        if (gamePin <= 0) return LockMove.PinGame;

        return 1 + extras.Count < maxLocked ? LockMove.AddExtra : LockMove.Full;
    }

    public static IReadOnlyList<RewardLock> Without(IReadOnlyList<RewardLock> extras, int id) => extras.Where(extra => extra.Id != id).ToList();

    public static IReadOnlyList<RewardLock> With(IReadOnlyList<RewardLock> extras, RewardLock added) => Without(extras, added.Id).Append(added).ToList();
}
