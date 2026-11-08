using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The cap is written at a fixed address, which moves with every game update. This reads the game's own
/// GameAssembly.dll (the copy the decomp was made from) and checks the code the mod expects is really there.
/// </summary>
public class InterestCodeInGameTests
{
    private static readonly string GameAssembly = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "sl-re", "ida", "GameAssembly.dll");

    [Fact]
    public void The_game_holds_its_interest_code_at_the_address_the_mod_writes()
    {
        Assert.True(File.Exists(GameAssembly), $"the game's GameAssembly.dll is needed at {GameAssembly}");

        var file = File.ReadAllBytes(GameAssembly);
        var offset = FileOffset(file, InterestCap.GameCodeRva);
        var code = file.AsSpan(offset, InterestCap.GameCode.Count).ToArray();

        Assert.Equal(InterestCap.GameCode, code);
        Assert.Equal(InterestCap.GameCap, InterestCap.CapIn(code));
    }

    /// <summary>Where <paramref name="rva"/> lies in the file, from the PE section table.</summary>
    private static int FileOffset(byte[] file, int rva)
    {
        var pe = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(0x3c));
        var sections = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(pe + 6));
        var optionalSize = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(pe + 20));
        var table = pe + 24 + optionalSize;

        foreach (var at in Enumerable.Range(0, sections).Select(i => table + i * 40))
        {
            var virtualAddress = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 12));
            var rawSize = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 16));
            var rawAt = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 20));

            if (rva >= virtualAddress && rva < virtualAddress + rawSize) return rva - virtualAddress + rawAt;
        }

        throw new InvalidOperationException($"rva 0x{rva:x} is in no section of {GameAssembly}");
    }
}
