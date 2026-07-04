using System.Collections.Generic;
using System.Diagnostics;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Segmented ComplexACK transmission: segments must respect the requester's
/// max-APDU-length-accepted (135 §5.2.1), must never write past the transport
/// buffer (previously an IndexOutOfRangeException with the default 1472-byte
/// payload), and the reassembled segments must equal the unsegmented encoding.
/// </summary>
public class BacnetClientSegmentationTests
{
    private static readonly BacnetObjectId TestObject = new(BacnetObjectTypes.OBJECT_DEVICE, 1234);
    private static readonly BacnetPropertyReference TestProperty =
        new((uint)BacnetPropertyIds.PROP_DESCRIPTION, ASN1.BACNET_ARRAY_ALL);

    private static BacnetValue MakeValue(int payloadLength)
    {
        var text = new StringBuilder(payloadLength);
        while (text.Length < payloadLength)
            text.Append("0123456789");
        return new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
            text.ToString(0, payloadLength));
    }

    private static byte[] ReferenceEncoding(BacnetValue value)
    {
        var buffer = new EncodeBuffer(); // expandable
        Services.EncodeReadPropertyAcknowledge(buffer, TestObject,
            TestProperty.propertyIdentifier, TestProperty.propertyArrayIndex, new[] { value });
        return buffer.ToArray();
    }

    private static List<byte[]> WaitForCompleteResponse(RecordingTransport transport)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 5000)
        {
            lock (transport.Sent)
            {
                var complexAcks = transport.Sent
                    .Select(s => s.Frame)
                    .Where(f => ((BacnetPduTypes)f[2] & BacnetPduTypes.PDU_TYPE_MASK) == BacnetPduTypes.PDU_TYPE_COMPLEX_ACK)
                    .ToList();
                if (complexAcks.Count > 0 &&
                    ((BacnetPduTypes)complexAcks[complexAcks.Count - 1][2] & BacnetPduTypes.MORE_FOLLOWS) == 0)
                    return complexAcks;
            }
            Thread.Sleep(20);
        }
        Assert.Fail("segmented response did not complete within 5 s");
        return null;
    }

    private static byte[] Reassemble(IEnumerable<byte[]> frames, out int maxApduLength)
    {
        var content = new List<byte>();
        var expectedSequence = 0;
        maxApduLength = 0;
        foreach (var frame in frames)
        {
            var npduLen = NPDU.Decode(frame, 0, out _, out _, out _, out _, out _, out _);
            var apduLength = frame.Length - npduLen;
            maxApduLength = Math.Max(maxApduLength, apduLength);

            var type = (BacnetPduTypes)frame[npduLen];
            Assert.True((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0, "every frame should be a segment");
            // bits 1-0 of a ComplexACK type octet are reserved and must be 0
            // (135 §20.1.5) - the SERVER flag belongs to SegmentACK only
            Assert.Equal(0, (byte)type & 0x03);
            Assert.Equal(expectedSequence, frame[npduLen + 2]);
            expectedSequence++;

            // [type][invoke-id][sequence][window][service-choice] payload
            content.AddRange(frame.Skip(npduLen + 5));
        }
        return content.ToArray();
    }

    [Fact]
    public void Segments_respect_requester_max_apdu()
    {
        var transport = new RecordingTransport(maxPayload: 1472, maxApdu: BacnetMaxAdpu.MAX_APDU1476)
        {
            AutoSegmentAck = true
        };
        var client = new BacnetClient(transport);
        client.Start();

        var adr = new BacnetAddress(BacnetAddressTypes.IP, "10.0.0.2:47808");
        var value = MakeValue(3000);
        var segmentation = client.GetSegmentBuffer(BacnetMaxSegments.MAX_SEG16, BacnetMaxAdpu.MAX_APDU480);

        client.ReadPropertyResponse(adr, 1, segmentation, TestObject, TestProperty, new[] { value });

        var frames = WaitForCompleteResponse(transport);
        var reassembled = Reassemble(frames, out var maxApduLength);

        Assert.InRange(maxApduLength, 1, 480);
        Assert.Equal(ReferenceEncoding(value), reassembled);
    }

    [Fact]
    public void Segmented_response_fits_default_transport_buffer_without_throwing()
    {
        // default maxPayload (1472) is smaller than the 1476 APDU limit:
        // this combination previously crashed the segment encoder with an
        // IndexOutOfRangeException on the first oversized response
        var transport = new RecordingTransport(maxPayload: 1472, maxApdu: BacnetMaxAdpu.MAX_APDU1476)
        {
            AutoSegmentAck = true
        };
        var client = new BacnetClient(transport);
        client.Start();

        var adr = new BacnetAddress(BacnetAddressTypes.IP, "10.0.0.2:47808");
        var value = MakeValue(4000);
        var segmentation = client.GetSegmentBuffer(BacnetMaxSegments.MAX_SEG16, null);

        client.ReadPropertyResponse(adr, 1, segmentation, TestObject, TestProperty, new[] { value });

        var frames = WaitForCompleteResponse(transport);
        var reassembled = Reassemble(frames, out var maxApduLength);

        Assert.InRange(maxApduLength, 1, transport.MaxBufferLength);
        Assert.Equal(ReferenceEncoding(value), reassembled);
    }

    [Fact]
    public void GetSegmentBuffer_inside_request_handler_captures_requester_max_apdu()
    {
        var transport = new RecordingTransport();
        var client = new BacnetClient(transport);
        BacnetClient.Segmentation captured = null;
        client.OnReadPropertyRequest += (sender, adr, invokeId, objectId, property, maxSegments) =>
        {
            captured = sender.GetSegmentBuffer(maxSegments);
        };
        client.Start();

        var request = new EncodeBuffer(new byte[64], 0);
        NPDU.Encode(request, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, null);
        APDU.EncodeConfirmedServiceRequest(request,
            BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY,
            BacnetMaxSegments.MAX_SEG16, BacnetMaxAdpu.MAX_APDU480, 1);
        Services.EncodeReadProperty(request, TestObject, (uint)BacnetPropertyIds.PROP_OBJECT_NAME);

        transport.Receive(request.ToArray(), new BacnetAddress(BacnetAddressTypes.IP, "10.0.0.2:47808"));

        Assert.NotNull(captured);
        Assert.Equal(BacnetMaxAdpu.MAX_APDU480, captured.RequesterMaxAdpu);
    }
}
