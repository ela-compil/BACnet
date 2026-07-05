using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// The maxApdu constructor parameter drives MaxAdpuLength, which sizes
/// segmented responses and is advertised in I-Am / confirmed request headers.
/// Constructing a transport opens no sockets (that happens in Start).
/// </summary>
public class TransportMaxApduTests
{
    [Fact]
    public void Ipv4_transport_defaults_to_1476_and_accepts_lower_max_apdu()
    {
        Assert.Equal(BacnetMaxAdpu.MAX_APDU1476,
            new BacnetIpUdpProtocolTransport(0xBAC0).MaxAdpuLength);
        Assert.Equal(BacnetMaxAdpu.MAX_APDU1024,
            new BacnetIpUdpProtocolTransport(0xBAC0, maxApdu: BacnetMaxAdpu.MAX_APDU1024).MaxAdpuLength);
    }

    [Fact]
    public void Ipv6_transport_defaults_to_1476_and_accepts_lower_max_apdu()
    {
        Assert.Equal(BacnetMaxAdpu.MAX_APDU1476,
            new BacnetIpV6UdpProtocolTransport(0xBAC0).MaxAdpuLength);
        Assert.Equal(BacnetMaxAdpu.MAX_APDU1024,
            new BacnetIpV6UdpProtocolTransport(0xBAC0, maxApdu: BacnetMaxAdpu.MAX_APDU1024).MaxAdpuLength);
    }
}
