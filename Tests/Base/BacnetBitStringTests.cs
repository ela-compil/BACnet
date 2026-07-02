using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// BacnetBitString.ConvertFromInt bit-length calculation (regression for #8 / #11):
/// bits_used must be floor(log2(value)) + 1, which the old Math.Ceiling(log2) got wrong
/// for exact powers of two and for value 1.
/// </summary>
public class BacnetBitStringTests
{
    [Theory]
    [InlineData(1u, 1)]    // 0b1
    [InlineData(2u, 2)]    // 0b10
    [InlineData(3u, 2)]    // 0b11
    [InlineData(4u, 3)]    // 0b100  - old Ceiling(log2) gave 2
    [InlineData(5u, 3)]
    [InlineData(7u, 3)]    // 0b111
    [InlineData(8u, 4)]    // 0b1000 - old gave 3
    [InlineData(255u, 8)]
    [InlineData(256u, 9)]  // 0b1_0000_0000
    public void ConvertFromInt_uses_correct_bit_length(uint value, int expectedBits)
    {
        Assert.Equal(expectedBits, BacnetBitString.ConvertFromInt(value).bits_used);
    }

    [Theory]
    [InlineData(0b0000u)] // no flags
    [InlineData(0b0010u)] // fault only
    [InlineData(0b1111u)] // all four
    public void ConvertFromInt_honors_explicit_bits_used(uint value)
    {
        // BACnetStatusFlags is a fixed 4-bit string regardless of which bits are set.
        Assert.Equal(4, BacnetBitString.ConvertFromInt(value, 4).bits_used);
    }
}
