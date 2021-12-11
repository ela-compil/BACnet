namespace System.IO.BACnet.Serialize;

public class APDU
{
    public static BacnetPduTypes GetDecodedType(byte[] buffer, int offset)
    {
        return (BacnetPduTypes)buffer[offset];
    }

    public static void SetDecodedType(byte[] buffer, int offset, BacnetPduTypes type)
    {
        buffer[offset] = (byte)type;
    }

    public static int GetDecodedInvokeId(byte[] buffer, int offset)
    {
        var type = GetDecodedType(buffer, offset);
        switch (type & BacnetPduTypes.PDU_TYPE_MASK)
        {
            case BacnetPduTypes.PDU_TYPE_SIMPLE_ACK:
            case BacnetPduTypes.PDU_TYPE_COMPLEX_ACK:
            case BacnetPduTypes.PDU_TYPE_ERROR:
            case BacnetPduTypes.PDU_TYPE_REJECT:
            case BacnetPduTypes.PDU_TYPE_ABORT:
                return buffer[offset + 1];
            case BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                return buffer[offset + 2];
            default:
                return -1;
        }
    }

    public static void EncodeConfirmedServiceRequest(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments maxSegments,
        BacnetMaxAdpu maxAdpu, byte invokeId, byte sequenceNumber = 0, byte proposedWindowSize = 0)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = (byte)((byte)maxSegments | (byte)maxAdpu);
        buffer.buffer[buffer.offset++] = invokeId;

        if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            buffer.buffer[buffer.offset++] = sequenceNumber;
            buffer.buffer[buffer.offset++] = proposedWindowSize;
        }
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeConfirmedServiceRequest(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service,
        out BacnetMaxSegments maxSegments, out BacnetMaxAdpu maxAdpu, out byte invokeId, out byte sequenceNumber, out byte proposedWindowNumber)
    {
        var orgOffset = offset;

        type = (BacnetPduTypes)buffer[offset++];
        maxSegments = (BacnetMaxSegments)(buffer[offset] & 0xF0);
        maxAdpu = (BacnetMaxAdpu)(buffer[offset++] & 0x0F);
        invokeId = buffer[offset++];

        sequenceNumber = 0;
        proposedWindowNumber = 0;
        if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            sequenceNumber = buffer[offset++];
            proposedWindowNumber = buffer[offset++];
        }
        service = (BacnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeUnconfirmedServiceRequest(EncodeBuffer buffer, BacnetPduTypes type, BacnetUnconfirmedServices service)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeUnconfirmedServiceRequest(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetUnconfirmedServices service)
    {
        var orgOffset = offset;

        type = (BacnetPduTypes)buffer[offset++];
        service = (BacnetUnconfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeSimpleAck(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeSimpleAck(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service, out byte invokeId)
    {
        var orgOffset = offset;

        type = (BacnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];
        service = (BacnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static int EncodeComplexAck(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId, byte sequenceNumber = 0, byte proposedWindowNumber = 0)
    {
        var len = 3;
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            buffer.buffer[buffer.offset++] = sequenceNumber;
            buffer.buffer[buffer.offset++] = proposedWindowNumber;
            len += 2;
        }
        buffer.buffer[buffer.offset++] = (byte)service;
        return len;
    }

    public static int DecodeComplexAck(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service, out byte invokeId,
        out byte sequenceNumber, out byte proposedWindowNumber)
    {
        var orgOffset = offset;

        type = (BacnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];

        sequenceNumber = 0;
        proposedWindowNumber = 0;
        if ((type & BacnetPduTypes.SEGMENTED_MESSAGE) > 0)
        {
            sequenceNumber = buffer[offset++];
            proposedWindowNumber = buffer[offset++];
        }
        service = (BacnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeSegmentAck(EncodeBuffer buffer, BacnetPduTypes type, byte originalInvokeId, byte sequenceNumber, byte actualWindowSize)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = originalInvokeId;
        buffer.buffer[buffer.offset++] = sequenceNumber;
        buffer.buffer[buffer.offset++] = actualWindowSize;
    }

    public static int DecodeSegmentAck(byte[] buffer, int offset, out BacnetPduTypes type, out byte originalInvokeId, out byte sequenceNumber, out byte actualWindowSize)
    {
        var orgOffset = offset;

        type = (BacnetPduTypes)buffer[offset++];
        originalInvokeId = buffer[offset++];
        sequenceNumber = buffer[offset++];
        actualWindowSize = buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeError(EncodeBuffer buffer, BacnetPduTypes type, BacnetConfirmedServices service, byte invokeId)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        buffer.buffer[buffer.offset++] = (byte)service;
    }

    public static int DecodeError(byte[] buffer, int offset, out BacnetPduTypes type, out BacnetConfirmedServices service, out byte invokeId)
    {
        var orgOffset = offset;

        type = (BacnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];
        service = (BacnetConfirmedServices)buffer[offset++];

        return offset - orgOffset;
    }

    public static void EncodeAbort(EncodeBuffer buffer, BacnetPduTypes type, byte invokeId, BacnetAbortReason reason)
    {
        EncodeAbortOrReject(buffer, type, invokeId, reason);
    }

    public static void EncodeReject(EncodeBuffer buffer, BacnetPduTypes type, byte invokeId, BacnetRejectReason reason)
    {
        EncodeAbortOrReject(buffer, type, invokeId, reason);
    }

    private static void EncodeAbortOrReject(EncodeBuffer buffer, BacnetPduTypes type, byte invokeId, dynamic reason)
    {
        buffer.buffer[buffer.offset++] = (byte)type;
        buffer.buffer[buffer.offset++] = invokeId;
        buffer.buffer[buffer.offset++] = (byte)reason;
    }

    public static int DecodeAbort(byte[] buffer, int offset, out BacnetPduTypes type,
        out byte invokeId, out BacnetAbortReason reason)
    {
        return DecodeAbortOrReject(buffer, offset, out type, out invokeId, out reason);
    }

    public static int DecodeReject(byte[] buffer, int offset, out BacnetPduTypes type,
        out byte invokeId, out BacnetRejectReason reason)
    {
        return DecodeAbortOrReject(buffer, offset, out type, out invokeId, out reason);
    }

    private static int DecodeAbortOrReject<TReason>(byte[] buffer, int offset,
        out BacnetPduTypes type, out byte invokeId, out TReason reason)
    {
        var orgOffset = offset;

        type = (BacnetPduTypes)buffer[offset++];
        invokeId = buffer[offset++];
        reason = (TReason)(dynamic)buffer[offset++];

        return offset - orgOffset;
    }
}
