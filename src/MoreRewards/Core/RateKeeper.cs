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
    private bool _isHolding;

    /// <param name="rate">Null keeps the character's own rate, and the attribute is never written.</param>
    public RateKeeper(int? rate)
    {
        Rate = rate;
    }

    /// <summary>The rate to keep, in percent, or null for the character's own.</summary>
    public int? Rate { get; private set; }

    /// <summary>
    /// A change of the setting. The next write is a fresh one, not a put-back. With no rate, a strengthening the mod
    /// wrote is taken off once, and from then on the attribute is left alone.
    /// </summary>
    public void Use(int? rate)
    {
        _hasWritten = false;
        Rate = rate;
    }

    /// <param name="baseUnits">The attribute's base, the part the save keeps.</param>
    /// <param name="strengthening">The attribute's strengthening value now.</param>
    /// <param name="scaling">The game's units per 1 (GameKey.Attr_ScalingRatio, 1000).</param>
    /// <returns>The strengthening to write, or null when the attribute already totals the rate.</returns>
    public RateWrite? Next(int baseUnits, int strengthening, float scaling)
    {
        if (Rate is not { } rate) return Release(baseUnits, strengthening);

        var wanted = InterestRate.Units(rate, scaling) - baseUnits;

        if (wanted == strengthening) return null;

        var isPutBack = _hasWritten;
        _hasWritten = true;
        _isHolding = true;

        return new RateWrite(wanted, baseUnits, isPutBack);
    }

    private RateWrite? Release(int baseUnits, int strengthening)
    {
        if (!_isHolding) return null;

        _isHolding = false;

        return strengthening == 0 ? null : new RateWrite(0, baseUnits, IsPutBack: false);
    }
}
