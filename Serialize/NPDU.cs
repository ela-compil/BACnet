namespace System.IO.BACnet.Serialize;

public class NPDU
{
    public const byte BACNET_PROTOCOL_VERSION = 1;

    public static BacnetNpduControls DecodeFunction(byte[] buffer, int offset)
    {
        if (buffer[offset + 0] != BACNET_PROTOCOL_VERSION) return 0;
        return (BacnetNpduControls)buffer[offset + 1];
    }

    public static int Decode(byte[] buffer, int offset, out BacnetNpduControls function, out BacnetAddress destination,
        out BacnetAddress source, out byte hopCount, out BacnetNetworkMessageTypes networkMsgType, out ushort vendorId)
    {
        var orgOffset = offset;

        offset++;
        function = (BacnetNpduControls)buffer[offset++];

        destination = null;
        if ((function & BacnetNpduControls.DestinationSpecified) == BacnetNpduControls.DestinationSpecified)
        {
            destination = new BacnetAddress(BacnetAddressTypes.None, (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0)), null);
            int adrLen = buffer[offset++];
            if (adrLen > 0)
            {
                destination.adr = new byte[adrLen];
                for (var i = 0; i < destination.adr.Length; i++)
                    destination.adr[i] = buffer[offset++];
            }
        }

        source = null;
        if ((function & BacnetNpduControls.SourceSpecified) == BacnetNpduControls.SourceSpecified)
        {
            source = new BacnetAddress(BacnetAddressTypes.None, (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0)), null);
            int adrLen = buffer[offset++];
            if (adrLen > 0)
            {
                source.adr = new byte[adrLen];
                for (var i = 0; i < source.adr.Length; i++)
                    source.adr[i] = buffer[offset++];
            }
        }

        hopCount = 0;
        if ((function & BacnetNpduControls.DestinationSpecified) == BacnetNpduControls.DestinationSpecified)
        {
            hopCount = buffer[offset++];
        }

        networkMsgType = BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK;
        vendorId = 0;
        if (function.HasFlag(BacnetNpduControls.NetworkLayerMessage))
        {
            networkMsgType = (BacnetNetworkMessageTypes)buffer[offset++];
            if ((byte)networkMsgType >= 0x80)
            {
                vendorId = (ushort)((buffer[offset++] << 8) | (buffer[offset++] << 0));
            }
            //DAL - this originally made no sense as the higher level code would just ignore network messages
            //                else if (networkMsgType == BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK)
            //                    offset += 2;  // Don't care about destination network adress
        }

        if (buffer[orgOffset + 0] != BACNET_PROTOCOL_VERSION)
            return -1;

        return offset - orgOffset;
    }

    public static void Encode(EncodeBuffer buffer, BacnetNpduControls function, BacnetAddress destination,
        BacnetAddress source, byte hopCount, BacnetNetworkMessageTypes networkMsgType, ushort vendorId)
    {
        Encode(buffer, function, destination, source, hopCount);

        if (function.HasFlag(BacnetNpduControls.NetworkLayerMessage)) // sure it is, otherwise the other Encode is used
        {
            buffer.buffer[buffer.offset++] = (byte)networkMsgType;
            if ((byte)networkMsgType >= 0x80) // who used this ??? sure nobody !
            {
                buffer.buffer[buffer.offset++] = (byte)((vendorId & 0xFF00) >> 8);
                buffer.buffer[buffer.offset++] = (byte)((vendorId & 0x00FF) >> 0);
            }
        }
    }

    public static void Encode(EncodeBuffer buffer, BacnetNpduControls function, BacnetAddress destination,
        BacnetAddress source = null, byte hopCount = 0xFF)
    {
        // Modif FC
        var hasDestination = destination != null && destination.net > 0; // && destination.net != 0xFFFF;
        var hasSource = source != null && source.net > 0 && source.net != 0xFFFF;

        buffer.buffer[buffer.offset++] = BACNET_PROTOCOL_VERSION;
        buffer.buffer[buffer.offset++] = (byte)(function | (hasDestination ? BacnetNpduControls.DestinationSpecified : 0) | (hasSource ? BacnetNpduControls.SourceSpecified : 0));

        if (hasDestination)
        {
            buffer.buffer[buffer.offset++] = (byte)((destination.net & 0xFF00) >> 8);
            buffer.buffer[buffer.offset++] = (byte)((destination.net & 0x00FF) >> 0);

            if (destination.net == 0xFFFF)                  //patch by F. Chaxel
                buffer.buffer[buffer.offset++] = 0;
            else
            {
                buffer.buffer[buffer.offset++] = (byte)destination.adr.Length;
                if (destination.adr.Length > 0)
                {
                    foreach (var t in destination.adr)
                        buffer.buffer[buffer.offset++] = t;
                }
            }
        }

        if (hasSource)
        {
            buffer.buffer[buffer.offset++] = (byte)((source.net & 0xFF00) >> 8);
            buffer.buffer[buffer.offset++] = (byte)((source.net & 0x00FF) >> 0);
            buffer.buffer[buffer.offset++] = (byte)source.adr.Length;
            if (source.adr.Length > 0)
            {
                foreach (var t in source.adr)
                    buffer.buffer[buffer.offset++] = t;
            }
        }

        if (hasDestination)
        {
            buffer.buffer[buffer.offset++] = hopCount;
        }
    }
}
