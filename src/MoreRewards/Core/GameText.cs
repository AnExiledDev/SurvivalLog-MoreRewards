namespace MoreRewards.Core;

/// <summary>
/// The game loads its English and Chinese text after its tables, and each only once. The rewards are read and written
/// only after both: read sooner, every game reward shows its text key (Talent_Name_30011) in the Rewards tab. The names
/// from the rewards file go into the same text, so they wait too.
/// </summary>
public static class GameText
{
    public static bool IsLoaded(object? english, object? chinese) => english != null && chinese != null;
}
