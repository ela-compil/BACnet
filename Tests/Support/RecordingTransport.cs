using System.Collections.Generic;
using System.IO.BACnet.Serialize;

namespace System.IO.BACnet.Tests.Support;

/// <summary>
/// In-memory transport: records every frame passed to <see cref="Send"/> and
/// lets tests inject incoming frames. With <see cref="AutoSegmentAck"/> it
/// acknowledges each segmented ComplexACK like a remote requester would, so
/// segmented responses run to completion without a network.
/// </summary>
public class RecordingTransport : BacnetTransportBase
{
    public List<(byte[] Frame, BacnetAddress Address)> Sent { get; } = [];
    public bool AutoSegmentAck { get; set; }

    public RecordingTransport(int maxPayload = 1472, BacnetMaxAdpu maxApdu = BacnetMaxAdpu.MAX_APDU1476)
    {
        Type = BacnetAddressTypes.IP;
        HeaderLength = 0;
        MaxBufferLength = maxPayload;
        MaxAdpuLength = maxApdu;
    }

    public override void Start()
    {
    }

    public override BacnetAddress GetBroadcastAddress()
    {
        return new BacnetAddress(BacnetAddressTypes.IP, "255.255.255.255:47808");
    }

    public override int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout)
    {
        var frame = new byte[dataLength];
        Array.Copy(buffer, offset, frame, 0, dataLength);
        lock (Sent)
        {
            Sent.Add((frame, address));
        }

        if (AutoSegmentAck)
            AckIfSegmented(frame, address);

        return dataLength;
    }

    /// <summary>Inject a frame as if it arrived from <paramref name="source"/>.</summary>
    public void Receive(byte[] frame, BacnetAddress source)
    {
        InvokeMessageRecieved(frame, 0, frame.Length, source);
    }

    private void AckIfSegmented(byte[] frame, BacnetAddress address)
    {
        var npduLen = NPDU.Decode(frame, 0, out var function, out _, out _, out _, out _, out _);
        if (npduLen <= 0 || function.HasFlag(BacnetNpduControls.NetworkLayerMessage))
            return;

        var pduType = (BacnetPduTypes)frame[npduLen];
        if ((pduType & BacnetPduTypes.PDU_TYPE_MASK) != BacnetPduTypes.PDU_TYPE_COMPLEX_ACK ||
            (pduType & BacnetPduTypes.SEGMENTED_MESSAGE) == 0)
            return;

        var invokeId = frame[npduLen + 1];
        var sequenceNumber = frame[npduLen + 2];

        // reply like the requester: ack this segment, actual window size 1
        var buffer = new EncodeBuffer(new byte[16], 0);
        NPDU.Encode(buffer, BacnetNpduControls.PriorityNormalMessage, null);
        APDU.EncodeSegmentAck(buffer, BacnetPduTypes.PDU_TYPE_SEGMENT_ACK, invokeId, sequenceNumber, 1);

        // fresh address instance: OnRecieve mutates RoutedSource on it
        Receive(buffer.ToArray(), new BacnetAddress(address.type, address.ToString()));
    }

    public override void Dispose()
    {
    }
}
