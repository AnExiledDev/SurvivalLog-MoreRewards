using System;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// The screen settings as the config has them now with what the player's rewards add, and the run's book of locks,
/// bans and shop buys. Settings are set on the main thread at start and after each change; static because the hooks
/// read them.
/// </summary>
internal static class LiveScreen
{
    private static ScreenSettings _fromConfig = ScreenSettings.Game;

    /// <summary>The config's settings with the rewards' perks added (<see cref="ScreenSettings.With"/>).</summary>
    public static ScreenSettings Settings { get; private set; } = ScreenSettings.Game;

    /// <summary>What the rewards the player owns add; none while the mod is off or no run is loaded.</summary>
    public static Perks Perks { get; private set; } = Perks.None;

    /// <summary>
    /// Whether the mod is on (General.Enabled). While it is off the hooks leave the game's rewards and screen as they
    /// are and the page shows none of the mod's buttons.
    /// </summary>
    public static bool IsOn { get; set; }

    public static readonly RunBook Book = new(new GameCounters());

    /// <summary>Goes up whenever a setting the rewards page draws from changes, so the page is told again.</summary>
    public static int Version { get; private set; }

    /// <summary>The perks, the free bans left or the free refreshes left changed.</summary>
    public static event Action? PerksChanged;

    public static int FreeBansLeft => Math.Max(Perks.BanTokens + Book.FreeBansBought - Book.FreeBansUsed, 0);

    /// <summary>Whether the next refresh is free, from the day's free refreshes.</summary>
    public static bool IsFreeRefreshReady => IsOn && Perks.FreeRefreshes > Book.FreeRefreshesUsed;

    public static void Changed() => Version++;

    public static void UseConfig(ScreenSettings settings)
    {
        _fromConfig = settings;
        Rebuild();
    }

    public static void UsePerks(Perks perks)
    {
        if (perks == Perks) return;

        Perks = perks;
        Rebuild();
        PerksChanged?.Invoke();
    }

    public static void UseFreeBan()
    {
        Book.UseFreeBan();
        Rebuild();
        PerksChanged?.Invoke();
    }

    public static void AddFreeBans(int count)
    {
        if (count <= 0) return;

        Book.AddFreeBans(count);
        Rebuild();
        PerksChanged?.Invoke();
    }

    public static void UseFreeRefresh()
    {
        Book.UseFreeRefresh();
        PerksChanged?.Invoke();
    }

    /// <summary>The day's rewards were drawn, so the day's free refreshes start over.</summary>
    public static void NewDay()
    {
        Book.NewDayOfRefreshes();
        PerksChanged?.Invoke();
    }

    private static void Rebuild()
    {
        Settings = _fromConfig.With(Perks, FreeBansLeft);
        Changed();
    }
}
