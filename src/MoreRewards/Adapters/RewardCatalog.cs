using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// The game's rewards as the shop and the cards' labels need them, read from its config tables. The tables are read
/// through their dictionaries, not ConfigManager's Get_Config_ methods, which write an error to the game's log for an id
/// they do not have. A reward's category and life never change, so each is worked out once.
/// </summary>
internal sealed class RewardCatalog
{
    /// <summary>Config_Talent.Type of the end-of-day rewards.</summary>
    private const int PlanType = 2;

    private static readonly string[] SkippedEffectFields = { "MainKey", "ID" };

    private readonly Dictionary<int, (string Category, RewardLife Life)> _kinds = new();

    /// <summary>Every reward the shop can list: the end-of-day rewards the game has switched on and named.</summary>
    public IReadOnlyList<Config_Talent> Plans()
    {
        var table = Configs()?._Config_Talent_Dict;
        var plans = new List<Config_Talent>();

        if (table == null) return plans;

        foreach (var talent in table.Values)
        {
            if (talent != null && talent.Type == PlanType && talent.Available != 0 && !string.IsNullOrEmpty(talent.Name)) plans.Add(talent);
        }

        return plans;
    }

    public IReadOnlyList<ShopRow> ShopRows(SurvivalPlanningComponent planning, int day, ScreenSettings settings, RunBook book, Language language)
    {
        var discount = LeadingRole.GetSettlementBuyDiscountRatio();
        var tally = book.Shop(day, settings.ShopReset);
        var rows = new List<ShopRow>();

        foreach (var talent in Plans())
        {
            var facts = Facts(talent, planning, book);
            var why = Blocker.Why(facts, day, language, settings.ShopLocked);

            if (why != null && settings.ShopHidesUnavailable) continue;

            var isEarly = why == null && facts.MissingNeeds.Count > 0;
            var cardCost = ShopPrice.CardCost(talent.LvUpCost, discount);
            var (category, life) = Kind(talent);

            rows.Add(new ShopRow(talent.ID, Text(talent.Name), Text(talent.Dec), talent.WebIcon ?? "", cardCost, ShopPrice.Of(cardCost, settings, tally.Buys, isEarly), why, life, category, isEarly));
        }

        return rows;
    }

    /// <summary>Why <paramref name="id"/> cannot be bought from the shop now, null when it can, and its card cost.</summary>
    public (string? Why, int CardCost, bool IsEarly)? ShopCheck(int id, SurvivalPlanningComponent planning, int day, ScreenSettings settings, RunBook book, Language language)
    {
        if (Talent(id) is not { } talent || talent.Type != PlanType) return null;

        var facts = Facts(talent, planning, book);
        var why = Blocker.Why(facts, day, language, settings.ShopLocked);

        return (why, ShopPrice.CardCost(talent.LvUpCost, LeadingRole.GetSettlementBuyDiscountRatio()), why == null && facts.MissingNeeds.Count > 0);
    }

    public IReadOnlyDictionary<int, RewardLife> Lives(IEnumerable<int> ids)
    {
        var lives = new Dictionary<int, RewardLife>();

        foreach (var id in ids)
        {
            if (Talent(id) is { } talent) lives[id] = Kind(talent).Life;
        }

        return lives;
    }

    /// <summary>Every reward joined to <paramref name="id"/> through the rewards they need first.</summary>
    public IReadOnlyCollection<int> LineOf(int id)
    {
        var needs = new Dictionary<int, IReadOnlyList<int>>();

        foreach (var talent in Plans()) needs[talent.ID] = Needs(talent);

        return RewardLine.Of(id, needs);
    }

    public string NameOf(int id) => Talent(id) is { } talent ? Text(talent.Name) : id.ToString();

    private RewardFacts Facts(Config_Talent talent, SurvivalPlanningComponent planning, RunBook book)
    {
        var current = ConfigManager.GetCurrentPlayerSelectId();
        var characters = Ids(talent.PlayerSelectId);
        var onlyFor = characters.Count > 0 && !characters.Contains(current) ? characters.Select(CharacterName).ToList() : new List<string>();
        var missing = Needs(talent).Where(need => !planning.HasActivated(need)).Select(NameOf).ToList();
        var isModeBlocked = GameKey.IsRowBlockedInDemoTwo(talent.DemoTwoMode) || GameKey.IsRowBlockedInEndless(talent.EndlessMode);

        return new RewardFacts(talent.Available != 0, book.IsBanned(talent.ID), planning.HasActivated(talent.ID), talent.IsReget, isModeBlocked,
            onlyFor, missing, Condition(talent)?.SurvivalToDay ?? 0, planning.IsValidTalentId(talent.ID));
    }

    private (string Category, RewardLife Life) Kind(Config_Talent talent)
    {
        if (_kinds.TryGetValue(talent.ID, out var known)) return known;

        var effect = Effect(talent);
        var script = effect?.ConfigName ?? RewardKind.DefaultScript;
        var category = RewardKind.Category(script, script == RewardKind.DefaultScript && effect != null ? SetFields(effect) : Array.Empty<string>());
        var kind = (category, RewardKind.Life(category, script, effect?.BuffInterval ?? 0f));

        return _kinds[talent.ID] = kind;
    }

    private static IReadOnlyList<int> Needs(Config_Talent talent) => Ids(Condition(talent)?.TalentNeed);

    private static Config_ConditionSet? Condition(Config_Talent talent) =>
        talent.ConditionSetId > 0 && Configs()?._Config_ConditionSet_Dict is { } table && table.TryGetValue(talent.ConditionSetId, out var condition) ? condition : null;

    private static Config_Effect? Effect(Config_Talent talent)
    {
        var configs = Configs();

        if (configs?._Config_Buff_Dict is not { } buffs || !buffs.TryGetValue(talent.BuffID, out var buff) || buff == null) return null;

        return configs._Config_Effect_Dict is { } effects && effects.TryGetValue(buff.EffectConfigId, out var effect) ? effect : null;
    }

    /// <summary>The names of the effect's number fields that are set; the default script's category depends on them.</summary>
    private static IEnumerable<string> SetFields(Config_Effect effect) =>
        typeof(Config_Effect).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(property => !SkippedEffectFields.Contains(property.Name) && IsSet(property, effect))
            .Select(property => property.Name)
            .ToList();

    private static bool IsSet(PropertyInfo property, Config_Effect effect)
    {
        if (property.PropertyType == typeof(int)) return (int)(property.GetValue(effect) ?? 0) != 0;

        if (property.PropertyType == typeof(float)) return (float)(property.GetValue(effect) ?? 0f) != 0f;

        return false;
    }

    private static string CharacterName(int id) =>
        Configs()?._Config_PlayerSelect_Dict is { } table && table.TryGetValue(id, out var character) && character != null ? Text(character.Name) : id.ToString();

    private static Config_Talent? Talent(int id) =>
        Configs()?._Config_Talent_Dict is { } table && table.TryGetValue(id, out var talent) ? talent : null;

    private static List<int> Ids(Il2CppSystem.Collections.Generic.List<int>? ids)
    {
        var list = new List<int>();

        if (ids == null) return list;

        for (var i = 0; i < ids.Count; i++) list.Add(ids[i]);

        return list;
    }

    private static string Text(string? key) => string.IsNullOrEmpty(key) ? "" : ConstantTextTools.GetLocalText(key) ?? key;

    private static ConfigManager? Configs() => GameSingleton.Existing<ConfigManager>();
}
