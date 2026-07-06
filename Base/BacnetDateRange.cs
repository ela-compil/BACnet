namespace System.IO.BACnet;

public struct BacnetDateRange : ASN1.IEncode, ASN1.IDecode
{
    public BacnetDate startDate;
    public BacnetDate endDate;

    public BacnetDateRange(BacnetDate start, BacnetDate end)
    {
        startDate = start;
        endDate = end;
    }

    public BacnetDateRange(DateTime start, DateTime end)
        : this(new BacnetDate(start), new BacnetDate(end))
    {
    }

    public void Encode(EncodeBuffer buffer)
    {
        ASN1.encode_application_date(buffer, startDate);
        ASN1.encode_application_date(buffer, endDate);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        if (offset + 10 > count)
            return -1;

        var len = 1; // skip the Date application tag
        len += startDate.Decode(buffer, offset + len, count);
        len++;
        len += endDate.Decode(buffer, offset + len, count);
        return len;
    }

    public bool IsAFittingDate(DateTime date)
    {
        date = new DateTime(date.Year, date.Month, date.Day);

        // a wildcarded boundary leaves that side of the range open (135-2016 20.2.12);
        // in particular the all-wildcard default Effective_Period is always in effect
        if (!startDate.IsPeriodic && date < startDate.ToDateTime())
            return false;
        if (!endDate.IsPeriodic && date > endDate.ToDateTime())
            return false;

        return true;
    }

    public override string ToString()
    {
        string ret;

        if (startDate.day != 255)
            ret = "From " + startDate;
        else
            ret = "From **/**/**";

        if (endDate.day != 255)
            ret = ret + " to " + endDate;
        else
            ret += " to **/**/**";

        return ret;
    }
};
