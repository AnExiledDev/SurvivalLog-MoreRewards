using System;
using System.Linq;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Watches the rewards screen's card list. The game rebuilds it in Reducer_Web_RogueSelect.BuildList (rva 0x2c01bf0)
/// whenever the screen opens, a card is bought, the cards refresh or the lock moves, and the web page draws from it.
/// The hook marks the mod's extra locks as locked, as the game marks its own, lets go of an extra lock whose card was
/// just bought, and keeps the state so the plugin knows which cards are on screen. Static because Harmony hooks are.
/// </summary>
internal static class ListPatch
{
    private const string Doing = "marking the extra locks on the rewards screen";

    /// <summary>The rewards screen's state from the last rebuild; null before the screen first opens.</summary>
    public static State_Web_RogueSelect? State { get; private set; }

    /// <summary>Goes up with every rebuild, so the plugin can tell the page when the screen changed.</summary>
    public static int Version { get; private set; }

    public static void Apply(Harmony harmony) =>
        PatchKit.Patch(harmony, typeof(ListPatch), typeof(Reducer_Web_RogueSelect), "BuildList", new[] { typeof(State_Web_RogueSelect) }, nameof(MarkExtras),
            "hooked the rewards screen's card list", "extra locked cards do not show as locked and the mod's buttons on the cards do not update");

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

            var kept = extras.ToList();

            for (var i = 0; i < __0.List.Count; i++)
            {
                var item = __0.List[i];
                var id = item.Id.Value;

                if (extras.All(held => held.Id != id)) continue;

                if (item.Bought.Value) kept.RemoveAll(held => held.Id == id);
                else item.Pinned.Value = true;
            }

            if (kept.Count != extras.Count) book.SetLocks(kept);
        }
        catch (Exception ex)
        {
            PatchKit.Fail(Doing, ex);
        }
    }
}
