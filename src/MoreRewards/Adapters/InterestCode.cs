using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Writes the mod's interest cap into the game's code (<see cref="InterestCap"/>), once, while the plugin loads: no day
/// has ended yet, so the code is not running. There is no hook, so the 14-byte rule for hook targets does not apply;
/// only two numbers inside the method change, and the bytes around them are checked first, so a game build with
/// different code keeps its own cap. The code is in GameAssembly.dll's executable il2cpp section, so it is made
/// writable for the write and given its old protection back.
/// </summary>
internal static class InterestCode
{
    private const string GameModule = "GameAssembly.dll";
    private const int GameCodeRva = 0x269a4c1;
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

            var address = module + GameCodeRva;
            var code = new byte[InterestCap.GameCode.Count];

            Marshal.Copy(address, code, 0, code.Length);

            if (InterestCap.Rewrite(code, cap) is not { } rewritten)
            {
                log($"the game's interest code is not the one this mod knows (a game update?), {keepsGameCap}");

                return;
            }

            Write(address, rewritten);
            log($"interest is now up to {cap} survival points a day (the game's own cap is {InterestCap.GameCap})");
        }
        catch (Exception ex)
        {
            log($"could not change the interest cap, {keepsGameCap}: {ex}");
        }
    }

    private static void Write(IntPtr address, byte[] code)
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
            VirtualProtect(address, size, oldProtection, out _);
        }
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
