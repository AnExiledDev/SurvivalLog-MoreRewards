using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RewardPlanTests
{
    /// <summary>A small slice of the game's tables: Sunlight Collection I and II, their buffs, effects, needs and pools.</summary>
    internal sealed class FakeTables : IRewardTables
    {
        /// <summary>Each game reward's next level: Sunlight Collection II needs I.</summary>
        public Dictionary<int, int> Tiers { get; } = new() { [31271] = 31272 };
        public HashSet<int> Rewards { get; } = new() { 31271, 31272 };
        public HashSet<int> Talents { get; } = new() { 31271, 31272, 50001 };
        public HashSet<int> Buffs { get; } = new() { 312710, 312720 };
        public HashSet<int> Effects { get; } = new() { 200312710, 200312720, 200100910 };
        public HashSet<int> Conditions { get; } = new() { 31271, 31272, 4001 };
        public int Blank { get; set; } = 4001;
        public HashSet<int> PoolIds { get; } = new() { 1013, 1020 };
        public HashSet<int> Days { get; } = new() { 1, 57 };
        public Dictionary<string, int> Scripts { get; } = new() { ["Buff/AE_PowerSolarEfficiency"] = 200312710, ["Buff/Buff_Empty"] = 200100910 };
        public List<DayEntry> DayRows { get; } = new();
        public Dictionary<int, int> Sizes { get; } = new();

        public bool IsReward(int id) => Rewards.Contains(id);
        public bool HasTalent(int id) => Talents.Contains(id);
        public bool HasBuff(int id) => Buffs.Contains(id);
        public bool HasEffect(int id) => Effects.Contains(id);
        public bool HasCondition(int id) => Conditions.Contains(id);
        public bool HasPool(int id) => PoolIds.Contains(id);
        public bool HasDay(int day) => Days.Contains(day);
        public int RewardBuff(int id) => Rewards.Contains(id) ? id * 10 : 0;
        public int RewardCondition(int id) => Conditions.Contains(id) && id != Blank ? id : 0;
        public int BlankCondition() => Blank;
        public int? EffectRunning(string script) => Scripts.TryGetValue(script, out var id) ? id : null;
        public int NextTier(int id) => Tiers.TryGetValue(id, out var next) ? next : 0;
        public IReadOnlyList<DayEntry> GameDays() => DayRows;
        public int PoolSize(int pool) => Sizes.TryGetValue(pool, out var size) ? size : 0;

        /// <summary>
        /// Days 1 and 7 as the game has them: day 1 draws from 9114 (7 rewards), 9102 (30) and 1005 (4), day 7 from 9103
        /// (72), 9121 (14) and 1005. So 9102 and 9103 are the main pools.
        /// </summary>
        public FakeTables WithDays()
        {
            DayRows.Add(new DayEntry(1, new[] { new DayPool(9114, 1), new DayPool(9102, 2), new DayPool(1005, 3) }));
            DayRows.Add(new DayEntry(7, new[] { new DayPool(9103, 1), new DayPool(9121, 1), new DayPool(1005, 1) }));
            Days.Add(7);

            foreach (var (pool, size) in new[] { (9114, 7), (9102, 30), (1005, 4), (9103, 72), (9121, 14) })
            {
                Sizes[pool] = size;
                PoolIds.Add(pool);
            }

            return this;
        }

        /// <summary>Adds Sunlight Collection III and every pool the wind rewards use.</summary>
        public FakeTables WithSunlightThree()
        {
            Rewards.Add(31273);
            Talents.Add(31273);
            Buffs.Add(312730);
            Conditions.Add(31273);
            Tiers[31272] = 31273;

            foreach (var pool in new[] { 1021, 1022, 9105, 9106, 9107, 9108, 9110, 9111, 9112, 9130, 9131, 9132, 9133 }) PoolIds.Add(pool);

            return this;
        }
    }

    private static RewardPlan Plan(string json, FakeTables? tables = null) => RewardPlan.Build(RewardFile.Parse(json), tables ?? new FakeTables());

    private static FieldValue? Value(RowEdit row, string field) => row.Fields.LastOrDefault(set => set.Field == field)?.Value;

    private static RowEdit Row(RewardPlan plan, Table table, int id) => Assert.Single(plan.Rows, row => row.Table == table && row.Id == id);

    [Fact]
    public void A_new_reward_is_a_copy_with_its_own_condition_buff_effect_and_text()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271, "name": { "en": "Wind I", "zh": "风 1" }, "cost": 10,
              "needs": [31271], "pools": { "1013": 1, "1020": 3 },
              "give": { "effect": "game:AE_PowerSolarEfficiency", "fields": { "FloatValue0": 0.15 } } } ] }
            """);

        Assert.Empty(plan.Problems);

        var reward = Row(plan, Table.Reward, 39001);
        Assert.Equal(31271, reward.CopyFrom);
        Assert.Equal(new FieldValue.Number(39001), Value(reward, "TalentID"));
        Assert.Equal(new FieldValue.Number(10), Value(reward, "LvUpCost"));
        Assert.Equal(new FieldValue.Number(390010), Value(reward, "BuffID"));
        Assert.Equal(new FieldValue.Number(39001), Value(reward, "ConditionSetId"));
        Assert.Equal(new FieldValue.Text("风 1"), Value(reward, "Name_Local"));

        var condition = Row(plan, Table.Condition, 39001);
        Assert.Equal(31271, condition.CopyFrom);
        Assert.Equal(new[] { 31271 }, Assert.IsType<FieldValue.Numbers>(Value(condition, "TalentNeed")).Values);

        var buff = Row(plan, Table.Buff, 390010);
        Assert.Equal(312710, buff.CopyFrom);
        Assert.Equal(new FieldValue.Number(200390010), Value(buff, "EffectConfigId"));

        var effect = Row(plan, Table.Effect, 200390010);
        Assert.Equal(200312710, effect.CopyFrom);
        Assert.Equal(new FieldValue.Number(0.15), Value(effect, "FloatValue0"));

        Assert.Equal(new[] { new PoolEdit(1013, 39001, 1), new PoolEdit(1020, 39001, 3) }, plan.Pools);
        Assert.Contains(plan.Texts, text => text.En == "Wind I" && text.Zh == "风 1");
        Assert.Empty(plan.Grants);
    }

    [Fact]
    public void A_copied_reward_never_changes_the_text_or_needs_of_the_reward_it_copies()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "name": "Wind", "needs": [] } ] }""");

        Assert.DoesNotContain(plan.Rows, row => row.Id == 31271);
        Assert.All(plan.Texts, text => Assert.Contains("39001", text.Key));
    }

    [Fact]
    public void An_mr_give_runs_the_marker_effect_and_hands_its_share_to_the_mod()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "give": { "effect": "mr:wind-power", "fields": { "FloatValue0": 0.15 } } } ] }""");

        Assert.Empty(plan.Problems);

        var effect = Row(plan, Table.Effect, 200390010);
        Assert.Equal(200100910, effect.CopyFrom);
        Assert.Null(Value(effect, "FloatValue0"));

        var grant = Assert.Single(plan.Grants);
        Assert.Equal(RewardPlan.WindPower, grant.Effect);
        Assert.Equal(39001, grant.RewardId);
        Assert.Equal(new[] { new FieldSet(RewardPlan.WindShare, new FieldValue.Number(0.15)) }, grant.Fields);
    }

    [Fact]
    public void Changing_a_game_reward_edits_its_own_rows_without_copying()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "cost": 4, "needs": [31272], "pools": { "1013": 5 } } ] }""");

        Assert.Empty(plan.Problems);

        var reward = Row(plan, Table.Reward, 31271);
        Assert.Null(reward.CopyFrom);
        Assert.Equal(new FieldValue.Number(4), Value(reward, "LvUpCost"));
        Assert.Null(Value(reward, "TalentID"));
        Assert.Null(Row(plan, Table.Condition, 31271).CopyFrom);
        Assert.Equal(new[] { new PoolEdit(1013, 31271, 5) }, plan.Pools);
    }

    [Fact]
    public void A_reward_with_any_problem_writes_none_of_its_rows_and_the_others_still_apply()
    {
        var plan = Plan("""
            { "rewards": [
              { "id": 39001, "copyFrom": 31271, "pools": { "7777": 1 }, "give": { "effect": "game:AE_PowerSolarEfficiency" } },
              { "id": 39002, "copyFrom": 31272 } ] }
            """);

        Assert.DoesNotContain(plan.Rows, row => row.Id is 39001 or 390010 or 200390010);
        Assert.Contains(plan.Problems, problem => problem.Contains("39001") && problem.Contains("7777"));
        Row(plan, Table.Reward, 39002);
    }

    [Theory]
    [InlineData("""{ "id": 50001, "copyFrom": 31271 }""", "talent")]
    [InlineData("""{ "id": 39001 }""", "copyFrom")]
    [InlineData("""{ "id": 39001, "copyFrom": 50001 }""", "not an end-of-day reward")]
    [InlineData("""{ "id": 39001, "copyFrom": 31271, "give": { "effect": "game:NoSuchScript" } }""", "NoSuchScript")]
    [InlineData("""{ "id": 39001, "copyFrom": 31271, "give": { "effect": "mr:nothing" } }""", "mr:nothing")]
    [InlineData("""{ "id": 39001, "copyFrom": 31271, "give": { "effect": "AE_PowerSolarEfficiency" } }""", "game:")]
    public void A_reward_that_cannot_be_built_is_skipped_and_says_why(string reward, string reason)
    {
        var plan = Plan($$"""{ "rewards": [ {{reward}} ] }""");

        Assert.Empty(plan.Rows);
        Assert.Contains(plan.Problems, problem => problem.Contains(reason));
    }

    [Fact]
    public void A_new_reward_never_takes_a_buff_or_effect_the_game_already_uses()
    {
        var tables = new FakeTables();
        tables.Effects.Add(200390010);

        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "give": { "effect": "game:AE_PowerSolarEfficiency" } } ] }""", tables);

        Assert.Empty(plan.Rows);
        Assert.Contains(plan.Problems, problem => problem.Contains("already in use"));
    }

    [Fact]
    public void A_disabled_reward_changes_nothing_including_its_own_buffs()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "enabled": false, "cost": 1, "buffs": [ { "id": 312710, "fields": { "Time": 5 } } ] } ] }""");

        Assert.Empty(plan.Rows);
        Assert.Empty(plan.Pools);
    }

    // A save that owns a new reward loses it on load when the reward has no row. Switching the reward off in the file
    // must only stop it being offered, never take its rows out of the tables.
    [Fact]
    public void A_disabled_new_reward_keeps_its_rows_but_is_never_offered()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "enabled": false, "copyFrom": 31271, "anyDay": 5, "alwaysOffered": true,
              "pools": { "1013": 5 }, "buffs": [ { "id": 390010, "copyFrom": 312710 } ],
              "levels": [ {}, { "fields": { "Available": 1 } } ] } ] }
            """);

        Assert.Equal(new FieldValue.Number(0), Value(Row(plan, Table.Reward, 39001), "Available"));
        Assert.Equal(new FieldValue.Number(0), Value(Row(plan, Table.Reward, 39002), "Available"));
        Assert.Single(plan.Rows, row => row.Table == Table.Buff && row.Id == 390010);
        Assert.Empty(plan.Pools);
        Assert.Empty(plan.AlwaysOffered);
    }

    [Fact]
    public void A_disabled_new_reward_gives_way_to_an_enabled_entry_with_the_same_id()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "cost": 3 }, { "id": 39001, "enabled": false, "copyFrom": 31271, "cost": 9 } ] }""");

        Assert.Equal(new FieldValue.Number(3), Value(Row(plan, Table.Reward, 39001), "LvUpCost"));
        Assert.NotEqual(new FieldValue.Number(0), Value(Row(plan, Table.Reward, 39001), "Available"));
        Assert.Empty(plan.Problems);
    }

    [Fact]
    public void The_second_entry_for_an_id_is_left_out()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "cost": 1 }, { "id": 31271, "cost": 2 } ] }""");

        Assert.Equal(new FieldValue.Number(1), Value(Row(plan, Table.Reward, 31271), "LvUpCost"));
        Assert.Contains(plan.Problems, problem => problem.Contains("already used"));
    }

    [Fact]
    public void An_entry_whose_levels_reach_an_id_used_above_it_is_left_out()
    {
        var plan = Plan("""
            { "rewards": [
              { "id": 39002, "copyFrom": 31271, "cost": 1 },
              { "id": 39001, "copyFrom": 31271, "levels": [ { "cost": 2 }, { "cost": 3 } ] } ] }
            """);

        Assert.Equal(new[] { 39002 }, plan.Rows.Where(row => row.Table == Table.Reward).Select(row => row.Id));
        Assert.Contains(plan.Problems, problem => problem.Contains("39001") && problem.Contains("39002"));
    }

    [Fact]
    public void Each_level_of_a_new_reward_copies_the_same_level_of_the_reward_it_copies_and_needs_the_one_below()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271, "name": { "en": "Wind", "zh": "风" }, "cost": 9, "pools": { "1013": 1 },
              "levels": [ { "cost": 10 }, { "pools": { "1020": 3 } }, { "name": "Top" } ] } ] }
            """);

        Assert.Empty(plan.Problems);
        Assert.Equal(31271, Row(plan, Table.Reward, 39001).CopyFrom);
        Assert.Equal(31272, Row(plan, Table.Reward, 39002).CopyFrom);
        Assert.Equal(31272, Row(plan, Table.Reward, 39003).CopyFrom);
        Assert.Equal(31271, Row(plan, Table.Condition, 39001).CopyFrom);
        Assert.Equal(31272, Row(plan, Table.Condition, 39002).CopyFrom);
        Assert.Null(Value(Row(plan, Table.Condition, 39001), "TalentNeed"));
        Assert.Equal(new[] { 39001 }, Assert.IsType<FieldValue.Numbers>(Value(Row(plan, Table.Condition, 39002), "TalentNeed")).Values);
        Assert.Equal(new[] { 39002 }, Assert.IsType<FieldValue.Numbers>(Value(Row(plan, Table.Condition, 39003), "TalentNeed")).Values);
        Assert.Equal(new FieldValue.Number(10), Value(Row(plan, Table.Reward, 39001), "LvUpCost"));
        Assert.Equal(new FieldValue.Number(9), Value(Row(plan, Table.Reward, 39002), "LvUpCost"));
        Assert.Equal(new[] { new PoolEdit(1013, 39001, 1), new PoolEdit(1020, 39002, 3), new PoolEdit(1013, 39003, 1) }, plan.Pools);
        Assert.Contains(plan.Texts, text => text.Key.EndsWith("Reward_Name_39001") && text.En == "Wind I" && text.Zh == "风 Lv1");
        Assert.Contains(plan.Texts, text => text.Key.EndsWith("Reward_Name_39002") && text.En == "Wind II" && text.Zh == "风 Lv2");
        Assert.Contains(plan.Texts, text => text.Key.EndsWith("Reward_Name_39003") && text.En == "Top");
    }

    [Fact]
    public void Levels_of_a_game_reward_change_its_own_next_levels_in_place()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "levels": [ { "cost": 4 }, { "cost": 7 } ] } ] }""");

        Assert.Empty(plan.Problems);
        Assert.Null(Row(plan, Table.Reward, 31271).CopyFrom);
        Assert.Null(Row(plan, Table.Reward, 31272).CopyFrom);
        Assert.Equal(new FieldValue.Number(7), Value(Row(plan, Table.Reward, 31272), "LvUpCost"));
        Assert.DoesNotContain(plan.Rows, row => row.Table == Table.Condition);
    }

    [Fact]
    public void A_level_past_the_game_reward_s_last_is_made_as_a_copy_of_its_top_level()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "levels": [ {}, {}, { "cost": 30 } ] } ] }""");

        Assert.Empty(plan.Problems);

        var top = Row(plan, Table.Reward, 31273);
        Assert.Equal(31272, top.CopyFrom);
        Assert.Equal(new[] { 31272 }, Assert.IsType<FieldValue.Numbers>(Value(Row(plan, Table.Condition, 31273), "TalentNeed")).Values);
    }

    [Fact]
    public void A_level_that_lands_on_some_other_game_reward_skips_the_whole_entry()
    {
        var tables = new FakeTables();
        tables.Rewards.Add(39002);
        tables.Talents.Add(39002);

        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "levels": [ {}, {} ] } ] }""", tables);

        Assert.Empty(plan.Rows);
        Assert.Contains(plan.Problems, problem => problem.Contains("not the next level of 39001"));
    }

    [Fact]
    public void A_second_give_is_a_hidden_buff_that_rides_with_the_reward_s_own()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271, "name": "Both",
              "gives": [ { "effect": "game:AE_PowerSolarEfficiency", "fields": { "FloatValue0": 0.1 } },
                         { "effect": "mr:wind-power", "fields": { "FloatValue0": 0.2 } } ] } ] }
            """);

        Assert.Empty(plan.Problems);
        Assert.Equal(new FieldValue.Number(390010), Value(Row(plan, Table.Reward, 39001), "BuffID"));

        var rider = Row(plan, Table.Buff, 390011);
        Assert.Equal(312710, rider.CopyFrom);
        Assert.Equal(new FieldValue.Flag(true), Value(rider, "IsHide"));
        Assert.Equal(new FieldValue.Number(200390011), Value(rider, "EffectConfigId"));
        Assert.Null(Value(rider, "Name"));
        Assert.Null(Value(Row(plan, Table.Buff, 390010), "IsHide"));
        Assert.Equal(200100910, Row(plan, Table.Effect, 200390011).CopyFrom);

        var companion = Assert.Single(plan.Companions);
        Assert.Equal(390010, companion.LevelBuff);
        Assert.Equal(new[] { 390011 }, companion.Buffs);
        Assert.Equal(39001, Assert.Single(plan.Grants).RewardId);
    }

    [Fact]
    public void A_give_s_level_fields_change_with_the_level_and_the_last_one_carries_on()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271,
              "gives": [ { "effect": "mr:wind-power", "levels": [ { "FloatValue0": 0.1 }, { "FloatValue0": 0.2 } ] },
                         { "effect": "game:AE_PowerSolarEfficiency", "fields": { "FloatValue0": 0.05 } } ],
              "levels": [ {}, {}, {} ] } ] }
            """);

        Assert.Empty(plan.Problems);
        Assert.Equal(new[] { 0.1, 0.2, 0.2 }, plan.Grants.Select(grant => Assert.IsType<FieldValue.Number>(grant.Fields.Single().Value).Value));
        Assert.Equal(new[] { 39001, 39002, 39003 }, plan.Grants.Select(grant => grant.RewardId));
        Assert.Equal(new FieldValue.Number(0.05), Value(Row(plan, Table.Effect, 200390031), "FloatValue0"));
        Assert.Equal(new[] { 390010, 390020, 390030 }, plan.Companions.Select(companion => companion.LevelBuff));
    }

    [Fact]
    public void One_give_makes_no_companions()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "gives": [ { "effect": "game:AE_PowerSolarEfficiency" } ] } ] }""");

        Assert.Empty(plan.Problems);
        Assert.Empty(plan.Companions);
    }

    [Fact]
    public void The_wind_rewards_build_like_sunlight_collection_one_two_and_three()
    {
        var plan = RewardPlan.Build(RewardFile.Parse(RewardFileTests.Resource("MoreRewards.wind-rewards.json")), new FakeTables().WithSunlightThree());
        var rewards = plan.Rows.Where(row => row.Table == Table.Reward).ToList();

        Assert.Empty(plan.Problems);
        Assert.Equal(new[] { 39001, 39002, 39003 }, rewards.Select(row => row.Id));
        Assert.Equal(new int?[] { 31271, 31272, 31273 }, rewards.Select(row => row.CopyFrom));
        Assert.Equal(new[] { 10.0, 16.0, 24.0 }, rewards.Select(row => Assert.IsType<FieldValue.Number>(Value(row, "LvUpCost")).Value));
        Assert.Equal(new[] { 390010, 390020, 390030 }, rewards.Select(row => (int)Assert.IsType<FieldValue.Number>(Value(row, "BuffID")).Value));
        Assert.All(rewards, row => Assert.Equal(new FieldValue.Text("Assets/RuntimeAssets/Texture/Furniture/UI_Item_Icon_Furnit_M_900300101"), Value(row, "Icon")));
        Assert.Null(Value(Row(plan, Table.Condition, 39001), "TalentNeed"));
        Assert.Equal(new[] { 39001 }, Assert.IsType<FieldValue.Numbers>(Value(Row(plan, Table.Condition, 39002), "TalentNeed")).Values);
        Assert.Equal(new[] { 39002 }, Assert.IsType<FieldValue.Numbers>(Value(Row(plan, Table.Condition, 39003), "TalentNeed")).Values);
        Assert.All(plan.Rows.Where(row => row.Table == Table.Condition), row => Assert.Equal(new[] { 910001, 910002, 910003 }, Assert.IsType<FieldValue.Numbers>(Value(row, "NeedFurnitureObtainable")).Values));
        Assert.Contains(plan.Texts, text => text.En == "Wind Collection II" && text.Zh == "风力收集 Lv2");
        Assert.Equal(new[] { 39001, 39002, 39003 }, plan.Grants.Select(grant => grant.RewardId));
        Assert.All(plan.Grants, grant => Assert.Equal(new[] { new FieldSet(RewardPlan.WindShare, new FieldValue.Number(0.15)) }, grant.Fields));
    }

    // Each wind level gives 15% more, so from level II on the card also says what the levels so far give together.
    [Fact]
    public void Each_wind_card_from_level_two_on_names_the_total()
    {
        var plan = RewardPlan.Build(RewardFile.Parse(RewardFileTests.Resource("MoreRewards.wind-rewards.json")), new FakeTables().WithSunlightThree());
        var card = (int id) => Assert.Single(plan.Texts, text => text.Key == "MoreRewards_Reward_Dec_" + id);

        Assert.Equal("Increases wind turbine power generation by 15%.", card(39001).En);
        Assert.Equal(("Increases wind turbine power generation by 15%. In total: +30%.", "风力发电机产电 +15%（累计+30%）"), (card(39002).En, card(39002).Zh));
        Assert.Equal(("Increases wind turbine power generation by 15%. In total: +45%.", "风力发电机产电 +15%（累计+45%）"), (card(39003).En, card(39003).Zh));
    }

    /// <summary>Sunlight Collection's pools in the game's Data Dump, card by card.</summary>
    [Fact]
    public void Each_wind_reward_is_in_the_same_pools_as_its_sunlight_card_with_the_same_weights()
    {
        var plan = RewardPlan.Build(RewardFile.Parse(RewardFileTests.Resource("MoreRewards.wind-rewards.json")), new FakeTables().WithSunlightThree());
        var both = new[] { 9105, 9106, 9111, 9130, 9131 };
        var all = new[] { 9107, 9108, 9112, 9132, 9133 };

        Assert.Equal(Pools((1013, 1), (1020, 3), (9110, 1)).Concat(Ones(both)).Concat(Ones(all)).OrderBy(pool => pool.Group), PoolsOf(plan, 39001));
        Assert.Equal(Pools((1013, 1), (1021, 3)).Concat(Ones(both)).Concat(Ones(all)).OrderBy(pool => pool.Group), PoolsOf(plan, 39002));
        Assert.Equal(Pools((1013, 1), (1022, 3)).Concat(Ones(all)).OrderBy(pool => pool.Group), PoolsOf(plan, 39003));
    }

    private static IEnumerable<PoolEntry> PoolsOf(RewardPlan plan, int reward) =>
        plan.Pools.Where(pool => pool.Reward == reward).Select(pool => new PoolEntry(pool.Pool, pool.Weight)).OrderBy(pool => pool.Group);

    [Fact]
    public void A_day_in_the_file_replaces_its_pools_and_amounts_in_draw_order_without_the_zeros()
    {
        var plan = Plan("""{ "rewards": [], "days": [ { "day": 57, "pools": [ { "pool": 1020, "amount": 2 }, { "pool": 1013, "amount": 0 }, { "pool": 1005, "amount": 1 } ] } ] }""", WithPool(1005));

        Assert.Empty(plan.Problems);

        var day = Assert.Single(plan.Days);

        Assert.Equal(57, day.Day);
        Assert.Equal(new[] { 1020, 1005 }, day.Pools);
        Assert.Equal(new[] { 2, 1 }, day.Amounts);
    }

    [Theory]
    [InlineData("""{ "day": 999, "pools": [ { "pool": 1013, "amount": 1 } ] }""", "day 999 is not in")]
    [InlineData("""{ "day": 57, "pools": [ { "pool": 4242, "amount": 1 } ] }""", "pool 4242")]
    public void A_day_the_game_cannot_take_is_a_problem_and_left_out(string day, string reason)
    {
        var plan = Plan("""{ "days": [ """ + day + """ ] }""");

        Assert.Contains(plan.Problems, problem => problem.Contains(reason));
        Assert.Empty(plan.Days);
    }

    [Fact]
    public void A_day_named_twice_keeps_the_first()
    {
        var plan = Plan("""{ "days": [ { "day": 1, "pools": [ { "pool": 1013, "amount": 2 } ] }, { "day": 1, "pools": [ { "pool": 1020, "amount": 5 } ] } ] }""");

        Assert.Contains(plan.Problems, problem => problem.Contains("twice"));
        Assert.Equal(new[] { 1013 }, Assert.Single(plan.Days).Pools);
    }

    [Theory]
    [InlineData("""{ "days": [ { "day": 1, "pools": [ { "pool": 1013, "amount": -1 } ] } ] }""")]
    [InlineData("""{ "days": [ { "day": 1 } ] }""")]
    [InlineData("""{ "days": [ { "day": 1, "pools": { "1013": 2 } } ] }""")]
    [InlineData("""{ "days": [ { "day": 1, "pools": [ { "pool": 1013, "amount": 2 }, { "pool": 1013, "amount": 1 } ] } ] }""")]
    public void A_day_that_cannot_be_read_is_reported(string json)
    {
        var file = RewardFile.Parse(json);

        Assert.Empty(file.Days);
        Assert.Single(file.Problems);
    }

    [Fact]
    public void The_players_days_replace_the_same_days_from_the_file_below()
    {
        var mine = RewardFile.Parse("""{ "days": [ { "day": 1, "pools": [ { "pool": 1013, "amount": 2 } ] } ] }""");
        var below = RewardFile.Parse("""{ "days": [ { "day": 1, "pools": [ { "pool": 1020, "amount": 1 } ] }, { "day": 57, "pools": [ { "pool": 1020, "amount": 1 } ] } ] }""");

        var both = mine.Over(below);

        Assert.Equal(new[] { 1, 57 }, both.Days.Select(day => day.Day));
        Assert.Equal(1013, both.Days[0].Pools[0].Pool);
    }

    [Fact]
    public void A_game_reward_with_fewer_levels_switches_off_the_games_higher_levels()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "levels": [ { "cost": 4 } ] } ] }""", new FakeTables().WithSunlightThree());

        Assert.Empty(plan.Problems);
        Assert.Equal(0d, Assert.IsType<FieldValue.Number>(Value(Row(plan, Table.Reward, 31272), "Available")).Value);
        Assert.Equal(0d, Assert.IsType<FieldValue.Number>(Value(Row(plan, Table.Reward, 31273), "Available")).Value);
        Assert.Null(Value(Row(plan, Table.Reward, 31271), "Available"));
    }

    [Fact]
    public void A_game_reward_with_no_levels_list_keeps_the_games_higher_levels()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "cost": 4 } ] }""", new FakeTables().WithSunlightThree());

        Assert.DoesNotContain(plan.Rows, row => row.Id is 31272 or 31273);
    }

    private static FakeTables WithPool(int pool)
    {
        var tables = new FakeTables();

        tables.PoolIds.Add(pool);

        return tables;
    }

    /// <summary>Adds Water Boost, a game reward with a buff and no condition set, as about half the game's rewards are.</summary>
    private static FakeTables WithWaterBoost()
    {
        var tables = new FakeTables();

        tables.Rewards.Add(31301);
        tables.Talents.Add(31301);
        tables.Buffs.Add(313010);

        return tables;
    }

    [Fact]
    public void From_day_on_a_reward_with_a_condition_set_changes_that_set()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "fromDay": 12 } ] }""");

        Assert.Empty(plan.Problems);
        Assert.Equal(new FieldValue.Number(12), Value(Row(plan, Table.Condition, 31271), "SurvivalToDay"));
        Assert.Null(Row(plan, Table.Condition, 31271).CopyFrom);
        Assert.Null(Value(Row(plan, Table.Reward, 31271), "ConditionSetId"));
    }

    [Fact]
    public void A_game_reward_with_no_condition_set_gets_its_own_copied_from_an_empty_one()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31301, "fromDay": 8, "needs": [31271] } ] }""", WithWaterBoost());
        var condition = Row(plan, Table.Condition, 31301);

        Assert.Empty(plan.Problems);
        Assert.Equal(4001, condition.CopyFrom);
        Assert.Equal(new FieldValue.Number(8), Value(condition, "SurvivalToDay"));
        Assert.Equal(new[] { 31271 }, Assert.IsType<FieldValue.Numbers>(Value(condition, "TalentNeed")).Values);
        Assert.Equal(new FieldValue.Number(31301), Value(Row(plan, Table.Reward, 31301), "ConditionSetId"));
    }

    [Fact]
    public void A_game_reward_with_no_condition_set_and_nothing_to_need_stays_without_one()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31301, "cost": 4 } ] }""", WithWaterBoost());

        Assert.Empty(plan.Problems);
        Assert.DoesNotContain(plan.Rows, row => row.Table == Table.Condition);
        Assert.Null(Value(Row(plan, Table.Reward, 31301), "ConditionSetId"));
    }

    [Fact]
    public void A_new_reward_copying_one_with_no_condition_set_gets_an_empty_one_when_it_needs_something()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31301, "fromDay": 3,
              "give": { "effect": "game:AE_PowerSolarEfficiency", "fields": { "FloatValue0": 0.1 } } } ] }
            """, WithWaterBoost());

        Assert.Empty(plan.Problems);
        Assert.Equal(4001, Row(plan, Table.Condition, 39001).CopyFrom);
        Assert.Equal(new FieldValue.Number(39001), Value(Row(plan, Table.Reward, 39001), "ConditionSetId"));
    }

    [Fact]
    public void Without_an_empty_condition_set_in_the_game_from_day_is_a_problem()
    {
        var tables = WithWaterBoost();

        tables.Blank = 0;

        var plan = Plan("""{ "rewards": [ { "id": 31301, "fromDay": 8 } ] }""", tables);

        Assert.Contains(plan.Problems, problem => problem.Contains("fromDay"));
        Assert.DoesNotContain(plan.Rows, row => row.Id == 31301);
    }

    [Fact]
    public void From_day_is_only_for_level_one()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "fromDay": 5, "levels": [ {}, {} ] } ] }""");

        Assert.Empty(plan.Problems);
        Assert.Equal(new FieldValue.Number(5), Value(Row(plan, Table.Condition, 31271), "SurvivalToDay"));
        Assert.DoesNotContain(plan.Rows, row => row.Table == Table.Condition && row.Id == 31272);
    }

    [Fact]
    public void Layers_on_a_game_reward_without_gives_changes_its_buff()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31301, "layers": 5 } ] }""", WithWaterBoost());

        Assert.Empty(plan.Problems);
        Assert.Equal(new FieldValue.Number(5), Value(Row(plan, Table.Buff, 313010), "MaxLayerCount"));
    }

    [Fact]
    public void Layers_with_gives_goes_on_the_levels_own_buff()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271, "layers": 3,
              "gives": [ { "effect": "game:AE_PowerSolarEfficiency" }, { "effect": "game:AE_PowerSolarEfficiency" } ] } ] }
            """);

        Assert.Empty(plan.Problems);
        Assert.Equal(new FieldValue.Number(3), Value(Row(plan, Table.Buff, 390010), "MaxLayerCount"));
        Assert.Null(Value(Row(plan, Table.Buff, 390011), "MaxLayerCount"));
        Assert.DoesNotContain(plan.Rows, row => row.Table == Table.Buff && row.Id == 312710);
    }

    [Fact]
    public void Layers_on_a_new_reward_without_gives_is_a_problem_so_the_copied_reward_keeps_its_buff()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "layers": 3 } ] }""");

        Assert.Contains(plan.Problems, problem => problem.Contains("layers"));
        Assert.DoesNotContain(plan.Rows, row => row.Table == Table.Buff);
    }

    [Fact]
    public void To_day_goes_on_every_level()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "toDay": 20, "levels": [ {}, {} ] } ] }""");

        Assert.Empty(plan.Problems);
        Assert.Equal(new Dictionary<int, int> { [31271] = 20, [31272] = 20 }, plan.LastDays);
    }

    [Fact]
    public void A_to_day_before_the_from_day_is_reported_and_kept()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "fromDay": 10, "toDay": 5 } ] }""");

        Assert.Contains(plan.Problems, problem => problem.Contains("toDay of 5, before its fromDay of 10"));
        Assert.Equal(5, plan.LastDays[31271]);
    }

    [Fact]
    public void Always_offered_goes_on_every_level_in_file_order()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271, "alwaysOffered": true, "levels": [ {}, {} ] },
              { "id": 31271, "alwaysOffered": true }, { "id": 39101, "copyFrom": 31271 } ] }
            """);

        Assert.Empty(plan.Problems);
        Assert.Equal(new[] { 39001, 39002, 31271 }, plan.AlwaysOffered);
    }

    [Fact]
    public void Any_day_puts_every_level_in_each_days_main_pool()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "anyDay": 10, "levels": [ {}, {} ] } ] }""", new FakeTables().WithDays());

        Assert.Empty(plan.Problems);
        Assert.Equal(Pools((9102, 10), (9103, 10)), PoolsOf(plan, 39001));
        Assert.Equal(Pools((9102, 10), (9103, 10)), PoolsOf(plan, 39002));
    }

    [Fact]
    public void Any_day_leaves_a_pool_the_reward_names_at_the_weight_given_there()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "anyDay": 10, "pools": { "9103": 3 } } ] }""", new FakeTables().WithDays());

        Assert.Empty(plan.Problems);
        Assert.Equal(Pools((9102, 10), (9103, 3)), PoolsOf(plan, 39001));
    }

    [Fact]
    public void Any_day_follows_a_day_the_file_changes()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271, "anyDay": 10 } ],
              "days": [ { "day": 7, "pools": [ { "pool": 9121, "amount": 2 }, { "pool": 1005, "amount": 1 } ] } ] }
            """, new FakeTables().WithDays());

        Assert.Empty(plan.Problems);
        Assert.Equal(Pools((9102, 10), (9121, 10)), PoolsOf(plan, 39001));
    }

    [Fact]
    public void A_skipped_reward_gets_no_last_day_main_pools_or_card()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "toDay": 5, "anyDay": 10, "alwaysOffered": true } ] }""", new FakeTables().WithDays());

        Assert.Contains(plan.Problems, problem => problem.Contains("was skipped"));
        Assert.Empty(plan.LastDays);
        Assert.Empty(plan.AlwaysOffered);
        Assert.Empty(plan.Pools);
    }

    [Fact]
    public void A_days_main_pool_is_its_biggest_drawn_pool_and_the_first_on_a_tie()
    {
        var sizes = new Dictionary<int, int> { [5] = 10, [6] = 10, [7] = 100 };

        Assert.Equal(5, MainPools.MainOf(new DayEntry(1, new[] { new DayPool(5, 1), new DayPool(6, 2), new DayPool(7, 0) }), pool => sizes[pool]));
        Assert.Equal(0, MainPools.MainOf(new DayEntry(1, new[] { new DayPool(7, 0) }), pool => sizes[pool]));
    }

    private static IEnumerable<PoolEntry> Pools(params (int Group, int Weight)[] pools) => pools.Select(pool => new PoolEntry(pool.Group, pool.Weight));

    private static IEnumerable<PoolEntry> Ones(IEnumerable<int> groups) => groups.Select(group => new PoolEntry(group, 1));
}
