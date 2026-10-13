using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Writes the mod's interest cap into the game's code (<see cref="InterestCap"/>), once, while the plugin loads: no day
/// has ended yet, so the code is not running. There is no hook, so the 14-byte rule for hook targets does not apply;
/// only two numbers inside the method change, and the bytes around them are checked first, so a game build with
/// different code keeps its own cap. Before those bytes are read, the module's size from its PE header must reach past
/// them, so a smaller module is never read beyond its end. The code is in GameAssembly.dll's executable il2cpp section, so it is made
/// writable for the write and given its old protection back.
/// </summary>
internal static class InterestCode
{
    private const string GameModule = "GameAssembly.dll";
    private const uint ExecuteReadWrite = 0x40;

    public static void Apply(int cap, Action<string> log)
    {
        var keepsGameCap = $"interest keeps the game's cap of {InterestCap.GameCap}";

        try
        {
            var module = GetModuleHandleW(GameModule);

            if (module == IntPtr.Zero)
            {
                log($"could not find {GameModule}, {keepsGameCap}");

                return;
            }

            if (!HoldsGameCode(module))
            {
                log($"the interest code would lie outside {GameModule} (a game update?), so it is not read; {keepsGameCap}");

                return;
            }

            var address = module + InterestCap.GameCodeRva;
            var code = new byte[InterestCap.GameCode.Count];

            Marshal.Copy(address, code, 0, code.Length);

            if (InterestCap.Rewrite(code, cap) is not { } rewritten)
            {
                log($"the game's interest code is not the one this mod knows (a game update?), {keepsGameCap}");

                return;
            }

            Write(address, rewritten, log);
            log($"interest is now up to {cap} survival points a day (the game's own cap is {InterestCap.GameCap})");
        }
        catch (Exception ex)
        {
            log($"could not change the interest cap, {keepsGameCap}: {ex}");
        }
    }

    /// <summary>Whether the module's size, from its PE header, reaches past the interest code.</summary>
    private static bool HoldsGameCode(IntPtr module)
    {
        var firstPage = new byte[PeHeader.PageSize];

        Marshal.Copy(module, firstPage, 0, firstPage.Length);

        return PeHeader.SizeOfImage(firstPage) is { } size && InterestCap.FitsIn(size);
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
