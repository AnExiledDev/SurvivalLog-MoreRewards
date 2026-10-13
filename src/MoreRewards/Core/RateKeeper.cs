namespace MoreRewards.Core;

/// <summary>
/// A strengthening value to write on the character's interest attribute, in the game's units, with the base it was
/// worked out from. <see cref="IsPutBack"/> when the mod had written one before and it was gone.
/// </summary>
public readonly record struct RateWrite(int Strengthening, int BaseUnits, bool IsPutBack);

/// <summary>
/// Keeps the character's interest attribute at the mod's rate. The attribute is a base plus a strengthening value, and
/// the game totals the two. Saves keep only the base (AttributeComponent.SaveGame, rva 0x2de1950), so the mod sets the
/// strengthening to whatever brings the total to its rate: a save never holds the mod's rate, and without the mod the
/// game's rate is back. A character that is made or loaded starts with no strengthening (AttributeComponent.OnCreate,
/// rva 0x2de1540), so the rate is written again.
/// </summary>
public sealed class RateKeeper
{
    private bool _hasWritten;

    public RateKeeper(int rate)
    {
        Rate = rate;
    }

    /// <summary>The rate to keep, in percent.</summary>
    public int Rate { get; }

    /// <param name="baseUnits">The attribute's base, the part the save keeps.</param>
    /// <param name="strengthening">The attribute's strengthening value now.</param>
    /// <param name="scaling">The game's units per 1 (GameKey.Attr_ScalingRatio, 1000).</param>
    /// <returns>The strengthening to write, or null when the attribute already totals the rate.</returns>
    public RateWrite? Next(int baseUnits, int strengthening, float scaling)
    {
        var wanted = InterestRate.Units(Rate, scaling) - baseUnits;

        if (wanted == strengthening) return null;

        var isPutBack = _hasWritten;
        _hasWritten = true;

        return new RateWrite(wanted, baseUnits, isPutBack);
    }
}
