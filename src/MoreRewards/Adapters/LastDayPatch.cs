using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;

namespace MoreRewards.Adapters;

/// <summary>
/// Keeps a reward with a "toDay" out once its last day has passed. SurvivalPlanningComponent.IsValidTalentId is the
/// game's check that a reward can be offered: GetRandomIdsFromGroup asks it for every reward it draws, and
/// CanKeepPinnedPlan asks it whether a locked reward stays locked. Saying no there means the game never draws the
/// reward, lets go of its lock, and shows it as not on offer, the same as a reward whose needs are not met. The day is
/// the one the draw uses (GameTimeManager.GetDay). Static because Harmony hooks are.
/// </summary>
internal static class LastDayPatch
{
    private const string Doing = "keeping rewards out after their last day, they may still be offered after it";

    private static IReadOnlyDictionary<int, int> _lastDays = new Dictionary<int, int>();
    private static PatchKit? _hooks;

    public static void Apply(PatchKit hooks)
    {
        _hooks = hooks;
        hooks.Patch(typeof(LastDayPatch), typeof(SurvivalPlanningComponent), "IsValidTalentId", new[] { typeof(int), typeof(bool) }, postfix: nameof(KeepOut),
            done: "hooked the check for which rewards can be offered", consequence: "a reward with a last day (\"toDay\") is still offered after that day");
    }

    /// <summary>The last day of each reward level in the rewards file now; empty while the mod is off.</summary>
    public static void Use(IReadOnlyDictionary<int, int> lastDays) => _lastDays = lastDays;

    /// <summary>Runs for every reward the game checks, so it looks the reward up before it reads the day.</summary>
    private static void KeepOut(int talentId, ref bool __result)
    {
        try
        {
            if (!__result || _lastDays.Count == 0 || !_lastDays.TryGetValue(talentId, out var last)) return;

            if (RunGame.Day() > last) __result = false;
        }
        catch (Exception ex)
        {
            _hooks?.Fail(Doing, ex);
        }
    }
}
