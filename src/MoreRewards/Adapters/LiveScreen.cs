using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// The screen settings as the config has them now, and the run's book of locks, bans and shop buys. Settings are set
/// on the main thread at start and after each change; static because the hooks read them.
/// </summary>
internal static class LiveScreen
{
    public static ScreenSettings Settings { get; set; } = ScreenSettings.Game;

    /// <summary>
    /// Whether the mod is on (General.Enabled). While it is off the hooks leave the game's rewards and screen as they
    /// are and the page shows none of the mod's buttons.
    /// </summary>
    public static bool IsOn { get; set; }

    public static readonly RunBook Book = new(new GameCounters());

    /// <summary>Goes up whenever a setting the rewards page draws from changes, so the page is told again.</summary>
    public static int Version { get; private set; }

    public static void Changed() => Version++;
}
