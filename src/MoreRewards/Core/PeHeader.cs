using System;
using System.Buffers.Binary;

namespace MoreRewards.Core;

/// <summary>Where a module keeps its x64 exception table (.pdata), as an offset from the module's start and a size in bytes.</summary>
public readonly record struct ExceptionTable(int Rva, int Size);

/// <summary>
/// Reads how big a loaded module is, and where its exception table is, from the PE header at its start, so the mod
/// never reads past the module's end. The header sits in the module's first page, which is always mapped.
/// </summary>
public static class PeHeader
{
    public const int PageSize = 0x1000;

    /// <summary>Where the DOS header keeps the offset of the "PE\0\0" header (e_lfanew).</summary>
    private const int PeOffsetAt = 0x3c;

    /// <summary>The optional header starts after the 4-byte signature and the 20-byte file header.</summary>
    private const int OptionalHeaderAfterPe = 4 + 20;

    /// <summary>SizeOfImage, 56 bytes into the optional header (PE32 and PE32+ alike).</summary>
    private const int SizeOfImageAt = 56;

    /// <summary>The optional header's magic number for a 64-bit module, the only kind with an x64 exception table.</summary>
    private const ushort Pe32Plus = 0x20b;

    /// <summary>NumberOfRvaAndSizes in a PE32+ optional header; the data directories follow it.</summary>
    private const int DirectoryCountAt = 108;

    /// <summary>The exception table is data directory 3, 8 bytes each, from 112 bytes into a PE32+ optional header.</summary>
    private const int ExceptionDirectory = 3;

    private const int DirectoriesAt = 112;

    /// <returns>The module's SizeOfImage, or null when <paramref name="firstPage"/> does not hold a PE header.</returns>
    public static int? SizeOfImage(byte[] firstPage)
    {
        if (OptionalHeader(firstPage, SizeOfImageAt + 4) is not { } optional) return null;

        var size = BinaryPrimitives.ReadInt32LittleEndian(firstPage.AsSpan(optional + SizeOfImageAt));

        return size > 0 ? size : null;
    }

    /// <returns>Where the module's exception table is, or null when <paramref name="firstPage"/> holds no 64-bit PE header with one.</returns>
    public static ExceptionTable? ExceptionTableOf(byte[] firstPage)
    {
        var directoryAt = DirectoriesAt + ExceptionDirectory * 8;

        if (OptionalHeader(firstPage, directoryAt + 8) is not { } optional) return null;

        if (BinaryPrimitives.ReadUInt16LittleEndian(firstPage.AsSpan(optional)) != Pe32Plus) return null;

        if (BinaryPrimitives.ReadUInt32LittleEndian(firstPage.AsSpan(optional + DirectoryCountAt)) <= ExceptionDirectory) return null;

        var rva = BinaryPrimitives.ReadInt32LittleEndian(firstPage.AsSpan(optional + directoryAt));
        var size = BinaryPrimitives.ReadInt32LittleEndian(firstPage.AsSpan(optional + directoryAt + 4));

        return rva > 0 && size > 0 ? new ExceptionTable(rva, size) : null;
    }

    /// <returns>
    /// Where the optional header starts in <paramref name="firstPage"/>, when the page holds a PE header with at least
    /// <paramref name="bytesNeeded"/> bytes of optional header; otherwise null.
    /// </returns>
    private static int? OptionalHeader(byte[] firstPage, int bytesNeeded)
    {
        if (firstPage.Length < PeOffsetAt + 4 || firstPage[0] != 'M' || firstPage[1] != 'Z') return null;

        var peAt = BinaryPrimitives.ReadInt32LittleEndian(firstPage.AsSpan(PeOffsetAt));

        if (peAt < 0 || peAt > firstPage.Length - OptionalHeaderAfterPe - bytesNeeded) return null;

        if (firstPage[peAt] != 'P' || firstPage[peAt + 1] != 'E' || firstPage[peAt + 2] != 0 || firstPage[peAt + 3] != 0) return null;

        return peAt + OptionalHeaderAfterPe;
    }
}
