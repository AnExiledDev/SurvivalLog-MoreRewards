using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using GameCore.HotUpdate.Battle.Logic;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Writes the mod's interest cap into the game's code (<see cref="InterestCap"/>) while the plugin loads, and again on
/// the main thread when MaxInterest changes; the code runs on the main thread when a day ends, so it is never running
/// while it is written. There is no hook, so the 14-byte rule for hook targets does not apply;
/// only two numbers inside the method change. The code is found by name: SettleDailySurvivalPoints's start comes from
/// its il2cpp method info, and the code is searched for in the method (<see cref="InterestCap.FindIn"/>), so a game
/// update that only moves the method changes nothing. Before the method is read, the module's size from its PE header
/// must reach past what is searched, so a module is never read beyond its end. When the code is not found the cap is
/// left as the game's and the reason is logged as an error, so the mod shows red in Mod Manager. The code is in
/// GameAssembly.dll's executable il2cpp section, so it is made writable for the write and given its old protection back.
/// </summary>
internal static class InterestCode
{
    private const string GameModule = "GameAssembly.dll";
    private const uint ExecuteReadWrite = 0x40;

    /// <summary>Where the cap code is, once found; the game's code does not move while it runs.</summary>
    private static IntPtr _code;

    /// <param name="cap">The cap to write; <see cref="InterestCap.GameCap"/> puts the game's back.</param>
    /// <returns>Whether the code now holds <paramref name="cap"/>.</returns>
    public static bool Apply(int cap, Action<string> log, Action<string> logError)
    {
        var keepsGameCap = "the interest cap is left as it was";

        try
        {
            if (_code == IntPtr.Zero && Find() is { } problem)
            {
                logError($"{problem}, {keepsGameCap}");

                return false;
            }

            var code = new byte[InterestCap.GameCode.Count];

            Marshal.Copy(_code, code, 0, code.Length);

            if (InterestCap.Rewrite(code, cap) is not { } rewritten)
            {
                logError($"the game's interest code changed since it was found, {keepsGameCap}");

                return false;
            }

            Write(_code, rewritten, log);
            log(cap == InterestCap.GameCap ? $"interest is back to the game's cap of {cap}" : $"interest is now up to {cap} survival points a day (the game's cap is {InterestCap.GameCap})");

            return true;
        }
        catch (Exception ex)
        {
            logError($"could not change the interest cap, {keepsGameCap}: {ex}");

            return false;
        }
    }

    /// <summary>Finds the cap code in SettleDailySurvivalPoints and keeps where it is.</summary>
    /// <returns>Null once found; otherwise why it was not.</returns>
    private static string? Find()
    {
        var module = GetModuleHandleW(GameModule);

        if (module == IntPtr.Zero) return $"could not find {GameModule}";

        var method = MethodStart();

        if (method == IntPtr.Zero) return "could not find SettleDailySurvivalPoints in the game (a game update?)";

        var firstPage = new byte[PeHeader.PageSize];

        Marshal.Copy(module, firstPage, 0, firstPage.Length);

        if (PeHeader.SizeOfImage(firstPage) is not { } size || !InterestCap.FitsIn(method.ToInt64() - module.ToInt64(), size))
            return $"SettleDailySurvivalPoints would lie outside {GameModule}, so it is not read";

        var body = new byte[InterestCap.SearchBytes];

        Marshal.Copy(method, body, 0, body.Length);

        if (InterestCap.FindIn(body) is not { } at) return "the game's interest code is not the one this mod knows (a game update?)";

        _code = method + at;

        return null;
    }

    /// <returns>Where SettleDailySurvivalPoints's code starts: the first field of its il2cpp method info, or zero when the interop has no single method info for it.</returns>
    private static IntPtr MethodStart()
    {
        var fields = typeof(SurvivalResultsManager).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(field => field.FieldType == typeof(IntPtr) && field.Name.StartsWith(InterestCap.MethodInfoField, StringComparison.Ordinal))
            .ToList();

        if (fields.Count != 1 || fields[0].GetValue(null) is not IntPtr methodInfo || methodInfo == IntPtr.Zero) return IntPtr.Zero;

        return Marshal.ReadIntPtr(methodInfo);
    }

    private static void Write(IntPtr address, byte[] code, Action<string> log)
    {
        var size = new UIntPtr((uint)code.Length);

        if (!VirtualProtect(address, size, ExecuteReadWrite, out var oldProtection)) throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            Marshal.Copy(code, 0, address, code.Length);
            FlushInstructionCache(GetCurrentProcess(), address, size);
        }
        finally
        {
            RestoreProtection(address, size, oldProtection, log);
        }
    }

    /// <summary>A failure here only leaves the code writable, so it is reported and nothing else is undone.</summary>
    private static void RestoreProtection(IntPtr address, UIntPtr size, uint oldProtection, Action<string> log)
    {
        if (VirtualProtect(address, size, oldProtection, out _)) return;

        log($"could not give the interest code its old memory protection back (error {Marshal.GetLastWin32Error()})");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string moduleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint newProtection, out uint oldProtection);

    [DllImport("kernel32.dll")]
    private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
}
