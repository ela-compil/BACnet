namespace System.IO.BACnet;

public struct BacnetweekNDay : ASN1.IEncode, ASN1.IDecode
{
    public byte month;  /* 1 January, 13 Odd, 14 Even, 255 Any */
    public byte week;   /* Don't realy understand ??? 1 for day 1 to 7, 2 for ... what's the objective ?  boycott it*/
    public byte wday;   /* 1=Monday-7=Sunday, 255 any */

    public BacnetweekNDay(byte day, byte month, byte week = 255)
    {
        wday = day;
        this.month = month;
        this.week = week;
    }

    public void Encode(EncodeBuffer buffer)
    {
        buffer.Add(month);
        buffer.Add(week);
        buffer.Add(wday);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        month = buffer[offset++];
        week = buffer[offset++];
        wday = buffer[offset];
        return 3;
    }

    private static string GetDayName(int day)
    {
        if (day == 7)
            day = 0;

        return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[day];
    }

    public override string ToString()
    {
        string ret = wday != 255 ? GetDayName(wday) : "Every days";

        if (month != 255)
            ret += " on " + CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[month - 1];
        else
            ret += " on every month";

        return ret;
    }

    public bool IsAFittingDate(DateTime date)
    {
        if (date.Month != month && month != 255 && month != 13 && month != 14)
            return false;
        if (month == 13 && (date.Month & 1) != 1)
            return false;
        if (month == 14 && (date.Month & 1) == 1)
            return false;

        // What about week, too much stupid : boycott it !

        if (wday == 255)
            return true;
        if (wday == 7 && date.DayOfWeek == 0)  // Sunday 7 for Bacnet, 0 for .NET
            return true;
        if (wday == (int)date.DayOfWeek)
            return true;

        return false;
    }
}
