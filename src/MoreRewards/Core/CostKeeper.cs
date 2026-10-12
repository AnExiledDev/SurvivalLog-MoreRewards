namespace MoreRewards.Core;

/// <summary>A cost to put in the game's refresh-cost field. <see cref="IsPutBack"/> when something else had changed it.</summary>
public readonly record struct CostWrite(int Cost, int GameCost, bool IsPutBack);

/// <summary>
/// Keeps the game's refresh cost at the mod's. The game reads its cost from a field it fills once from its settings
/// and then trusts, so the mod writes its cost there. The first cost seen is the game's own and every target is worked
/// out from it; a value something else writes later is put back, never taken as the game's cost, so the cost cannot
/// creep up with each check.
/// </summary>
public sealed class CostKeeper
{
    private readonly RewardSettings _settings;
    private int? _gameCost;
    private int? _written;

    public CostKeeper(RewardSettings settings)
    {
        _settings = settings;
    }

    /// <param name="current">The field's value; 0 or less is a cost the game has not loaded yet.</param>
    /// <returns>The cost to write, or null when the field should stay as it is.</returns>
    public CostWrite? Next(int current)
    {
        if (current <= 0 || current == _written) return null;

        var gameCost = _gameCost ??= current;
        var target = RefreshCost.Target(gameCost, _settings);
        var isPutBack = _written != null;
        _written = target;

        return target == current ? null : new CostWrite(target, gameCost, isPutBack);
    }
}
