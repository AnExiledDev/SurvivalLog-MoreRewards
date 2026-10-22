using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// The screen settings as the config has them now, and the run's book of locks, bans and shop buys. Settings are set
/// on the main thread at start and after each change; static because the hooks read them.
/// </summary>
internal static class LiveScreen
{
    public static ScreenSettings Settings { get; set; } = ScreenSettings.Game;

    public static readonly RunBook Book = new(new GameCounters());
}
