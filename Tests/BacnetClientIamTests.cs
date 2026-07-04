using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// I-Am addressing: a receiver that carries a RoutedSource (a Who-Is that came
/// through a BACnet router) must produce an I-Am whose NPDU destination is the
/// original source network/address while the frame goes back to the router.
/// </summary>
public class BacnetClientIamTests
{
    [Fact]
    public void Iam_to_routed_receiver_targets_original_source_via_router()
    {
        var transport = new RecordingTransport();
        var client = new BacnetClient(transport);
        client.Start();

        var sourceMac = new byte[] { 172, 28, 1, 20, 0xBA, 0xC0 };
        var router = new BacnetAddress(BacnetAddressTypes.IP, "172.28.2.10:47808")
        {
            RoutedSource = new BacnetAddress(BacnetAddressTypes.None, 1, sourceMac)
        };

        client.Iam(1234, receiver: router);

        var (frame, sentTo) = Assert.Single(transport.Sent);
        Assert.Same(router, sentTo); // datalink frame goes to the router

        NPDU.Decode(frame, 0, out _, out var destination, out _, out _, out _, out _);
        Assert.NotNull(destination); // NPDU carries DNET/DADR of the original source
        Assert.Equal((ushort)1, destination.net);
        Assert.Equal(sourceMac, destination.adr);
    }

    [Fact]
    public void Iam_to_local_receiver_has_no_npdu_destination()
    {
        var transport = new RecordingTransport();
        var client = new BacnetClient(transport);
        client.Start();

        var receiver = new BacnetAddress(BacnetAddressTypes.IP, "10.0.0.2:47808");
        client.Iam(1234, receiver: receiver);

        var (frame, sentTo) = Assert.Single(transport.Sent);
        Assert.Same(receiver, sentTo);

        NPDU.Decode(frame, 0, out _, out var destination, out _, out _, out _, out _);
        Assert.Null(destination);
    }

    [Fact]
    public void Iam_without_receiver_broadcasts_locally()
    {
        var transport = new RecordingTransport();
        var client = new BacnetClient(transport);
        client.Start();

        client.Iam(1234);

        var (frame, sentTo) = Assert.Single(transport.Sent);
        Assert.Equal(transport.GetBroadcastAddress(), sentTo);

        NPDU.Decode(frame, 0, out _, out var destination, out _, out _, out _, out _);
        Assert.Null(destination);
    }
}
