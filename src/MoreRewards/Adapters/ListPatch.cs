using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Watches the rewards screen's card list. The game rebuilds it in Reducer_Web_RogueSelect.BuildList (rva 0x2c3c1d0)
/// whenever the screen opens, a card is bought, the cards refresh or the lock moves, and the web page draws from it.
/// The hook marks the mod's extra locks as locked, as the game marks its own, lets go of an extra lock whose card was
/// just bought, and keeps the state so the plugin knows which cards are on screen. Before the rebuild it puts back a
/// screen <see cref="KeepScreen"/> asked to keep. Static because Harmony hooks are.
/// </summary>
internal static class ListPatch
{
    private const string Doing = "marking the extra locks on the rewards screen";
    private const string DoingKeep = "keeping the rewards screen after a refresh found nothing new";

    private static List<int>? _kept;

    /// <summary>The rewards screen's state from the last rebuild; null before the screen first opens.</summary>
    public static State_Web_RogueSelect? State { get; private set; }

    /// <summary>Goes up with every rebuild, so the plugin can tell the page when the screen changed.</summary>
    public static int Version { get; private set; }

    public static void Apply(Harmony harmony) =>
        PatchKit.Patch(harmony, typeof(ListPatch), typeof(Reducer_Web_RogueSelect), "BuildList", new[] { typeof(State_Web_RogueSelect) }, nameof(MarkExtras),
            "hooked the rewards screen's card list", "extra locked cards do not show as locked and the mod's buttons on the cards do not update", nameof(PutBack));

    /// <summary>The run ended: the state belongs to a screen the game has let go.</summary>
    public static void Forget()
    {
        State = null;
        _kept = null;
    }

    /// <summary>
    /// The screen's cards now, to be put back when the next rebuild finds the list emptied (a refresh that found nothing
    /// new). False lets go of any screen kept earlier. Outside a rebuild of an emptied list the kept cards are dropped
    /// unused, so they can never come back on a later day.
    /// </summary>
    public static void KeepScreen(bool keep) => _kept = keep && State?.CachedSurvivalPlanIds is { } shown ? OfferPatch.FromGame(shown) : null;

    /// <summary>The game's refresh just emptied the list; it gets the cards it had before.</summary>
    private static void PutBack(State_Web_RogueSelect __0)
    {
        try
        {
            var kept = _kept;

            _kept = null;

            if (kept == null || __0?.CachedSurvivalPlanIds is not { } list || list.Count != 0) return;

            foreach (var id in kept) list.Add(id);

            PatchKit.Log($"a refresh found nothing new, the {kept.Count} cards on screen stay");
        }
        catch (Exception ex)
        {
            PatchKit.Fail(DoingKeep, ex);
        }
    }

    private static void MarkExtras(State_Web_RogueSelect __0)
    {
        try
        {
            if (__0?.List == null) return;

            State = __0;
            Version++;

            var book = LiveScreen.Book;
            var extras = book.Locks();

            if (extras.Count == 0) return;

            var kept = new List<RewardLock>(extras);
            var list = __0.List;
            var count = list.Count;

            for (var i = 0; i < count; i++)
            {
                var item = list[i];
                var id = item.Id.Value;

                if (IndexOf(extras, id) < 0) continue;

                if (item.Bought.Value)
                {
                    for (var at = IndexOf(kept, id); at >= 0; at = IndexOf(kept, id)) kept.RemoveAt(at);
                }
                else
                {
                    item.Pinned.Value = true;
                }
            }

            if (kept.Count != extras.Count) book.SetLocks(kept);
        }
        catch (Exception ex)
        {
            PatchKit.Fail(Doing, ex);
        }
    }

    /// <returns>Where the lock on <paramref name="id"/> is in <paramref name="locks"/>, or -1. A plain loop: the hook runs on every rebuild.</returns>
    private static int IndexOf(IReadOnlyList<RewardLock> locks, int id)
    {
        for (var i = 0; i < locks.Count; i++)
        {
            if (locks[i].Id == id) return i;
        }

        return -1;
    }
}
