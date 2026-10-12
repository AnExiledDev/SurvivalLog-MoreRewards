using System;
using System.Collections.Generic;
using HarmonyLib;

namespace MoreRewards.Adapters;

/// <summary>Hooking and failure logging shared by the hooks. A hook that cannot be placed costs its feature, never the game.</summary>
internal static class PatchKit
{
    private static readonly HashSet<string> ReportedFailures = new();

    public static Action<string> Log { get; set; } = _ => { };

    /// <param name="parameters">The hooked method's parameter types, so a new overload of it cannot take the hook's place.</param>
    /// <returns>Whether the hook was placed.</returns>
    public static bool Patch(Harmony harmony, Type hooks, Type type, string method, Type[] parameters, string postfix, string done, string consequence)
    {
        try
        {
            harmony.Patch(AccessTools.Method(type, method, parameters), postfix: new HarmonyMethod(hooks, postfix));
            Log(done);

            return true;
        }
        catch (Exception ex)
        {
            Log($"could not hook {type.Name}.{method}, {consequence}: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    /// Runs from hooks the game calls again and again, so each kind of failure is written once; one that repeats
    /// cannot flood the log or hide a different one later.
    /// </summary>
    public static void Fail(string doing, Exception ex)
    {
        if (ReportedFailures.Add(doing)) Log($"failed {doing} (reported once): {ex}");
    }
}
