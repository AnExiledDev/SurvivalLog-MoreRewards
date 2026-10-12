using System;
using GameCore.HotUpdate;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Sets the price of a rewards refresh. It cannot be hooked: GameKey.get_GlobalSetting_RefreshSurvivalCost (rva
/// 0x2f12a80) and SurvivalResultsManager.GetRefreshCost (rva 0x26992d0) both open with the method-init check,
/// `sub rsp,28h / cmp byte [rip+x],0 / jnz short` (bytes 48 83 ec 28 80 3d .. .. .. .. 00 75 xx, checked in
/// GameAssembly.dll), so the first 14 bytes the hook library rewrites hold a RIP-relative operand at byte 4 and a short
/// jump at byte 11. But the getter fills its static field _GlobalSetting_RefreshSurvivalCost (offset 0x558 of the
/// static fields) from the GlobalSetting table only while it is 0, and returns the field from then on, and nothing
/// else in the game writes it. So
/// the cost is written into that field, and every reader sees it: the charge in OnRefreshSurvivalPlans (rva
/// 0x2699b60) and the cost the page shows, which CountUpTimer.TriggerDaySettlement (rva 0x254b250) sends when a day
/// ends. The field is checked every second and put back if something changed it.
/// </summary>
internal sealed class CostField
{
    private const float CheckSeconds = 1f;

    private readonly CostKeeper _keeper;
    private readonly Action<string> _log;
    private float _nextCheckAt;
    private bool _hasLoggedPutBack;

    public CostField(CostKeeper keeper, Action<string> log)
    {
        _keeper = keeper;
        _log = log;
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var current = GameKey._GlobalSetting_RefreshSurvivalCost;

        if (current <= 0) current = LoadGameCost();

        if (_keeper.Next(current) is not { } write) return;

        GameKey._GlobalSetting_RefreshSurvivalCost = write.Cost;
        Report(write);
    }

    /// <summary>
    /// Has the game's getter fill the field, as it would the first time a day ends. The getter looks the cost up in
    /// ConfigManager's GlobalSetting table (Get_Config_GlobalSetting, rva 0x2caba00), which throws while the table is
    /// not loaded and logs an error for a missing key, so it is only asked once the table holds the key.
    /// </summary>
    private static int LoadGameCost()
    {
        var table = GameSingleton.Existing<ConfigManager>()?._Config_GlobalSetting_Dict;
        var key = GameKey.Key_GlobalSetting_RefreshSurvivalCost;

        if (table == null || key == null || !table.ContainsKey(key)) return 0;

        return GameKey.GlobalSetting_RefreshSurvivalCost;
    }

    /// <summary>The first write happens once; putting the cost back can repeat, so it is reported once.</summary>
    private void Report(CostWrite write)
    {
        if (!write.IsPutBack)
        {
            _log($"refresh cost is now {write.Cost} survival points (the game's own is {write.GameCost})");

            return;
        }

        if (!_hasLoggedPutBack) _log($"the refresh cost was changed by something else; put back to {write.Cost} (reported once)");

        _hasLoggedPutBack = true;
    }
}
