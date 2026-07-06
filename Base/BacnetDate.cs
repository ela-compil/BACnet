namespace System.IO.BACnet;

public struct BacnetDate : ASN1.IEncode, ASN1.IDecode
{
    public byte year;     /* 255 any */
    public byte month;      /* 1=Jan; 255 any, 13 Odd, 14 Even */
    public byte day;        /* 1..31; 32 last day of the month; 33 odd, 34 even days; 255 any */
    public byte wday;       /* 1=Monday-7=Sunday, 255 any */

    public BacnetDate(byte year, byte month, byte day, byte wday = 255)
    {
        this.year = year;
        this.month = month;
        this.day = day;
        this.wday = wday;
    }

    public BacnetDate(DateTime date)
    {
        year = (byte)(date.Year - 1900);
        month = (byte)date.Month;
        day = (byte)date.Day;
        wday = (byte)(date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek);
    }

    /// <summary>The fully-unspecified date, matching every day.</summary>
    public static readonly BacnetDate Any = new BacnetDate(255, 255, 255);

    /// <summary>Like the constructor, but maps the DateTime(1,1,1) 'any date' sentinel to <see cref="Any"/>.</summary>
    public static BacnetDate FromDateTime(DateTime date)
    {
        return date == new DateTime(1, 1, 1) ? Any : new BacnetDate(date);
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
        if (offset + 4 > count)
            return -1;

        year = buffer[offset];
        month = buffer[offset + 1];
        day = buffer[offset + 2];
        wday = buffer[offset + 3];
        return 4;
    }

    public bool IsPeriodic => year == 255 || month > 12 || day > 32;

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

        if (day == 32)
        {
            if (date.Day != DateTime.DaysInMonth(date.Year, date.Month))
                return false;
        }
        else if (day == 33)
        {
            if ((date.Day & 1) != 1)
                return false;
        }
        else if (day == 34)
        {
            if ((date.Day & 1) == 1)
                return false;
        }
        else if (date.Day != day && day != 255)
        {
            return false;
        }

        if (wday == 255)
            return true;

        if (wday == 7 && date.DayOfWeek == 0)  // Sunday 7 for Bacnet, 0 for .NET
            return true;

        if (wday == (int)date.DayOfWeek)
            return true;

        return false;
    }

    public DateTime ToDateTime() // not always possible (any month, any year, ...): those yield the wildcard sentinel
    {
        try
        {
            if (IsPeriodic)
                return new DateTime(1, 1, 1);

            return day == 32 // last day of the month
                ? new DateTime(year + 1900, month, DateTime.DaysInMonth(year + 1900, month))
                : new DateTime(year + 1900, month, day);
        }
        catch
        {
            return new DateTime(1, 1, 1);
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

        switch (day)
        {
            case 32:
                ret += "last/";
                break;
            case 33:
                ret += "odd/";
                break;
            case 34:
                ret += "even/";
                break;
            case 255:
                ret += "**/";
                break;
            default:
                ret = ret + day + "/";
                break;
        }

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
