using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Regression tests for the ReadRange-ACK result flags (reported by @BennoMeijer, PR #15).
/// BACnetResultFlags ::= BIT STRING { firstItem(0), lastItem(1), moreItems(2) } is a fixed 3-bit
/// field. <see cref="BacnetClient.ReadRangeResponse"/> builds it with
/// <c>BacnetBitString.ConvertFromInt((uint)status, 3)</c>; deriving the width from the value instead
/// (the old bug) collapsed all-false / high-bit-clear flags into a shorter — non-conformant — bitstring.
/// These lock the fixed-width wire format the call site now produces.
/// </summary>
public class ReadRangeResultFlagsTests
{
    [Theory]
    // context tag 3, 5 unused bits, one MSB-first data byte. Without the explicit width these encoded
    // as 0 / 1 / 2 bits respectively (a shorter bitstring), so a peer saw fewer than three flags.
    [InlineData(BacnetResultFlags.NONE, 0x00)]                                        // 000
    [InlineData(BacnetResultFlags.FIRST_ITEM, 0x80)]                                  // 100
    [InlineData(BacnetResultFlags.FIRST_ITEM | BacnetResultFlags.LAST_ITEM, 0xC0)]    // 110
    [InlineData(BacnetResultFlags.MORE_ITEMS, 0x20)]                                  // 001
    public void ResultFlags_always_encode_three_bits(BacnetResultFlags status, byte expectedData)
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_context_bitstring(buffer, 3, BacnetBitString.ConvertFromInt((uint)status, 3));

        Assert.Equal(new byte[] { 0x3A, 0x05, expectedData }, buffer.ToArray());
    }
}
