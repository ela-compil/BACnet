using System.IO.BACnet.Serialize;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Routed BACnet addresses (NPDU source/destination specifiers) round-trip for any MAC length,
/// including the 6-byte VMAC/SADR of devices reached through a router (#27).
/// </summary>
public class NpduRoutedAddressTests
{
    [Theory]
    [InlineData(1)]   // MS/TP-style 1-byte MAC
    [InlineData(6)]   // 6-byte VMAC - devices behind a router (#27)
    [InlineData(18)]  // IPv6-length MAC
    public void Routed_destination_roundtrips_any_mac_length(int macLen)
    {
        var mac = Enumerable.Range(0xA0, macLen).Select(i => (byte)i).ToArray();
        var dest = new BacnetAddress(BacnetAddressTypes.None, 302, mac);

        var buffer = new EncodeBuffer();
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, dest, null, 0xFF);

        NPDU.Decode(buffer.buffer, 0, out _, out var decoded, out _, out _, out _, out _);

        Assert.NotNull(decoded);
        Assert.Equal((ushort)302, decoded.net);
        Assert.Equal(mac, decoded.adr);
    }

    [Fact]
    public void Routed_source_roundtrips_6_byte_vmac()
    {
        var mac = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
        var source = new BacnetAddress(BacnetAddressTypes.None, 100, mac);

        var buffer = new EncodeBuffer();
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, null, source, 0xFF);

        NPDU.Decode(buffer.buffer, 0, out _, out _, out var decoded, out _, out _, out _);

        Assert.NotNull(decoded);
        Assert.Equal((ushort)100, decoded.net);
        Assert.Equal(mac, decoded.adr);
    }
}
