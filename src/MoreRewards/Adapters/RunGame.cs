using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace MoreRewards.Adapters;

/// <summary>The parts of the running game the rewards screen works with; each is null outside a run.</summary>
internal static class RunGame
{
    public static SurvivalResultsManager? Results() => GameSingleton.Existing<BattleLogicWorld>()?._SurvivalResultsManager;

    public static LeadingRole? Role() => GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();

    public static SurvivalPlanningComponent? Planning()
    {
        var role = Role();

        return role == null ? null : AgentTools.GetAgentComponent<SurvivalPlanningComponent>(role);
    }

    public static int Day() => GameSingleton.Existing<BattleLogicWorld>()?._GameTimeManager?.GetDay() ?? 0;
}
