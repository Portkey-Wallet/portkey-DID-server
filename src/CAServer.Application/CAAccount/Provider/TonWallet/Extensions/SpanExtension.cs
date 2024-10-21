using System;

namespace CAServer.CAAccount.Provider.TonWallet.Extensions;

public static class SpanExtension
{
    /// <summary>
    /// Copies data from a source to the target starting at the specified offset.
    /// Updates the offset to reflect the new position after copying.
    /// </summary>
    /// <para>Ensure the target span is large enough to accommodate the data from the source span.</para>
    /// <param name="target">The target span where data will be copied to.</param>
    /// <param name="source">The source span from which data will be copied.</param>
    /// <param name="offset">The starting position in the target span. Updated to the new position after copying.</param>
    public static void CopyFrom(this Span<byte> target, ReadOnlySpan<byte> source, ref int offset)
    {
        source.CopyTo(target.Slice(offset, source.Length));
        offset += source.Length;
    }
}