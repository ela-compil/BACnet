namespace System.IO.BACnet;

/// <summary>
/// BACnetCalendarEntry ::= CHOICE { date [0] Date, date-range [1] BACnetDateRange,
/// weekNDay [2] BACnetWeekNDay } (ASHRAE 135-2016 Clause 21). Exactly one member is set.
/// Elements of a Calendar object's Date_List and of a BACnetSpecialEvent period.
/// </summary>
public class BacnetCalendarEntry : ASN1.IEncode, ASN1.IDecode
{
    public BacnetDate? Date;
    public BacnetDateRange? DateRange;
    public BacnetWeekNDay? WeekNDay;

    public BacnetCalendarEntry()
    {
    }

    public BacnetCalendarEntry(BacnetDate date)
    {
        Date = date;
    }

    public BacnetCalendarEntry(BacnetDateRange dateRange)
    {
        DateRange = dateRange;
    }

    public BacnetCalendarEntry(BacnetWeekNDay weekNDay)
    {
        WeekNDay = weekNDay;
    }

    public void Encode(EncodeBuffer buffer)
    {
        if (Date != null)
        {
            ASN1.encode_tag(buffer, 0, true, 4);
            Date.Value.Encode(buffer);
        }
        else if (DateRange != null)
        {
            ASN1.encode_opening_tag(buffer, 1);
            DateRange.Value.Encode(buffer);
            ASN1.encode_closing_tag(buffer, 1);
        }
        else if (WeekNDay != null)
        {
            ASN1.encode_tag(buffer, 2, true, 3);
            WeekNDay.Value.Encode(buffer);
        }
        else
        {
            throw new InvalidOperationException("The calendar entry has no choice member set");
        }
    }

    /// <summary>
    /// Consumes exactly the CHOICE production: the surrounding construct (e.g. the special event's
    /// period [0] wrapper) owns any enclosing opening/closing tags.
    /// </summary>
    public int Decode(byte[] buffer, int offset, uint count)
    {
        if (offset >= count || ASN1.IS_EXTENDED_TAG_NUMBER(buffer[offset]))
            return -1;

        Date = null;
        DateRange = null;
        WeekNDay = null;

        var len = ASN1.decode_tag_number(buffer, offset, out var tagNumber);
        switch (tagNumber)
        {
            case 0: // primitive context tag, four date octets
                if (offset + len + 4 > count) return -1;
                var date = new BacnetDate();
                len += date.Decode(buffer, offset + len, count);
                Date = date;
                return len;

            case 1: // constructed: two application-tagged dates between opening/closing tags
                var dateRange = new BacnetDateRange();
                var rangeLen = dateRange.Decode(buffer, offset + len, count);
                if (rangeLen < 0 || offset + len + rangeLen >= count) return -1;
                len += rangeLen;
                if (!ASN1.decode_is_closing_tag_number(buffer, offset + len, 1)) return -1;
                DateRange = dateRange;
                return len + 1;

            case 2: // primitive context tag, three weekNDay octets
                var weekNDay = new BacnetWeekNDay();
                var weekLen = weekNDay.Decode(buffer, offset + len, count);
                if (weekLen < 0) return -1;
                WeekNDay = weekNDay;
                return len + weekLen;

            default:
                return -1;
        }
    }

    public bool IsAFittingDate(DateTime date)
    {
        if (Date != null)
            return Date.Value.IsAFittingDate(date);
        if (DateRange != null)
            return DateRange.Value.IsAFittingDate(date);
        if (WeekNDay != null)
            return WeekNDay.Value.IsAFittingDate(date);
        return false;
    }

    public override string ToString()
    {
        return Date?.ToString() ?? DateRange?.ToString() ?? WeekNDay?.ToString() ?? "empty";
    }
}
