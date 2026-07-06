namespace System.IO.BACnet;

/// <summary>
/// BACnetDateTime ::= SEQUENCE { date Date, time Time } (ASHRAE 135-2016 Clause 21), both
/// application-tagged on the wire and each octet individually wildcardable. Decoded DATETIME
/// values carry this struct whenever a merged <see cref="DateTime"/> cannot represent the
/// octets faithfully.
/// </summary>
public struct BacnetDateTime : ASN1.IEncode, ASN1.IDecode
{
    public BacnetDate Date;
    public BacnetTime Time;

    public BacnetDateTime(BacnetDate date, BacnetTime time)
    {
        Date = date;
        Time = time;
    }

    public BacnetDateTime(DateTime dateTime)
    {
        Date = new BacnetDate(dateTime);
        Time = new BacnetTime(dateTime.TimeOfDay);
    }

    public bool IsFullySpecified => !Date.IsPeriodic && Time.IsFullySpecified;

    /// <summary>Wildcarded components degrade like the merging decoders: date to the minimum
    /// sentinel, time components to zero.</summary>
    public DateTime ToDateTime()
    {
        return Date.ToDateTime().Add(Time.ToTimeSpan());
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.encode_application_date(buffer, Date);
        ASN1.encode_application_time(buffer, Time);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        var len = 0;

        if (offset + 10 > count || !ASN1.decode_is_application_tag(buffer, offset, BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE))
            return -1;
        len++;
        len += Date.Decode(buffer, offset + len, count);

        if (!ASN1.decode_is_application_tag(buffer, offset + len, BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME))
            return -1;
        len++;
        len += Time.Decode(buffer, offset + len, count);

        return len;
    }

    public override string ToString()
    {
        return $"{Date} {Time}";
    }
}
