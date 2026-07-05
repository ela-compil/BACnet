namespace System.IO.BACnet;

/// <summary>
/// BACnetSpecialEvent ::= SEQUENCE { period CHOICE { calendar-entry [0] BACnetCalendarEntry,
/// calendar-reference [1] BACnetObjectIdentifier }, list-of-time-values [2] SEQUENCE OF
/// BACnetTimeValue, event-priority [3] Unsigned (1..16) } (ASHRAE 135-2016 Clause 21).
/// One element of a Schedule object's Exception_Schedule BACnetARRAY; priority 1 is the most
/// important, ties are broken by the lower array index (Clause 12.24.8).
/// </summary>
public class BacnetSpecialEvent : ASN1.IEncode, ASN1.IDecode
{
    /// <summary>The period CHOICE: either a calendar entry or a reference to a Calendar object.</summary>
    public BacnetCalendarEntry CalendarEntry;
    public BacnetObjectId? CalendarReference;

    public List<BacnetTimeValue> ListOfTimeValues = new List<BacnetTimeValue>();
    public uint EventPriority = 16;

    public BacnetSpecialEvent()
    {
    }

    public BacnetSpecialEvent(BacnetCalendarEntry period, IEnumerable<BacnetTimeValue> timeValues, uint eventPriority)
    {
        CalendarEntry = period;
        ListOfTimeValues = new List<BacnetTimeValue>(timeValues);
        EventPriority = eventPriority;
    }

    public BacnetSpecialEvent(BacnetObjectId calendarReference, IEnumerable<BacnetTimeValue> timeValues, uint eventPriority)
    {
        CalendarReference = calendarReference;
        ListOfTimeValues = new List<BacnetTimeValue>(timeValues);
        EventPriority = eventPriority;
    }

    public void Encode(EncodeBuffer buffer)
    {
        if (CalendarEntry != null)
        {
            ASN1.encode_opening_tag(buffer, 0);
            CalendarEntry.Encode(buffer);
            ASN1.encode_closing_tag(buffer, 0);
        }
        else if (CalendarReference != null)
        {
            ASN1.encode_context_object_id(buffer, 1, CalendarReference.Value.type, CalendarReference.Value.instance);
        }
        else
        {
            throw new InvalidOperationException("The special event has no period set");
        }

        ASN1.encode_opening_tag(buffer, 2);
        foreach (var timeValue in ListOfTimeValues)
            timeValue.Encode(buffer);
        ASN1.encode_closing_tag(buffer, 2);

        ASN1.encode_context_unsigned(buffer, 3, EventPriority);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        if (offset >= count)
            return -1;

        CalendarEntry = null;
        CalendarReference = null;

        int len;
        if (ASN1.decode_is_opening_tag_number(buffer, offset, 0))
        {
            len = 1;
            var entry = new BacnetCalendarEntry();
            var entryLen = entry.Decode(buffer, offset + len, count);
            if (entryLen < 0 || offset + len + entryLen >= count)
                return -1;
            len += entryLen;
            if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 0))
                return -1;
            len++;
            CalendarEntry = entry;
        }
        else
        {
            len = ASN1.decode_tag_number_and_value(buffer, offset, out byte tagNumber, out uint lenValueType);
            if (tagNumber != 1 || lenValueType != 4 || offset + len + 4 > count)
                return -1;
            len += ASN1.decode_object_id(buffer, offset + len, out BacnetObjectTypes objectType, out var instance);
            CalendarReference = new BacnetObjectId(objectType, instance);
        }

        if (offset + len >= count || !ASN1.decode_is_opening_tag_number(buffer, offset + len, 2))
            return -1;
        len++;

        ListOfTimeValues = new List<BacnetTimeValue>();
        while (offset + len < count && !ASN1.decode_is_closing_tag_number(buffer, offset + len, 2))
        {
            var timeValue = new BacnetTimeValue();
            var timeValueLen = timeValue.Decode(buffer, offset + len, count);
            if (timeValueLen < 0)
                return -1;
            len += timeValueLen;
            ListOfTimeValues.Add(timeValue);
        }
        if (offset + len >= count)
            return -1;
        len++; // closing tag [2]

        if (offset + len >= count)
            return -1;
        var priorityTagLen = ASN1.decode_tag_number_and_value(buffer, offset + len, out byte priorityTag, out uint priorityLen);
        if (priorityTag != 3 || offset + len + priorityTagLen + priorityLen > count)
            return -1;
        len += priorityTagLen;
        len += ASN1.decode_unsigned(buffer, offset + len, priorityLen, out var eventPriority);
        EventPriority = eventPriority;

        return len;
    }

    public override string ToString()
    {
        var period = CalendarEntry?.ToString() ?? CalendarReference?.ToString() ?? "no period";
        return $"{period}: {ListOfTimeValues.Count} time values, priority {EventPriority}";
    }
}
