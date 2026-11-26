using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// A reward hands out one buff, so a reward that gives several things hands out the first and this adds the rest
/// alongside it: whenever anything gets a reward's buff, it gets that reward's companion buffs from the same releaser,
/// and when the reward's buff is removed, so are they. Static because a Harmony postfix has nowhere else to look.
/// </summary>
internal static class CompanionBuffs
{
    private const string Doing = "adding or removing a reward's extra buffs";
    private const string Consequence = "a reward that gives more than one thing gives only its first";

    private static readonly Dictionary<int, IReadOnlyList<int>> Riders = new();
    private static PatchKit? _hooks;
    private static bool _isHooked;

    /// <summary>The buff hooks go in the first time a reward has companions, since adding a buff is a busy call.</summary>
    public static void Apply(PatchKit hooks) => _hooks = hooks;

    /// <summary>Replaces the companions; called each time the rewards files are written into the tables.</summary>
    public static void Use(IReadOnlyList<Companion> companions)
    {
        Riders.Clear();

        foreach (var companion in companions) Riders[companion.LevelBuff] = companion.Buffs;

        if (Riders.Count > 0 && !_isHooked && _hooks is { } hooks) Hook(hooks);
    }

    /// <summary>Taking the extra buffs away is hooked only once giving them is.</summary>
    private static void Hook(PatchKit hooks)
    {
        _isHooked = true;

        if (!hooks.Patch(typeof(CompanionBuffs), typeof(BuffComponent), nameof(BuffComponent.AddBuff), new[] { typeof(long), typeof(int) }, postfix: nameof(AfterAdd), consequence: Consequence)) return;

        hooks.Patch(typeof(CompanionBuffs), typeof(BuffComponent), nameof(BuffComponent.RemoveBuff), new[] { typeof(int) }, postfix: nameof(AfterRemove), consequence: Consequence);
    }

    /// <summary>Never throws into the game. A companion is never a reward's buff, so adding one does not cascade.</summary>
    private static void AfterAdd(BuffComponent __instance, long __0, int __1)
    {
        if (!Riders.TryGetValue(__1, out var riders)) return;

        try
        {
            foreach (var rider in riders) __instance.AddBuff(__0, rider);
        }
        catch (Exception ex)
        {
            _hooks?.Fail(Doing, ex);
        }
    }

    private static void AfterRemove(BuffComponent __instance, int __0)
    {
        if (!Riders.TryGetValue(__0, out var riders)) return;

        try
        {
            foreach (var rider in riders) __instance.RemoveBuff(rider);
        }
        catch (Exception ex)
        {
            _hooks?.Fail(Doing, ex);
        }
    }
}
