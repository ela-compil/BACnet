using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// ASHRAE 135 §5.4.5: a responding BACnet-user must recognise a retransmitted confirmed request
/// (same source, invoke-id and content) and not execute the service again - the original response
/// is retransmitted instead. Without this, one retransmitted WriteProperty is written twice and
/// every subscriber gets duplicate COV notifications.
/// </summary>
public class BacnetClientDuplicateRequestTests
{
    private static byte[] BuildWritePropertyRequest(byte invokeId, float value)
    {
        var buffer = new EncodeBuffer();
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, null);
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY,
            BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1476, invokeId);
        Services.EncodeWriteProperty(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 7),
            (uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL, 0,
            new[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, value) });
        return buffer.ToArray();
    }

    private static (BacnetClient Client, RecordingTransport Transport, List<byte> HandledInvokeIds) MakeServer()
    {
        var transport = new RecordingTransport();
        var client = new BacnetClient(transport);
        var handled = new List<byte>();
        client.OnWritePropertyRequest += (sender, adr, invokeId, objectId, value, maxSegments) =>
        {
            handled.Add(invokeId);
            sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId);
        };
        client.Start();
        return (client, transport, handled);
    }

    private static BacnetAddress Requester() => new(BacnetAddressTypes.IP, "10.0.0.5:47808");

    [Fact]
    public void Retransmitted_request_is_executed_once_and_response_is_retransmitted()
    {
        var (_, transport, handled) = MakeServer();
        var request = BuildWritePropertyRequest(77, 61.5f);

        transport.Receive(request, Requester());
        transport.Receive(request, Requester());

        Assert.Equal(new byte[] { 77 }, handled);
        Assert.Equal(2, transport.Sent.Count); // the SimpleAck and its retransmission
        Assert.Equal(transport.Sent[0].Frame, transport.Sent[1].Frame);
    }

    [Fact]
    public void Different_invoke_ids_are_separate_transactions()
    {
        var (_, transport, handled) = MakeServer();

        transport.Receive(BuildWritePropertyRequest(77, 61.5f), Requester());
        transport.Receive(BuildWritePropertyRequest(78, 61.5f), Requester());

        Assert.Equal(new byte[] { 77, 78 }, handled);
        Assert.Equal(2, transport.Sent.Count);
    }

    [Fact]
    public void Reused_invoke_id_with_different_content_is_a_new_transaction()
    {
        var (_, transport, handled) = MakeServer();

        transport.Receive(BuildWritePropertyRequest(77, 61.5f), Requester());
        transport.Receive(BuildWritePropertyRequest(77, 99.9f), Requester());

        Assert.Equal(new byte[] { 77, 77 }, handled);
        Assert.Equal(2, transport.Sent.Count);
    }

    [Fact]
    public void Requests_from_different_sources_are_separate_transactions()
    {
        var (_, transport, handled) = MakeServer();
        var request = BuildWritePropertyRequest(77, 61.5f);

        transport.Receive(request, Requester());
        transport.Receive(request, new BacnetAddress(BacnetAddressTypes.IP, "10.0.0.6:47808"));

        Assert.Equal(new byte[] { 77, 77 }, handled);
    }

    [Fact]
    public void Detection_can_be_disabled()
    {
        var (client, transport, handled) = MakeServer();
        client.DuplicateRequestDetection = false;
        var request = BuildWritePropertyRequest(77, 61.5f);

        transport.Receive(request, Requester());
        transport.Receive(request, Requester());

        Assert.Equal(new byte[] { 77, 77 }, handled);
    }

    [Fact]
    public void Unhandled_service_reject_is_retransmitted_for_a_duplicate()
    {
        var transport = new RecordingTransport();
        var client = new BacnetClient(transport); // no handlers -> Reject(UNRECOGNIZED_SERVICE)
        client.Start();
        var request = BuildWritePropertyRequest(42, 1f);

        transport.Receive(request, Requester());
        transport.Receive(request, Requester());

        Assert.Equal(2, transport.Sent.Count);
        Assert.Equal(transport.Sent[0].Frame, transport.Sent[1].Frame);
        var npduLen = NPDU.Decode(transport.Sent[0].Frame, 0, out _, out _, out _, out _, out _, out _);
        Assert.Equal(BacnetPduTypes.PDU_TYPE_REJECT, (BacnetPduTypes)transport.Sent[0].Frame[npduLen] & BacnetPduTypes.PDU_TYPE_MASK);
    }
}
