using System;
using System.Collections.Generic;
using System.Linq;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using Il2CppInterop.Runtime.InteropTypes;
using MoreRewards.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;
using LanguageMap = Il2CppSystem.Collections.Generic.Dictionary<string, string>;

namespace MoreRewards.Adapters;

/// <summary>
/// Writes the rewards files into the game's tables (effects, buffs, condition sets, rewards and reward pools) and its
/// language maps, once the game has loaded them. It writes again when the game loads the tables afresh, and when a
/// setting changes what applies: then it first puts back everything it wrote, so a reward turned off leaves the pools
/// and the game's rows as they were. A reward it made stays in the table, out of the pools, so a save that owns it
/// still finds it.
/// </summary>
internal sealed class RewardTables
{
    private const float CheckSeconds = 1f;

    private readonly Func<RewardFile> _active;
    private readonly Action<string> _log;
    private readonly List<Action> _undo = new();
    private readonly HashSet<(Table, int)> _made = new();
    private (IntPtr Talents, IntPtr Buffs, IntPtr Effects, IntPtr Conditions, IntPtr Pools) _applied;
    private bool _isStale = true;
    private float _nextCheckAt;

    /// <param name="active">The rewards that apply now, read each time the tables are written.</param>
    public RewardTables(Func<RewardFile> active, Action<string> log)
    {
        _active = active;
        _log = log;
    }

    /// <summary>The More Rewards effects the rewards written now give.</summary>
    public IReadOnlyList<MrGrant> Grants { get; private set; } = Array.Empty<MrGrant>();

    /// <summary>The game's rewards as the game made them, read just before the last write, for the Rewards tab.</summary>
    public GameSnapshot Game { get; private set; } = GameSnapshot.Empty;

    /// <summary>What went wrong in the last write, for the Rewards tab.</summary>
    public IReadOnlyList<string> Problems { get; private set; } = Array.Empty<string>();

    /// <summary>Goes up each time the tables are written, so the shop knows to list them again.</summary>
    public int Edits { get; private set; }

    /// <summary>What applies has changed: write the tables again at the next check.</summary>
    public void Changed() => _isStale = true;

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var config = GameSingleton.Existing<ConfigManager>();
        var talents = config?._Config_Talent_Dict;
        var buffs = config?._Config_Buff_Dict;
        var effects = config?._Config_Effect_Dict;
        var conditions = config?._Config_ConditionSet_Dict;
        var pools = config?._Config_RandomGroup_Dict;
        var cache = config?._customCache_k__BackingField;

        if (talents == null || buffs == null || effects == null || conditions == null || pools == null || cache == null) return;

        if (talents.Count == 0 || buffs.Count == 0 || effects.Count == 0 || pools.Count == 0) return;

        var tables = (talents.Pointer, buffs.Pointer, effects.Pointer, conditions.Pointer, pools.Pointer);

        if (tables == _applied && !_isStale) return;

        if (tables == _applied) Undo();
        else Forget();

        _applied = tables;
        _isStale = false;

        Game = GameReader.Read(config, cache, (table, id) => _made.Contains((table, id)));

        var plan = RewardPlan.Build(_active(), new GameTables(talents, buffs, effects, conditions, pools, _made));
        var problems = new List<string>(plan.Problems);

        foreach (var edit in plan.Rows)
        {
            var problem = edit.Table switch
            {
                Table.Effect => Apply(effects, edit, () => new Config_Effect()),
                Table.Buff => Apply(buffs, edit, () => new Config_Buff()),
                Table.Condition => Apply(conditions, edit, () => new Config_ConditionSet()),
                _ => Apply(talents, edit, () => new Config_Talent()),
            };

            if (problem != null) problems.Add(problem);
        }

        foreach (var edit in plan.Pools) Pool(pools[edit.Pool], edit);

        WriteTexts(cache, plan.Texts);
        Grants = plan.Grants;
        Problems = problems;
        CompanionBuffs.Use(plan.Companions);
        Edits++;
        Report(plan, problems);
    }

    /// <summary>Puts back what the last write changed, newest first.</summary>
    private void Undo()
    {
        for (var i = _undo.Count - 1; i >= 0; i--) _undo[i]();

        _undo.Clear();
    }

    /// <summary>The game loaded its tables afresh, so nothing in them is ours any more.</summary>
    private void Forget()
    {
        _undo.Clear();
        _made.Clear();
    }

    private string? Apply<T>(Il2CppSystem.Collections.Generic.Dictionary<int, T> table, RowEdit edit, Func<T> make) where T : Il2CppObjectBase
    {
        if (edit.CopyFrom is { } source) return Copy(table, edit, source, make);

        if (!table.ContainsKey(edit.Id)) return $"{edit.Table} {edit.Id} is gone";

        return Set(table[edit.Id], edit);
    }

    /// <summary>
    /// Makes the row as a copy of <paramref name="source"/>. Undone, a game row it replaced comes back; a row it made
    /// stays, and a reward it made is no longer offered. A row made by an earlier write is made again from scratch.
    /// </summary>
    private string? Copy<T>(Il2CppSystem.Collections.Generic.Dictionary<int, T> table, RowEdit edit, int source, Func<T> make) where T : Il2CppObjectBase
    {
        if (!table.ContainsKey(source)) return $"{edit.Table} {edit.Id}: copyFrom {source} is gone";

        var isOurs = _made.Contains((edit.Table, edit.Id));
        var replaced = !isOurs && table.ContainsKey(edit.Id) ? table[edit.Id] : null;
        var row = make();

        RowFields.CopyAll(table[source], row);
        table[edit.Id] = row;

        if (replaced != null) _undo.Add(() => table[edit.Id] = replaced);
        else _made.Add((edit.Table, edit.Id));

        if (replaced == null && row is Config_Talent reward) _undo.Add(() => reward._Available_k__BackingField = 0);

        return Set(row, edit, isOwnRow: true);
    }

    /// <summary>Sets the edit's fields; on a game row, each old value is kept so it can be put back.</summary>
    private string? Set(object row, RowEdit edit, bool isOwnRow = false)
    {
        var refused = new List<string>();

        foreach (var field in edit.Fields)
        {
            var old = RowFields.Get(row, field.Field);

            if (RowFields.Set(row, field.Field, field.Value) is { } problem) refused.Add(problem);
            else if (!isOwnRow) _undo.Add(() => RowFields.Restore(row, field.Field, old));
        }

        return refused.Count == 0 ? null : $"{edit.Table} {edit.Id}: " + string.Join("; ", refused);
    }

    /// <summary>The pool gets new lists, so the old ones can be put back as they were.</summary>
    private void Pool(Config_RandomGroup pool, PoolEdit edit)
    {
        var oldIds = pool._IdList_k__BackingField;
        var oldWeights = pool._CountList_k__BackingField;
        var ids = new Il2CppInts();
        var weights = new Il2CppInts();
        var isListed = false;

        for (var i = 0; i < oldIds.Count; i++)
        {
            var isReward = oldIds[i] == edit.Reward;

            ids.Add(oldIds[i]);
            weights.Add(isReward ? edit.Weight : i < oldWeights.Count ? oldWeights[i] : 1);
            isListed |= isReward;
        }

        if (!isListed)
        {
            ids.Add(edit.Reward);
            weights.Add(edit.Weight);
        }

        pool._IdList_k__BackingField = ids;
        pool._CountList_k__BackingField = weights;
        _undo.Add(() =>
        {
            pool._IdList_k__BackingField = oldIds;
            pool._CountList_k__BackingField = oldWeights;
        });
    }

    /// <summary>The game reads text from the map of the language in use; that may be one of these two or a third.</summary>
    private static void WriteTexts(CustomCache cache, IReadOnlyList<TextEntry> texts)
    {
        var english = cache._EnglishLanguageMap_k__BackingField;
        var chinese = cache._ChineseLanguageMap_k__BackingField;
        var current = cache._LanguageMap_k__BackingField;

        foreach (var text in texts)
        {
            if (english != null) english[text.Key] = text.En;

            if (chinese != null) chinese[text.Key] = text.Zh;

            if (current != null && !IsSame(current, english) && !IsSame(current, chinese)) current[text.Key] = IsChinese(cache) ? text.Zh : text.En;
        }
    }

    private static bool IsSame(LanguageMap map, LanguageMap? other) => other != null && map.Pointer == other.Pointer;

    private static bool IsChinese(CustomCache cache) => cache._LanguageMap_k__BackingField?.Pointer == cache._ChineseLanguageMap_k__BackingField?.Pointer;

    private void Report(RewardPlan plan, List<string> problems)
    {
        var made = plan.Rows.Where(row => row.Table == Table.Reward).Select(row => row.Id).ToList();

        if (made.Count > 0 || problems.Count > 0) _log(made.Count == 0 ? "rewards file: no reward applies" : $"rewards file: {string.Join(", ", made)} written");

        foreach (var problem in problems) _log("rewards file: " + problem);
    }

    /// <summary>
    /// What the plan needs to know of the game's tables, read once per write, before it. Rows an earlier write made
    /// are left in the tables, so they are not counted as the game's.
    /// </summary>
    private sealed class GameTables : IRewardTables
    {
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_Talent> _talents;
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> _buffs;
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> _effects;
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_ConditionSet> _conditions;
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_RandomGroup> _pools;
        private readonly HashSet<(Table, int)> _made;
        private Dictionary<string, int>? _effectByScript;

        public GameTables(
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Talent> talents,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> buffs,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_ConditionSet> conditions,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_RandomGroup> pools,
            HashSet<(Table, int)> made)
        {
            _talents = talents;
            _buffs = buffs;
            _effects = effects;
            _conditions = conditions;
            _pools = pools;
            _made = made;
        }

        public bool IsReward(int id) =>
            Talent(id) is { } row && row._Type_k__BackingField == RewardCatalog.PlanType && row._TalentID_k__BackingField == id;

        public bool HasTalent(int id) => Talent(id) != null;

        public bool HasBuff(int id) => !_made.Contains((Table.Buff, id)) && _buffs.ContainsKey(id);

        public bool HasEffect(int id) => !_made.Contains((Table.Effect, id)) && _effects.ContainsKey(id);

        public bool HasCondition(int id) => !_made.Contains((Table.Condition, id)) && _conditions.ContainsKey(id);

        public int NextTier(int id)
        {
            var next = id + 1;
            var condition = RewardCondition(next);
            var needs = condition == 0 ? null : _conditions[condition]._TalentNeed_k__BackingField;

            return IsReward(next) && needs != null && needs.Contains(id) ? next : 0;
        }

        public bool HasPool(int id) => _pools.ContainsKey(id);

        public int RewardBuff(int id) => Talent(id)?._BuffID_k__BackingField ?? 0;

        public int RewardCondition(int id) =>
            Talent(id)?._ConditionSetId_k__BackingField is { } condition && _conditions.ContainsKey(condition) ? condition : 0;

        /// <summary>The lowest effect row that runs the script, found by one pass over the effects table.</summary>
        public int? EffectRunning(string script)
        {
            if (_effectByScript == null)
            {
                _effectByScript = new Dictionary<string, int>();

                foreach (var effect in _effects.Values)
                {
                    var name = effect?._ConfigName_k__BackingField;

                    if (effect == null || name == null || _made.Contains((Table.Effect, effect._ID_k__BackingField))) continue;

                    if (!_effectByScript.TryGetValue(name, out var first) || effect._ID_k__BackingField < first) _effectByScript[name] = effect._ID_k__BackingField;
                }
            }

            return _effectByScript.TryGetValue(script, out var id) ? id : null;
        }

        private Config_Talent? Talent(int id) => !_made.Contains((Table.Reward, id)) && _talents.ContainsKey(id) ? _talents[id] : null;
    }
}
