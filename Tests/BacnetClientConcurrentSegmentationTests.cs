using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Segmentation state belongs to a transfer, and a transfer is a device plus the invoke-id that
/// device chose: two panels numbering their own traffic independently both send an invoke-id 1, and
/// frames are processed concurrently (the UDP transport re-arms its receive before handling the
/// datagram it just took), so state keyed by the invoke-id alone mixes the segments of one device
/// into the transfer of another, and a single segment-ack slot lets the ack of one device discard
/// the ack of another.
/// </summary>
public class BacnetClientConcurrentSegmentationTests
{
    private const BacnetConfirmedServices Service = BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY;

    private static BacnetAddress Device(int index) => new(BacnetAddressTypes.IP, $"10.0.0.{index}:47808");

    /// <summary>A segment of a ComplexACK: [type][invoke-id][sequence][window][service] payload.</summary>
    private static byte[] Segment(byte invokeId, byte sequenceNumber, bool moreFollows, params byte[] payload)
    {
        var type = BacnetPduTypes.PDU_TYPE_COMPLEX_ACK | BacnetPduTypes.SEGMENTED_MESSAGE |
                   (moreFollows ? BacnetPduTypes.MORE_FOLLOWS : 0);

        var buffer = new EncodeBuffer(new byte[payload.Length + 16], 0);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, null);
        APDU.EncodeComplexAck(buffer, type, Service, invokeId, sequenceNumber, 1);
        buffer.Add(payload, payload.Length);
        return buffer.ToArray();
    }

    private static byte[] SegmentAck(byte invokeId, byte sequenceNumber, byte windowSize)
    {
        var buffer = new EncodeBuffer(new byte[16], 0);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, null);
        APDU.EncodeSegmentAck(buffer, BacnetPduTypes.PDU_TYPE_SEGMENT_ACK, invokeId, sequenceNumber, windowSize);
        return buffer.ToArray();
    }

    /// <summary>The payload of every ComplexACK the client assembled, in arrival order.</summary>
    private static List<byte[]> RecordAssembled(BacnetClient client)
    {
        var assembled = new List<byte[]>();

        client.OnComplexAck += (sender, adr, type, service, invokeId, buffer, offset, length) =>
        {
            lock (assembled)
                assembled.Add(buffer.Skip(offset).Take(length).ToArray());
        };

        return assembled;
    }

    [Fact]
    public void Two_devices_can_run_a_segmented_transfer_with_the_same_invoke_id()
    {
        var transport = new RecordingTransport();
        using var client = new BacnetClient(transport);
        client.Start();
        var assembled = RecordAssembled(client);

        // both devices answer with invoke-id 1, interleaved, two segments each
        transport.Receive(Segment(1, 0, true, 0xA0), Device(2));
        transport.Receive(Segment(1, 0, true, 0xB0), Device(3));
        transport.Receive(Segment(1, 1, false, 0xA1), Device(2));
        transport.Receive(Segment(1, 1, false, 0xB1), Device(3));

        Assert.Equal(2, assembled.Count);
        Assert.Contains(assembled, payload => payload.SequenceEqual(new byte[] { 0xA0, 0xA1 }));
        Assert.Contains(assembled, payload => payload.SequenceEqual(new byte[] { 0xB0, 0xB1 }));
    }

    [Fact]
    public void Segmented_transfers_of_several_devices_assemble_while_they_run_in_parallel()
    {
        const int devices = 4;
        const int transfersPerDevice = 25;

        var transport = new RecordingTransport();
        using var client = new BacnetClient(transport);
        client.Start();
        var assembled = RecordAssembled(client);

        Parallel.For(0, devices, device =>
        {
            for (var transfer = 0; transfer < transfersPerDevice; transfer++)
            {
                // every device numbers its own transfers, so they all use invoke-id 1
                transport.Receive(Segment(1, 0, true, (byte)device, (byte)transfer), Device(device + 2));
                transport.Receive(Segment(1, 1, false, (byte)device, (byte)transfer), Device(device + 2));
            }
        });

        var expected = Enumerable.Range(0, devices).SelectMany(device =>
            Enumerable.Range(0, transfersPerDevice).Select(transfer =>
                new byte[] { (byte)device, (byte)transfer, (byte)device, (byte)transfer }));

        Assert.Equal(devices * transfersPerDevice, assembled.Count);
        Assert.All(expected, payload => Assert.Contains(assembled, a => a.SequenceEqual(payload)));
    }

    [Fact]
    public void The_segment_ack_of_one_device_does_not_discard_the_ack_of_another()
    {
        var transport = new RecordingTransport();
        using var client = new BacnetClient(transport);
        client.Start();

        // both acks arrive before the sending threads get to wait for them
        transport.Receive(SegmentAck(2, 5, 1), Device(3));
        transport.Receive(SegmentAck(1, 0, 1), Device(2));

        var deviceB = new BacnetClient.Segmentation();
        Assert.True(client.WaitForSegmentAck(Device(3), 2, deviceB, 200), "the segment ack of device B was lost");
        Assert.Equal(6, deviceB.sequence_number);
        Assert.Equal(1, deviceB.window_size);

        var deviceA = new BacnetClient.Segmentation();
        Assert.True(client.WaitForSegmentAck(Device(2), 1, deviceA, 200), "the segment ack of device A was lost");
        Assert.Equal(1, deviceA.sequence_number);
    }
}
