using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Sets the interest rate (<see cref="RateKeeper"/>) on the character's PointsInterest attribute, which
/// SettleDailySurvivalPoints (rva 0x2699ec0) reads from the world's leading role. It is checked every second, so a
/// character that was just made or loaded has the rate long before its first day ends. The value goes in through the
/// game's own setter, AttributeComponent.SetStrengtheningValue_Float (rva 0x2de1e00), which also tells the game's
/// screens.
/// </summary>
internal sealed class RateField
{
    private const float CheckSeconds = 1f;

    private readonly RateKeeper _keeper;
    private readonly Action<string> _log;
    private float _nextCheckAt;
    private bool _hasLoggedPutBack;

    public RateField(RateKeeper keeper, Action<string> log)
    {
        _keeper = keeper;
        _log = log;
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var attributes = CharacterAttributes();
        var table = attributes?.AttrDict;

        if (attributes == null || table == null || !table.TryGetValue(AttrName.PointsInterest, out var attr) || attr == null) return;

        var scaling = GameKey.Attr_ScalingRatio;

        if (_keeper.Next(attr.BaseValue, attr.StrengtheningValue, scaling) is not { } write) return;

        attributes.SetStrengtheningValue_Float(AttrName.PointsInterest, InterestRate.GameValue(write.Strengthening, scaling));
        Report(write, scaling);
    }

    /// <summary>The leading role's attributes, or null while no game is loaded.</summary>
    private static AttributeComponent? CharacterAttributes()
    {
        var role = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();

        return role == null ? null : AgentTools.GetAgentComponent<AttributeComponent>(role);
    }

    /// <summary>The first write happens once; writing it again comes with every character made or loaded, so it is reported once.</summary>
    private void Report(RateWrite write, float scaling)
    {
        if (!write.IsPutBack)
        {
            _log(FormattableString.Invariant($"interest rate is now {_keeper.Rate}% (the character's own is {write.BaseUnits * 100f / scaling:0.#}%)"));

            return;
        }

        if (!_hasLoggedPutBack) _log($"interest rate set to {_keeper.Rate}% again, for a new or loaded character or after something else changed it (reported once)");

        _hasLoggedPutBack = true;
    }
}
