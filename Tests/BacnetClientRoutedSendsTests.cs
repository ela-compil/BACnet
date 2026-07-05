using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Unconfirmed sends to a peer behind a BACnet router: the NPDU destination
/// must be the peer's network/MAC (RoutedSource) while the datalink frame goes
/// to the fronting router - the same addressing the Iam fix established.
/// </summary>
public class BacnetClientRoutedSendsTests
{
    private static readonly byte[] PeerMac = { 172, 28, 1, 20, 0xBA, 0xC0 };

    private static (BacnetClient client, RecordingTransport transport, BacnetAddress router) Setup()
    {
        var transport = new RecordingTransport();
        var client = new BacnetClient(transport);
        client.Start();

        var router = new BacnetAddress(BacnetAddressTypes.IP, "172.28.2.10:47808")
        {
            RoutedSource = new BacnetAddress(BacnetAddressTypes.None, 1, PeerMac)
        };
        return (client, transport, router);
    }

    private static void AssertRoutedFrame(RecordingTransport transport, BacnetAddress router)
    {
        var (frame, sentTo) = Assert.Single(transport.Sent);
        Assert.Same(router, sentTo); // datalink frame goes to the router

        NPDU.Decode(frame, 0, out _, out var destination, out _, out _, out _, out _);
        Assert.NotNull(destination); // NPDU carries DNET/DADR of the peer
        Assert.Equal((ushort)1, destination.net);
        Assert.Equal(PeerMac, destination.adr);
    }

    [Fact]
    public void WhoIs_to_routed_receiver_targets_peer_via_router()
    {
        var (client, transport, router) = Setup();
        client.WhoIs(1234, 1234, router);
        AssertRoutedFrame(transport, router);
    }

    [Fact]
    public void WhoIs_without_receiver_broadcasts_locally()
    {
        var (client, transport, _) = Setup();
        client.WhoIs();

        var (frame, sentTo) = Assert.Single(transport.Sent);
        Assert.Equal(transport.GetBroadcastAddress(), sentTo);
        NPDU.Decode(frame, 0, out _, out var destination, out _, out _, out _, out _);
        Assert.Null(destination);
    }

    [Fact]
    public void WhoHas_to_routed_receiver_targets_peer_via_router()
    {
        var (client, transport, router) = Setup();
        client.WhoHas("Room temperature", receiver: router);
        AssertRoutedFrame(transport, router);
    }

    [Fact]
    public void IHave_to_routed_receiver_targets_peer_via_router()
    {
        var (client, transport, router) = Setup();
        client.IHave(new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1234),
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), "Room temperature", receiver: router);
        AssertRoutedFrame(transport, router);
    }

    [Fact]
    public void IHave_without_receiver_broadcasts_locally()
    {
        var (client, transport, _) = Setup();
        client.IHave(new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1234),
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), "Room temperature");

        var (frame, sentTo) = Assert.Single(transport.Sent);
        Assert.Equal(transport.GetBroadcastAddress(), sentTo);
        NPDU.Decode(frame, 0, out _, out var destination, out _, out _, out _, out _);
        Assert.Null(destination);
    }

    [Fact]
    public void SynchronizeTime_to_routed_peer_targets_peer_via_router()
    {
        var (client, transport, router) = Setup();
        client.SynchronizeTime(router, new DateTime(2026, 7, 5, 12, 0, 0, DateTimeKind.Local));
        AssertRoutedFrame(transport, router);
    }

    [Fact]
    public void UnconfirmedPrivateTransfer_to_routed_peer_targets_peer_via_router()
    {
        var (client, transport, router) = Setup();
        client.SendUnconfirmedPrivateTransfer(router, 260, 1, new byte[] { 1, 2, 3 });
        AssertRoutedFrame(transport, router);
    }

    [Fact]
    public void UnconfirmedEventNotification_to_routed_peer_targets_peer_via_router()
    {
        var (client, transport, router) = Setup();
        var eventData = new BacnetEventNotificationData
        {
            processIdentifier = 1,
            initiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1234),
            eventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1),
            timeStamp = new BacnetGenericTime(DateTime.Now, BacnetTimestampTags.TIME_STAMP_DATETIME),
            notificationClass = 1,
            priority = 1,
            // ack-notification carries no event-values production, which keeps
            // this test focused on the NPDU addressing
            notifyType = BacnetNotifyTypes.NOTIFY_ACK_NOTIFICATION,
            fromState = BacnetEventStates.EVENT_STATE_NORMAL,
            toState = BacnetEventStates.EVENT_STATE_NORMAL
        };

        client.SendUnconfirmedEventNotification(router, eventData);
        AssertRoutedFrame(transport, router);
    }
}
