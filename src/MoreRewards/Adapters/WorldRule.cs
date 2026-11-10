namespace MoreRewards.Adapters;

/// <summary>
/// Exiled's Toolkit lets a setting with this tag be set per world (Customizable Difficulty picks it
/// for a new world, and the Mods screen changes it for the world being played). Every tagged setting must be watched,
/// or the Toolkit leaves it out.
/// </summary>
internal static class WorldRule
{
    public const string Tag = "ModManager.WorldRule";
}
