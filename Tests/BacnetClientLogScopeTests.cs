using System.IO.BACnet.Tests.Support;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Every log entry about a request or an answer carries the address of the remote device as a
/// logging scope, so that a sink serving a client that talks to several devices can tell them
/// apart. Broadcasts have no remote device and no scope.
/// </summary>
public class BacnetClientLogScopeTests
{
    private static readonly BacnetAddress Device = new(BacnetAddressTypes.IP, "10.0.0.2:47808");

    [Fact]
    public void Requests_are_logged_within_the_scope_of_the_remote_device()
    {
        var logger = new RecordingLogger();
        var client = new BacnetClient(new RecordingTransport()) { Log = logger };
        client.Start();

        client.BeginReadPropertyRequest(Device, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1),
            BacnetPropertyIds.PROP_PRESENT_VALUE, waitForTransmit: false);

        var entry = logger.Entry("Sending ReadPropertyRequest");
        var scope = Assert.Single(entry.Scopes);
        Assert.Equal(Device, RecordingLogger.RemoteAddress(scope));
        Assert.Equal(Device.ToString(), scope.ToString());
    }

    [Fact]
    public void Unconfirmed_requests_are_logged_within_the_scope_of_the_receiver()
    {
        var logger = new RecordingLogger();
        var client = new BacnetClient(new RecordingTransport()) { Log = logger };
        client.Start();

        client.Iam(1234, receiver: Device);

        var scope = Assert.Single(logger.Entry("Sending Iam").Scopes);
        Assert.Equal(Device, RecordingLogger.RemoteAddress(scope));
    }

    [Fact]
    public void Broadcasts_are_logged_without_a_remote_device_scope()
    {
        var logger = new RecordingLogger();
        var client = new BacnetClient(new RecordingTransport()) { Log = logger };
        client.Start();

        client.WhoIs();

        Assert.Empty(logger.Entry("Broadcasting WhoIs").Scopes);
    }

    [Fact]
    public void Received_frames_and_their_responses_are_logged_within_the_scope_of_the_sender()
    {
        var (transportA, transportB) = LoopbackTransport.CreatePair();
        var clientLogger = new RecordingLogger();
        var serverLogger = new RecordingLogger();
        using var client = new BacnetClient(transportA, timeout: 500) { Log = clientLogger };
        using var server = new BacnetClient(transportB, timeout: 500) { Log = serverLogger };
        client.Start();
        server.Start();

        BacnetAddress requester = null;
        server.OnWritePropertyRequest += (sender, adr, invokeId, objectId, value, maxSegments) =>
        {
            requester = adr;
            sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId);
        };

        client.WritePropertyRequest(client.Transport.GetBroadcastAddress(),
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), BacnetPropertyIds.PROP_PRESENT_VALUE,
            new[] { new BacnetValue(21.5f) });

        Assert.NotNull(requester);

        // the server logs the request and the answer it sends within the scope of the requester
        var request = serverLogger.Entry("ConfirmedServiceRequest SERVICE_CONFIRMED_WRITE_PROPERTY");
        Assert.Equal(requester, RecordingLogger.RemoteAddress(Assert.Single(request.Scopes)));

        var response = serverLogger.Entry("Sending SimpleAckResponse");
        Assert.All(response.Scopes, scope => Assert.Equal(requester, RecordingLogger.RemoteAddress(scope)));

        // the client logs the answer within the scope of the device that sent it - the loopback
        // delivers on the requesting thread, so the scope of the request itself is still open too
        var ack = clientLogger.Entry("Received SimpleAck");
        Assert.NotEmpty(ack.Scopes);
        Assert.All(ack.Scopes, scope => Assert.NotNull(RecordingLogger.RemoteAddress(scope)));
    }
}
