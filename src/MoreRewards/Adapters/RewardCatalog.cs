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
/// they do not have.
/// What never changes in a game (the rewards the shop can list, their names, what they need first, their category and
/// life) is read once into a <see cref="PlanTable"/>, a slice a frame from the start of a run, and kept until the game
/// swaps the talent table or the text table (a change of language). What a run changes (bans, what is owned, the
/// character, the game mode) is read again every time.
/// </summary>
internal sealed class RewardCatalog
{
    /// <summary>Config_Talent.Type of the end-of-day rewards.</summary>
    private const int PlanType = 2;

    /// <summary>How often, with no build running, the catalog looks for a run and a table to build.</summary>
    private const float LookSeconds = 1f;

    private static readonly string[] SkippedEffectFields = { "MainKey", "ID" };

    /// <summary>The effect's number fields that can change the default script's category, found once.</summary>
    private static PropertyInfo[]? _tellingFields;

    private readonly FrameBudget _budget;
    private readonly KeyedBuild<TableKey, PlanTable> _plans;
    private float _nextLookAt = LookSeconds / 4;

    public RewardCatalog(FrameBudget budget)
    {
        _budget = budget;
        _plans = new KeyedBuild<TableKey, PlanTable>(_ => new PlanTableBuild());
    }

    /// <summary>
    /// Builds the table ahead of the first shop or ban, a slice a frame, once a run is going. With nothing to build
    /// this costs two field reads a frame, and looks at the game once a second.
    /// </summary>
    public void Tick(float now)
    {
        if (_plans.IsBuilding)
        {
            _budget.Start(now);
            _plans.Continue(_budget.IsSpent);

            return;
        }

        if (now < _nextLookAt) return;

        _nextLookAt = now + LookSeconds;

        if (RunGame.Results() == null || Key() is not { } key || _plans.Ready(key) != null) return;

        _budget.Start(now);
        _plans.Advance(key, _budget.IsSpent);
    }

    /// <summary>The id of every reward the shop can list, in the game's table order.</summary>
    public IReadOnlyList<int> PlanIds() => Table().Ids;

    /// <summary>
    /// The shop's rows, written into <paramref name="json"/> a reward a step, after waiting (a slice a frame) for the
    /// table if it is not built yet.
    /// </summary>
    public IEnumerable<bool> ShopSteps(SurvivalPlanningComponent planning, int day, ScreenSettings settings, RunBook book, Language language, ShopJson json)
    {
        PlanTable table;

        while (true)
        {
            if (Key() is not { } key)
            {
                table = PlanTable.Empty;

                break;
            }

            if (_plans.Advance(key, _budget.IsSpent) is { } built)
            {
                table = built;

                break;
            }

            yield return false;
        }

        var discount = LeadingRole.GetSettlementBuyDiscountRatio();
        var tally = book.Shop(day, settings.ShopReset);
        var character = ConfigManager.GetCurrentPlayerSelectId();

        foreach (var row in table.Rows)
        {
            var facts = Facts(row, planning, book, character);
            var why = Blocker.Why(facts, day, language, settings.ShopLocked);

            if (why == null || !settings.ShopHidesUnavailable)
            {
                var isEarly = why == null && facts.MissingNeeds.Count > 0;
                var cardCost = ShopPrice.CardCost(row.LvUpCost, discount);

                json.Add(new ShopRow(row.Id, row.Name, row.Desc, row.Icon, cardCost, ShopPrice.Of(cardCost, settings, tally.Buys, isEarly), why, row.Life, row.Category, isEarly));
            }

            yield return true;
        }
    }

    /// <summary>Why <paramref name="id"/> cannot be bought from the shop now, null when it can, and its card cost.</summary>
    public (string? Why, int CardCost, bool IsEarly)? ShopCheck(int id, SurvivalPlanningComponent planning, int day, ScreenSettings settings, RunBook book, Language language)
    {
        if (PlanRowOf(id) is not { } row) return null;

        var facts = Facts(row, planning, book, ConfigManager.GetCurrentPlayerSelectId());
        var why = Blocker.Why(facts, day, language, settings.ShopLocked);

        return (why, ShopPrice.CardCost(row.LvUpCost, LeadingRole.GetSettlementBuyDiscountRatio()), why == null && facts.MissingNeeds.Count > 0);
    }

    public IReadOnlyDictionary<int, RewardLife> Lives(IEnumerable<int> ids)
    {
        var built = Built();
        var lives = new Dictionary<int, RewardLife>();

        foreach (var id in ids)
        {
            if (built != null && built.ById.TryGetValue(id, out var row)) lives[id] = row.Life;
            else if (Talent(id) is { } talent) lives[id] = Kind(talent).Life;
        }

        return lives;
    }

    /// <summary>Every reward joined to <paramref name="id"/> through the rewards they need first.</summary>
    public IReadOnlyCollection<int> LineOf(int id) => RewardLine.Of(id, Table().NeedsById);

    public string NameOf(int id) => Talent(id) is { } talent ? Text(talent.Name) : id.ToString();

    /// <summary>The table for the game's tables now, built on the spot if it is not ready (only a click that beats the build pays for that).</summary>
    private PlanTable Table() => Key() is { } key ? _plans.Get(key) : PlanTable.Empty;

    /// <summary>The table for the game's tables now if it is built, else null; never builds.</summary>
    private PlanTable? Built() => Key() is { } key ? _plans.Ready(key) : null;

    /// <summary>
    /// The end-of-day reward from the built table, or read from the game when the table is not built or does not list
    /// it; null for anything else, which the shop never sells.
    /// </summary>
    private PlanRow? PlanRowOf(int id)
    {
        if (Built() is { } built && built.ById.TryGetValue(id, out var row)) return row;

        if (Talent(id) is not { } talent || talent.Type != PlanType) return null;

        return Read(talent);
    }

    private static TableKey? Key()
    {
        var configs = Configs();

        if (configs?._Config_Talent_Dict is not { } table) return null;

        var text = configs._customCache_k__BackingField?._LanguageMap_k__BackingField;

        return new TableKey(table.Pointer.ToInt64(), text?.Pointer.ToInt64() ?? 0);
    }

    /// <summary>What a run changes about the reward: read every time.</summary>
    private static RewardFacts Facts(PlanRow row, SurvivalPlanningComponent planning, RunBook book, int character)
    {
        var onlyFor = row.Characters.Count > 0 && !row.Characters.Contains(character) ? row.CharacterNames : Array.Empty<string>();
        var isModeBlocked = GameKey.IsRowBlockedInDemoTwo(row.DemoTwoMode) || GameKey.IsRowBlockedInEndless(row.EndlessMode);

        return new RewardFacts(row.IsAvailable, book.IsBanned(row.Id), planning.HasActivated(row.Id), row.IsReget, isModeBlocked,
            onlyFor, Missing(row, planning), row.FromDay, planning.IsValidTalentId(row.Id));
    }

    /// <returns>The names of the rewards <paramref name="row"/> needs first that the player does not have; a list is made only when one is missing.</returns>
    private static IReadOnlyList<string> Missing(PlanRow row, SurvivalPlanningComponent planning)
    {
        List<string>? missing = null;

        for (var i = 0; i < row.Needs.Count; i++)
        {
            if (!planning.HasActivated(row.Needs[i])) (missing ??= new List<string>()).Add(row.NeedNames[i]);
        }

        return (IReadOnlyList<string>?)missing ?? Array.Empty<string>();
    }

    /// <summary>What never changes about the reward in a game, in the language shown now.</summary>
    private static PlanRow Read(Config_Talent talent)
    {
        var characters = Ids(talent.PlayerSelectId);
        var condition = Condition(talent);
        var needs = Ids(condition?.TalentNeed);
        var (category, life) = Kind(talent);

        return new PlanRow(talent.ID, Text(talent.Name), Text(talent.Dec), talent.WebIcon ?? "", talent.LvUpCost, talent.Available != 0, talent.IsReget,
            talent.DemoTwoMode, talent.EndlessMode, characters, characters.Select(CharacterName).ToList(), needs, needs.Select(NameOfTalent).ToList(),
            condition?.SurvivalToDay ?? 0, category, life);
    }

    private static string NameOfTalent(int id) => Talent(id) is { } talent ? Text(talent.Name) : id.ToString();

    private static (string Category, RewardLife Life) Kind(Config_Talent talent)
    {
        var buff = Buff(talent);
        var effect = buff != null ? Effect(buff) : null;
        var script = effect?.ConfigName ?? RewardKind.DefaultScript;
        var category = RewardKind.Category(script, script == RewardKind.DefaultScript && effect != null ? SetFields(effect) : Array.Empty<string>());

        return (category, RewardKind.Life(category, buff?.BuffDuring));
    }

    private static Config_ConditionSet? Condition(Config_Talent talent) =>
        talent.ConditionSetId > 0 && Configs()?._Config_ConditionSet_Dict is { } table && table.TryGetValue(talent.ConditionSetId, out var condition) ? condition : null;

    private static Config_Buff? Buff(Config_Talent talent) =>
        Configs()?._Config_Buff_Dict is { } buffs && buffs.TryGetValue(talent.BuffID, out var buff) ? buff : null;

    private static Config_Effect? Effect(Config_Buff buff) =>
        Configs()?._Config_Effect_Dict is { } effects && effects.TryGetValue(buff.EffectConfigId, out var effect) ? effect : null;

    /// <summary>
    /// The names of the effect's number fields that are set, among those that can change the default script's category
    /// (<see cref="RewardKind.IsTellingField"/>); the category comes out the same as from every set field.
    /// </summary>
    private static IEnumerable<string> SetFields(Config_Effect effect)
    {
        var set = new List<string>();

        foreach (var property in TellingFields())
        {
            if (IsSet(property, effect)) set.Add(property.Name);
        }

        return set;
    }

    private static PropertyInfo[] TellingFields() =>
        _tellingFields ??= typeof(Config_Effect).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(property => !SkippedEffectFields.Contains(property.Name) && RewardKind.IsTellingField(property.Name)
                && (property.PropertyType == typeof(int) || property.PropertyType == typeof(float)))
            .ToArray();

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

    /// <summary>
    /// Reads the shop's rewards from the game's talent table: first the table's rows as they stand, then one row a
    /// step, kept when it is an end-of-day reward the game has switched on and named, as the shop has always listed.
    /// </summary>
    private sealed class PlanTableBuild : ISlicedBuild<PlanTable>
    {
        private readonly List<PlanRow> _rows = new();

        public PlanTable Result { get; private set; } = PlanTable.Empty;

        public IEnumerable<bool> Steps()
        {
            var talents = new List<Config_Talent>();

            if (Configs()?._Config_Talent_Dict is { } table)
            {
                foreach (var talent in table.Values)
                {
                    if (talent != null) talents.Add(talent);
                }
            }

            yield return true;

            foreach (var talent in talents)
            {
                if (talent.Type == PlanType && talent.Available != 0 && !string.IsNullOrEmpty(talent.Name)) _rows.Add(Read(talent));

                yield return true;
            }

            Result = new PlanTable(_rows);
        }
    }
}

/// <summary>A reward as the shop lists it, with what never changes about it in a game; the names are in the language shown when it was read.</summary>
/// <param name="Characters">The characters it is for (Config_Talent.PlayerSelectId); empty for every character.</param>
/// <param name="CharacterNames">Their names, in the same order.</param>
/// <param name="Needs">The rewards it needs first.</param>
/// <param name="NeedNames">Their names, in the same order.</param>
/// <param name="FromDay">The first day it can be offered, 0 for any day.</param>
internal sealed record PlanRow(
    int Id,
    string Name,
    string Desc,
    string Icon,
    int LvUpCost,
    bool IsAvailable,
    bool IsReget,
    int DemoTwoMode,
    int EndlessMode,
    IReadOnlyList<int> Characters,
    IReadOnlyList<string> CharacterNames,
    IReadOnlyList<int> Needs,
    IReadOnlyList<string> NeedNames,
    int FromDay,
    string Category,
    RewardLife Life);

/// <summary>Every reward the shop can list, in the game's table order, and the same rewards by id.</summary>
internal sealed class PlanTable
{
    public static readonly PlanTable Empty = new(Array.Empty<PlanRow>());

    public PlanTable(IReadOnlyList<PlanRow> rows)
    {
        Rows = rows;

        var ids = new List<int>(rows.Count);

        foreach (var row in rows)
        {
            ById[row.Id] = row;
            NeedsById[row.Id] = row.Needs;
            ids.Add(row.Id);
        }

        Ids = ids;
    }

    public IReadOnlyList<PlanRow> Rows { get; }

    public Dictionary<int, PlanRow> ById { get; } = new();

    /// <summary>What each reward needs first; a ban on a whole line follows it.</summary>
    public Dictionary<int, IReadOnlyList<int>> NeedsById { get; } = new();

    public IReadOnlyList<int> Ids { get; }
}
