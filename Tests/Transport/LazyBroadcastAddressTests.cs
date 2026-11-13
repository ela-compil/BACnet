using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Resolving the broadcast address throws on a host with several IPv4 interfaces when no
/// localEndpointIp selects one. Only a broadcast needs that address, so it must not keep a client
/// that never broadcasts from starting (#236).
/// </summary>
public class LazyBroadcastAddressTests
{
    /// <summary>
    /// Opens real sockets: the shared port is taken by any free one, so a BACnet tool running on the
    /// test host does not get in the way.
    /// </summary>
    private sealed class AmbiguousInterfaceTransport() : BacnetIpUdpProtocolTransport(0)
    {
        public int Resolved { get; private set; }

        protected override BacnetAddress _GetBroadcastAddress()
        {
            Resolved++;
            throw new InvalidOperationException("ambiguous broadcast interface");
        }
    }

    [Fact]
    public void A_client_starts_without_resolving_the_broadcast_address()
    {
        var transport = new AmbiguousInterfaceTransport();
        using var client = new BacnetClient(transport);

        client.Start();

        Assert.Equal(0, transport.Resolved);
        // the first broadcast is where an ambiguous interface surfaces
        Assert.Throws<InvalidOperationException>(() => client.WhoIs());
        Assert.Equal(1, transport.Resolved);
    }
}
