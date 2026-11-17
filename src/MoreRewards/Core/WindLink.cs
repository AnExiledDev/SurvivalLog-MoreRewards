using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MoreRewards.Core;

/// <summary>
/// Wind Power's turbine multipliers, found by name so this mod loads and runs without Wind Power. The method is
/// WindPower.WindBoosts.Add(string source, Func&lt;float&gt; multiplier), in Wind Power 0.9.3 or newer; every
/// turbine's rating is multiplied by what the function returns, read once a second.
/// </summary>
public static class WindLink
{
    public const string Guid = "com.dev.survivallog.windpower";
    public const string TypeName = "WindPower.WindBoosts";

    /// <summary>Hands the multiplier to Wind Power. False when no loaded assembly has Wind Power's Add.</summary>
    public static bool Add(IEnumerable<Assembly> assemblies, string source, Func<float> multiplier)
    {
        var add = Find(assemblies);

        if (add == null) return false;

        add.Invoke(null, new object[] { source, multiplier });

        return true;
    }

    /// <summary>The share more power the turbines make: the wind rewards owned, added up.</summary>
    public static double Share(IEnumerable<MrGrant> grants, Func<int, bool> isOwned) =>
        grants.Where(grant => grant.Effect == RewardPlan.WindPower && isOwned(grant.RewardId)).Sum(grant => ShareOf(grant.Fields));

    /// <summary>What the turbines make with the wind rewards owned: 1 plus their share, never below the turbines' own.</summary>
    public static float Multiplier(double share) => double.IsNaN(share) ? 1f : 1f + (float)Math.Max(0.0, share);

    private static double ShareOf(IReadOnlyList<FieldSet> fields) =>
        fields.LastOrDefault(field => field.Field == RewardPlan.WindShare)?.Value is FieldValue.Number number ? number.Value : 0;

    private static MethodInfo? Find(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            Type? type;

            try
            {
                type = assembly.GetType(TypeName);
            }
            catch (Exception)
            {
                continue;
            }

            var add = type?.GetMethod("Add", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(Func<float>) }, null);

            if (add != null) return add;
        }

        return null;
    }
}
