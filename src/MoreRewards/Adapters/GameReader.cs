using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using MoreRewards.Core;
using LanguageMap = Il2CppSystem.Collections.Generic.Dictionary<string, string>;

namespace MoreRewards.Adapters;

/// <summary>
/// Reads the game's rewards for the Rewards tab: each reward with the levels above it, the pools they sit in, the
/// fields of the first effect that runs each script, and the item images. It runs when the tables are as the game made
/// them, just before the rewards file is written into them; rows an earlier write made are left out.
/// </summary>
internal static class GameReader
{
    private const string ScriptPrefix = "Buff/";

    public static GameSnapshot Read(ConfigManager config, CustomCache cache, Func<Table, int, bool> isMade)
    {
        var talents = config._Config_Talent_Dict;
        var buffs = config._Config_Buff_Dict;
        var effects = config._Config_Effect_Dict;
        var conditions = config._Config_ConditionSet_Dict;
        var english = cache._EnglishLanguageMap_k__BackingField;
        var chinese = cache._ChineseLanguageMap_k__BackingField;
        var rewards = new Dictionary<int, Config_Talent>();

        foreach (var row in talents.Values)
        {
            // The row ID is the reward. 75 of the game's rewards have a TalentID of ID / 10, which the game never reads.
            if (row == null || row._Type_k__BackingField != RewardCatalog.PlanType || isMade(Table.Reward, row._ID_k__BackingField)) continue;

            rewards[row._ID_k__BackingField] = row;
        }

        var pools = PoolsOf(config, out var poolIds);
        var templates = Templates(effects, isMade);
        var levelOf = new Func<Config_Talent, GameRewardLevel>(row => Level(row, buffs, effects, english, chinese, pools));
        int Next(int id) => rewards.TryGetValue(id + 1, out var next) && Needs(next, conditions).Contains(id) ? id + 1 : 0;
        var tiers = new HashSet<int>(rewards.Keys.Select(Next).Where(id => id != 0));
        var list = new List<GameReward>();

        foreach (var head in rewards.Keys.Where(id => !tiers.Contains(id)).OrderBy(id => id))
        {
            var chain = new List<Config_Talent>();

            for (var id = head; id != 0 && chain.Count < RewardEntry.MaxLevels; id = Next(id)) chain.Add(rewards[id]);

            var first = chain[0];

            list.Add(new GameReward(
                head,
                first._Available_k__BackingField,
                first._WebIcon_k__BackingField ?? "",
                first._Icon_k__BackingField ?? "",
                Needs(first, conditions),
                Furniture(first, conditions),
                Rules(first, buffs, conditions),
                chain.Select(levelOf).ToList()));
        }

        return new GameSnapshot(list, templates, ItemIcons(config), poolIds, Characters(config, english, chinese), Days(config._Config_DailyRandom_Dict), PoolSizes(config));
    }

    /// <summary>The condition fields the tab edits itself; the rest are shown as they are.</summary>
    private static readonly HashSet<string> EditedNeeds = new(StringComparer.Ordinal) { "ID", "Source", "TalentNeed", "NeedFurnitureObtainable", "SurvivalToDay" };

    private static GameRules Rules(Config_Talent row, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> buffs, Il2CppSystem.Collections.Generic.Dictionary<int, Config_ConditionSet> conditions)
    {
        var buffId = row._BuffID_k__BackingField;
        var condition = Condition(row, conditions);

        return new GameRules(
            row._IsReget_k__BackingField,
            row._EndlessMode_k__BackingField,
            Ints(row._PlayerSelectId_k__BackingField),
            buffId != 0 && buffs.ContainsKey(buffId) ? buffs[buffId]?._MaxLayerCount_k__BackingField ?? 0 : 0,
            condition?._SurvivalToDay_k__BackingField ?? 0,
            condition == null ? Array.Empty<string>() : RowFields.Describe(condition, EditedNeeds));
    }

    private static List<GameCharacter> Characters(ConfigManager config, LanguageMap? english, LanguageMap? chinese)
    {
        var characters = new List<GameCharacter>();
        var table = config._Config_PlayerSelect_Dict;

        if (table == null) return characters;

        foreach (var row in table.Values)
        {
            if (row != null) characters.Add(new GameCharacter(row._ID_k__BackingField, Text(english, chinese, row._Name_k__BackingField, row._Name_Local_k__BackingField)));
        }

        return characters;
    }

    /// <summary>Each day's pools and amounts as the table has them now; the plan reads it too, after an earlier write is put back.</summary>
    public static List<DayEntry> Days(Il2CppSystem.Collections.Generic.Dictionary<int, Config_DailyRandom>? table)
    {
        var days = new List<DayEntry>();

        if (table == null) return days;

        foreach (var row in table.Values)
        {
            if (row == null) continue;

            var pools = Ints(row._RandomPlanID_k__BackingField);
            var amounts = Ints(row._RandomAmount_k__BackingField);

            days.Add(new DayEntry(row._Day_k__BackingField, pools.Select((pool, i) => new DayPool(pool, i < amounts.Count ? amounts[i] : 0)).ToList()));
        }

        return days;
    }

    private static GameRewardLevel Level(Config_Talent row, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> buffs, Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects, LanguageMap? english, LanguageMap? chinese, Dictionary<int, List<PoolEntry>> pools)
    {
        var buffId = row._BuffID_k__BackingField;
        var effectId = buffs.ContainsKey(buffId) ? buffs[buffId]?._EffectConfigId_k__BackingField ?? 0 : 0;
        var effect = effectId != 0 && effects.ContainsKey(effectId) ? effects[effectId] : null;

        return new GameRewardLevel(
            row._ID_k__BackingField,
            row._LvUpCost_k__BackingField,
            Text(english, chinese, row._Name_k__BackingField, row._Name_Local_k__BackingField),
            Text(english, chinese, row._Dec_k__BackingField, row._Dec_Local_k__BackingField),
            effect?._ConfigName_k__BackingField ?? "",
            effect == null ? new List<FieldSet>() : RowFields.ReadAll(effect),
            pools.TryGetValue(row._ID_k__BackingField, out var listed) ? listed : new List<PoolEntry>());
    }

    /// <summary>Every reward's place in the pools, with its weight there, and the IDs of the pools that hold any.</summary>
    private static Dictionary<int, List<PoolEntry>> PoolsOf(ConfigManager config, out List<int> poolIds)
    {
        var byReward = new Dictionary<int, List<PoolEntry>>();

        poolIds = new List<int>();

        foreach (var pool in config._Config_RandomGroup_Dict.Values)
        {
            var ids = pool?._IdList_k__BackingField;
            var weights = pool?._CountList_k__BackingField;

            if (pool == null || ids == null) continue;

            poolIds.Add(pool._ID_k__BackingField);

            for (var i = 0; i < ids.Count; i++)
            {
                if (!byReward.TryGetValue(ids[i], out var listed)) byReward[ids[i]] = listed = new List<PoolEntry>();

                listed.Add(new PoolEntry(pool._ID_k__BackingField, weights != null && i < weights.Count ? weights[i] : 1));
            }
        }

        return byReward;
    }

    /// <summary>How many rewards each pool holds, so the tab finds each day's main pool as the plan does (<see cref="MainPools"/>).</summary>
    private static Dictionary<int, int> PoolSizes(ConfigManager config)
    {
        var sizes = new Dictionary<int, int>();

        foreach (var pool in config._Config_RandomGroup_Dict.Values)
        {
            if (pool != null) sizes[pool._ID_k__BackingField] = pool._IdList_k__BackingField?.Count ?? 0;
        }

        return sizes;
    }

    /// <summary>The fields of the lowest effect row running each script, which a new give starts from, as the plan copies it.</summary>
    private static Dictionary<string, IReadOnlyList<FieldSet>> Templates(Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects, Func<Table, int, bool> isMade)
    {
        var lowest = new Dictionary<string, Config_Effect>(StringComparer.Ordinal);

        foreach (var effect in effects.Values)
        {
            var name = effect?._ConfigName_k__BackingField;

            if (effect == null || name == null || !name.StartsWith(ScriptPrefix, StringComparison.Ordinal) || isMade(Table.Effect, effect._ID_k__BackingField)) continue;

            if (!lowest.TryGetValue(name, out var first) || effect._ID_k__BackingField < first._ID_k__BackingField) lowest[name] = effect;
        }

        return lowest.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<FieldSet>)RowFields.ReadAll(pair.Value), StringComparer.Ordinal);
    }

    private static IReadOnlyList<int> Needs(Config_Talent row, Il2CppSystem.Collections.Generic.Dictionary<int, Config_ConditionSet> conditions) =>
        Ints(Condition(row, conditions)?._TalentNeed_k__BackingField);

    private static IReadOnlyList<int> Furniture(Config_Talent row, Il2CppSystem.Collections.Generic.Dictionary<int, Config_ConditionSet> conditions) =>
        Ints(Condition(row, conditions)?._NeedFurnitureObtainable_k__BackingField);

    private static Config_ConditionSet? Condition(Config_Talent row, Il2CppSystem.Collections.Generic.Dictionary<int, Config_ConditionSet> conditions)
    {
        var id = row._ConditionSetId_k__BackingField;

        return id != 0 && conditions.ContainsKey(id) ? conditions[id] : null;
    }

    private static IReadOnlyList<int> Ints(Il2CppSystem.Collections.Generic.List<int>? list)
    {
        var values = new List<int>();

        if (list == null) return values;

        for (var i = 0; i < list.Count; i++) values.Add(list[i]);

        return values;
    }

    /// <summary>The images of the game's items, which a reward can show as well as any reward's.</summary>
    private static List<GameIcon> ItemIcons(ConfigManager config)
    {
        var icons = new List<GameIcon>();
        var items = config._Config_Item_Dict;

        if (items == null) return icons;

        foreach (var item in items.Values)
        {
            if (item != null && !string.IsNullOrEmpty(item.WebIcon)) icons.Add(new GameIcon(item.WebIcon, item.Icon ?? ""));
        }

        return icons;
    }

    /// <summary>A table names its text by a language key; a key with no translation shows as itself, as the game shows it.</summary>
    private static LocalText Text(LanguageMap? english, LanguageMap? chinese, string? key, string? local)
    {
        if (string.IsNullOrEmpty(key)) return new LocalText("", local ?? "");

        var en = english != null && english.ContainsKey(key) ? english[key] : key;
        var zh = chinese != null && chinese.ContainsKey(key) ? chinese[key] : !string.IsNullOrEmpty(local) ? local! : en;

        return new LocalText(en, zh);
    }
}
