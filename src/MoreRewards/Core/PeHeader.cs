using System;
using System.Buffers.Binary;

namespace MoreRewards.Core;

/// <summary>
/// Reads how big a loaded module is from the PE header at its start, so the mod never reads past the module's end. The
/// header sits in the module's first page, which is always mapped.
/// </summary>
public static class PeHeader
{
    public const int PageSize = 0x1000;

    /// <summary>Where the DOS header keeps the offset of the "PE\0\0" header (e_lfanew).</summary>
    private const int PeOffsetAt = 0x3c;

    /// <summary>SizeOfImage, after the 4-byte signature and the 20-byte file header, 56 bytes into the optional header (PE32 and PE32+ alike).</summary>
    private const int SizeOfImageAfterPe = 4 + 20 + 56;

    /// <returns>The module's SizeOfImage, or null when <paramref name="firstPage"/> does not hold a PE header.</returns>
    public static int? SizeOfImage(byte[] firstPage)
    {
        if (firstPage.Length < PeOffsetAt + 4 || firstPage[0] != 'M' || firstPage[1] != 'Z') return null;

        var peAt = BinaryPrimitives.ReadInt32LittleEndian(firstPage.AsSpan(PeOffsetAt));

        if (peAt < 0 || peAt > firstPage.Length - SizeOfImageAfterPe - 4) return null;

        if (firstPage[peAt] != 'P' || firstPage[peAt + 1] != 'E' || firstPage[peAt + 2] != 0 || firstPage[peAt + 3] != 0) return null;

        var size = BinaryPrimitives.ReadInt32LittleEndian(firstPage.AsSpan(peAt + SizeOfImageAfterPe));

        return size > 0 ? size : null;
    }
}
