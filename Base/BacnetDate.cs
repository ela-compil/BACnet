namespace System.IO.BACnet;

public struct BacnetDate : ASN1.IEncode, ASN1.IDecode
{
    public byte year;     /* 255 any */
    public byte month;      /* 1=Jan; 255 any, 13 Odd, 14 Even */
    public byte day;        /* 1..31; 32 last day of the month; 255 any */
    public byte wday;       /* 1=Monday-7=Sunday, 255 any */

    public BacnetDate(byte year, byte month, byte day, byte wday = 255)
    {
        this.year = year;
        this.month = month;
        this.day = day;
        this.wday = wday;
    }

    public void Encode(EncodeBuffer buffer)
    {
        buffer.Add(year);
        buffer.Add(month);
        buffer.Add(day);
        buffer.Add(wday);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        year = buffer[offset];
        month = buffer[offset + 1];
        day = buffer[offset + 2];
        wday = buffer[offset + 3];
        return 4;
    }

    public bool IsPeriodic => year == 255 || month > 12 || day == 255;

    public bool IsAFittingDate(DateTime date)
    {
        if (date.Year != year + 1900 && year != 255)
            return false;

        if (date.Month != month && month != 255 && month != 13 && month != 14)
            return false;
        if (month == 13 && (date.Month & 1) != 1)
            return false;
        if (month == 14 && (date.Month & 1) == 1)
            return false;

        if (date.Day != day && day != 255)
            return false;
        // day 32 todo

        if (wday == 255)
            return true;

        if (wday == 7 && date.DayOfWeek == 0)  // Sunday 7 for Bacnet, 0 for .NET
            return true;

        if (wday == (int)date.DayOfWeek)
            return true;

        return false;
    }

    public DateTime toDateTime() // Not every time possible, too much complex (any month, any year ...)
    {
        try
        {
            return IsPeriodic
                ? new DateTime(1, 1, 1)
                : new DateTime(year + 1900, month, day);
        }
        catch
        {
            return DateTime.Now; // or anything else why not !
        }
    }

    private static string GetDayName(int day)
    {
        if (day == 7)
            day = 0;

        return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[day];
    }

    public override string ToString()
    {
        string ret;

        if (wday != 255)
            ret = GetDayName(wday) + " ";
        else
            ret = "";

        if (day != 255)
            ret = ret + day + "/";
        else
            ret += "**/";

        switch (month)
        {
            case 13:
                ret += "odd/";
                break;
            case 14:
                ret += "even/";
                break;
            case 255:
                ret += "**/";
                break;
            default:
                ret = ret + month + "/";
                break;
        }


        if (year != 255)
            ret += year + 1900;
        else
            ret += "****";

        return ret;
    }
}
