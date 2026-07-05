namespace System.IO.BACnet;

/// <summary>
/// BACnetDailySchedule ::= SEQUENCE { day-schedule [0] SEQUENCE OF BACnetTimeValue }
/// (ASHRAE 135-2016 Clause 21). One element of a Schedule object's Weekly_Schedule BACnetARRAY[7]
/// (elements 1-7 = Monday-Sunday); an empty list means no scheduled action on that day.
/// </summary>
public class BacnetDailySchedule : ASN1.IEncode, ASN1.IDecode
{
    public List<BacnetTimeValue> DaySchedule = new List<BacnetTimeValue>();

    public BacnetDailySchedule()
    {
    }

    public BacnetDailySchedule(IEnumerable<BacnetTimeValue> daySchedule)
    {
        DaySchedule = new List<BacnetTimeValue>(daySchedule);
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.encode_opening_tag(buffer, 0);
        foreach (var timeValue in DaySchedule)
            timeValue.Encode(buffer);
        ASN1.encode_closing_tag(buffer, 0);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        if (offset >= count || !ASN1.decode_is_opening_tag_number(buffer, offset, 0))
            return -1;

        var len = 1;
        DaySchedule = new List<BacnetTimeValue>();

        while (offset + len < count)
        {
            if (ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                return len + 1;

            var timeValue = new BacnetTimeValue();
            var timeValueLen = timeValue.Decode(buffer, offset + len, count);
            if (timeValueLen < 0)
                return -1;

            len += timeValueLen;
            DaySchedule.Add(timeValue);
        }

        return -1; // ran out of data before the closing tag
    }

    public override string ToString()
    {
        return DaySchedule.Count == 0 ? "no entries" : string.Join("; ", DaySchedule);
    }
}
