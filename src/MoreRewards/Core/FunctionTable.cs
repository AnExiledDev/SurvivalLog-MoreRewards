using System;
using System.Buffers.Binary;

namespace MoreRewards.Core;

/// <summary>Reads <paramref name="count"/> bytes of a loaded module, starting <paramref name="rva"/> bytes from the module's start.</summary>
public delegate byte[] ReadModule(int rva, int count);

/// <summary>
/// Finds where a game method ends from the module's x64 exception table (.pdata), so a search for its code never runs
/// on into the next method. The table has one 12-byte entry per piece of compiled code, sorted by start: the piece's
/// start, its end, and where its unwind info is. A long method can be split over several entries, its own first, then
/// pieces whose unwind info chains back to it. SettleDailySurvivalPoints in game 1.1.20719 is 247 bytes of setup in its
/// own entry and 2745 bytes in a chained one, and the interest code this mod changes is in the chained one. So a method runs
/// from its own entry's start to the end of the last piece right after it that chains back to it.
/// </summary>
public static class FunctionTable
{
    private const int EntrySize = 12;

    /// <summary>UNW_FLAG_CHAININFO: the unwind info ends with the entry of the piece this one belongs to.</summary>
    private const int ChainInfo = 0x4;

    /// <summary>RUNTIME_FUNCTION_INDIRECT: the entry points at another entry, not at unwind info. Not followed.</summary>
    private const int Indirect = 0x1;

    /// <summary>No compiler chains this many pieces; a chain this long means the table is broken.</summary>
    private const int MostChainLinks = 32;

    /// <returns>
    /// How many bytes long the method starting <paramref name="methodRva"/> bytes into the module is, or null when no
    /// entry starts there, the entry there is only a piece of another method, or any of it lies outside the module.
    /// </returns>
    public static int? MethodLength(ReadModule read, int sizeOfImage, ExceptionTable table, long methodRva)
    {
        var module = new Module(read, sizeOfImage);

        if (table.Rva < 0 || table.Size < 0 || (long)table.Rva + table.Size > sizeOfImage) return null;

        if (Starting(module, table, methodRva) is not { } own || RootOf(module, own) != methodRva) return null;

        var end = own.End;

        for (var index = own.Index + 1; index < table.Size / EntrySize; index++)
        {
            if (EntryAt(module, table, index) is not { } piece || piece.Begin != end || RootOf(module, piece) != methodRva) break;

            end = piece.End;
        }

        return end - (int)methodRva;
    }

    /// <returns>The entry starting at <paramref name="rva"/>, found by a binary search of the sorted table, or null when none does.</returns>
    private static Entry? Starting(Module module, ExceptionTable table, long rva)
    {
        var low = 0;
        var high = table.Size / EntrySize - 1;

        while (low <= high)
        {
            var middle = low + (high - low) / 2;

            if (EntryAt(module, table, middle) is not { } entry) return null;

            if (entry.Begin == rva) return entry;

            if (entry.Begin < rva) low = middle + 1;
            else high = middle - 1;
        }

        return null;
    }

    /// <returns>Where the method holding <paramref name="piece"/> starts, following its chain of unwind info, or null when the chain can't be followed.</returns>
    private static long? RootOf(Module module, Entry piece)
    {
        var entry = piece;

        for (var link = 0; link < MostChainLinks; link++)
        {
            if ((entry.Unwind & Indirect) != 0 || module.Read(entry.Unwind, 4) is not { } header) return null;

            if (((header[0] >> 3) & ChainInfo) == 0) return entry.Begin;

            // The unwind codes are 2 bytes each, padded to an even count; the parent's entry follows them.
            var codes = (header[2] + 1) & ~1;

            if (module.Read((long)entry.Unwind + 4 + codes * 2, EntrySize) is not { } parent) return null;

            entry = Parse(parent, -1);
        }

        return null;
    }

    /// <returns>Entry <paramref name="index"/> of the table, or null when it can't be read or doesn't describe code inside the module.</returns>
    private static Entry? EntryAt(Module module, ExceptionTable table, int index)
    {
        if (module.Read(table.Rva + (long)index * EntrySize, EntrySize) is not { } bytes) return null;

        var entry = Parse(bytes, index);

        return entry.Begin >= 0 && entry.Begin < entry.End && entry.End <= module.Size ? entry : null;
    }

    private static Entry Parse(byte[] bytes, int index) => new(
        index,
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0)),
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4)),
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)));

    /// <summary>One entry of the table: <paramref name="Index"/> is its place in the table, -1 for one read out of unwind info.</summary>
    private readonly record struct Entry(int Index, int Begin, int End, int Unwind);

    /// <summary>The module, read only inside its own size.</summary>
    private sealed class Module(ReadModule read, int size)
    {
        public int Size => size;

        public byte[]? Read(long rva, int count) => rva >= 0 && rva + count <= size ? read((int)rva, count) : null;
    }
}
