using System;
using System.Collections.Generic;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Works out the perks (<see cref="Perks"/>) of the rewards the player owns and hands them to <see cref="LiveScreen"/>.
/// It only adds them up again when the run, the owned rewards or the rewards file changed: a check is a component
/// lookup and a list count. Checked once a second, and at once when the rewards screen opens, a reward is bought or a
/// card is banned. Static because the hooks call it.
/// </summary>
internal static class PerkWatch
{
    private const float CheckSeconds = 1f;

    private static float _nextCheckAt;
    private static (IntPtr Run, int Owned, object? Grants, bool IsOn) _seen;

    /// <summary>Every More Rewards effect the rewards file gives, set at start.</summary>
    public static Func<IReadOnlyList<MrGrant>> Grants { get; set; } = () => Array.Empty<MrGrant>();

    public static void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Refresh();
    }

    public static void Refresh()
    {
        var planning = LiveScreen.IsOn ? RunGame.Planning() : null;
        var grants = Grants();
        var seen = (planning?.Pointer ?? IntPtr.Zero, planning?._SaveCache_k__BackingField?.Count ?? -1, (object?)grants, LiveScreen.IsOn);

        if (seen.Equals(_seen)) return;

        _seen = seen;
        LiveScreen.UsePerks(planning == null ? Perks.None : Perks.Of(grants, planning.HasActivated));
    }
}
