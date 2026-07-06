namespace System.IO.BACnet;

/// <summary>
/// BACnetTimeValue ::= SEQUENCE { time Time, value ABSTRACT-SYNTAX.&amp;Type } (ASHRAE 135-2016 Clause 21).
/// One (time, value) pair of a schedule; the time is always a specific time of day (wildcard octets are
/// not allowed in schedules - Clause 12.24.7) and the value is any primitive datatype, including NULL.
/// </summary>
public struct BacnetTimeValue : ASN1.IEncode, ASN1.IDecode
{
    public TimeSpan Time;
    public BacnetValue Value;

    public BacnetTimeValue(TimeSpan time, BacnetValue value)
    {
        Time = time;
        Value = value;
    }

    public BacnetTimeValue(int hour, int minute, int second, BacnetValue value)
        : this(new TimeSpan(hour, minute, second), value)
    {
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.encode_application_time(buffer, new DateTime(1, 1, 1).Add(Time));
        ASN1.bacapp_encode_application_data(buffer, Value);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        var len = ASN1.decode_tag_number_and_value(buffer, offset, out byte tagNumber, out uint lenValueType);
        if (tagNumber != (byte)BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME || lenValueType != 4)
            return -1;
        if (offset + len + 4 > count)
            return -1;

        len += ASN1.decode_bacnet_time(buffer, offset + len, out var time);
        // schedule times must be specific (12.24.7); tolerate a spec-illegal wildcard as midnight
        Time = time == ASN1.BACNET_TIME_WILDCARD ? TimeSpan.Zero : time.TimeOfDay;

        if (offset + len >= count)
            return -1;

        var tagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out BacnetApplicationTags valueTag, out lenValueType);
        len += tagLen;
        var valueLen = ASN1.bacapp_decode_data(buffer, offset + len, (int)count, valueTag, lenValueType, out var value);
        if (valueLen < 0)
            return -1;

        len += valueLen;
        Value = value;
        return len;
    }

    public override string ToString()
    {
        return $"{Time:hh\\:mm\\:ss} = {Value}";
    }
}
