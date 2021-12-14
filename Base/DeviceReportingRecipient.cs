namespace System.IO.BACnet;

public struct DeviceReportingRecipient : ASN1.IEncode
{
    public BacnetBitString WeekofDay;
    public DateTime toTime, fromTime;

    public BacnetObjectId Id;
    public BacnetAddress adr;

    public uint processIdentifier;
    public bool Ack_Required;
    public BacnetBitString evenType;

    public DeviceReportingRecipient(BacnetValue v0, BacnetValue v1, BacnetValue v2, BacnetValue v3, BacnetValue v4, BacnetValue v5, BacnetValue v6)
    {
        Id = new BacnetObjectId();
        adr = null;

        WeekofDay = (BacnetBitString)v0.Value;
        fromTime = (DateTime)v1.Value;
        toTime = (DateTime)v2.Value;
        if (v3.Value is BacnetObjectId id)
        {
            Id = id;
        }
        else
        {
            var netdescr = (BacnetValue[])v3.Value;
            var s = (ushort)(uint)netdescr[0].Value;
            var b = (byte[])netdescr[1].Value;
            adr = new BacnetAddress(BacnetAddressTypes.IP, s, b);
        }
        processIdentifier = (uint)v4.Value;
        Ack_Required = (bool)v5.Value;
        evenType = (BacnetBitString)v6.Value;
    }

    public DeviceReportingRecipient(BacnetBitString weekofDay, DateTime fromTime, DateTime toTime, BacnetObjectId id, uint processIdentifier, bool ackRequired, BacnetBitString evenType)
    {
        adr = null;

        WeekofDay = weekofDay;
        this.toTime = toTime;
        this.fromTime = fromTime;
        Id = id;
        this.processIdentifier = processIdentifier;
        Ack_Required = ackRequired;
        this.evenType = evenType;
    }

    public DeviceReportingRecipient(BacnetBitString weekofDay, DateTime fromTime, DateTime toTime, BacnetAddress adr, uint processIdentifier, bool ackRequired, BacnetBitString evenType)
    {
        Id = new BacnetObjectId();
        WeekofDay = weekofDay;
        this.toTime = toTime;
        this.fromTime = fromTime;
        this.adr = adr;
        this.processIdentifier = processIdentifier;
        Ack_Required = ackRequired;
        this.evenType = evenType;
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.bacapp_encode_application_data(buffer, new BacnetValue(WeekofDay));
        ASN1.bacapp_encode_application_data(buffer, new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, fromTime));
        ASN1.bacapp_encode_application_data(buffer, new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, toTime));

        if (adr != null)
        {
            adr.Encode(buffer);
        }
        else
        {
            // BacnetObjectId is context specific encoded
            ASN1.encode_context_object_id(buffer, 0, Id.type, Id.instance);
        }

        ASN1.bacapp_encode_application_data(buffer, new BacnetValue(processIdentifier));
        ASN1.bacapp_encode_application_data(buffer, new BacnetValue(Ack_Required));
        ASN1.bacapp_encode_application_data(buffer, new BacnetValue(evenType));
    }
}
