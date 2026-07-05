namespace System.IO.BACnet;

/// <summary>
/// BACnetWeekNDay ::= OCTET STRING (SIZE(3)) - month, week-of-month, day-of-week - where each octet
/// may be X'FF' for "any" (ASHRAE 135-2016 Clause 21). All specified octets must match independently
/// for a date to fit (Clauses 12.9.6 / 12.24.8).
/// </summary>
public struct BacnetWeekNDay : ASN1.IEncode, ASN1.IDecode
{
    public BacnetMonthOptions Month;
    public BacnetWeekOfMonthOptions WeekOfMonth;
    public BacnetDayOfWeekOptions DayOfWeek;

    public BacnetWeekNDay(BacnetDayOfWeekOptions dayOfWeek,
        BacnetMonthOptions month = BacnetMonthOptions.AnyMonth,
        BacnetWeekOfMonthOptions weekOfMonth = BacnetWeekOfMonthOptions.AnyWeek)
    {
        Month = month;
        WeekOfMonth = weekOfMonth;
        DayOfWeek = dayOfWeek;
    }

    public BacnetWeekNDay(byte day, byte month, byte week = 255)
    {
        Month = (BacnetMonthOptions)month;
        WeekOfMonth = (BacnetWeekOfMonthOptions)week;
        DayOfWeek = (BacnetDayOfWeekOptions)day;
    }

    public void Encode(EncodeBuffer buffer)
    {
        buffer.Add((byte)Month);
        buffer.Add((byte)WeekOfMonth);
        buffer.Add((byte)DayOfWeek);
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        if (offset + 3 > count)
            return -1;

        Month = (BacnetMonthOptions)buffer[offset];
        WeekOfMonth = (BacnetWeekOfMonthOptions)buffer[offset + 1];
        DayOfWeek = (BacnetDayOfWeekOptions)buffer[offset + 2];
        return 3;
    }

    public bool IsAFittingDate(DateTime date)
    {
        return MonthMatches(date) && WeekOfMonthMatches(date) && DayOfWeekMatches(date);
    }

    private bool MonthMatches(DateTime date)
    {
        switch (Month)
        {
            case BacnetMonthOptions.AnyMonth:
                return true;
            case BacnetMonthOptions.OddMonths:
                return date.Month % 2 == 1;
            case BacnetMonthOptions.EvenMonths:
                return date.Month % 2 == 0;
            default:
                return date.Month == (byte)Month;
        }
    }

    private bool WeekOfMonthMatches(DateTime date)
    {
        if (WeekOfMonth == BacnetWeekOfMonthOptions.AnyWeek)
            return true;

        var week = (byte)WeekOfMonth;
        if (week >= 1 && week <= 5)
            return date.Day > (week - 1) * 7 && date.Day <= week * 7;

        // 6..9 count 7-day slices back from the end of the month
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        switch (WeekOfMonth)
        {
            case BacnetWeekOfMonthOptions.Last7Days:
                return date.Day > daysInMonth - 7;
            case BacnetWeekOfMonthOptions.Prior7DaysToLast7Days:
                return date.Day > daysInMonth - 14 && date.Day <= daysInMonth - 7;
            case BacnetWeekOfMonthOptions.Prior7DaysToLast14Days:
                return date.Day > daysInMonth - 21 && date.Day <= daysInMonth - 14;
            case BacnetWeekOfMonthOptions.Prior7DaysToLast21Days:
                return date.Day > daysInMonth - 28 && date.Day <= daysInMonth - 21;
            default:
                return false;
        }
    }

    private bool DayOfWeekMatches(DateTime date)
    {
        if (DayOfWeek == BacnetDayOfWeekOptions.AnyDayOfWeek)
            return true;

        var bacnetDayOfWeek = date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;
        return bacnetDayOfWeek == (byte)DayOfWeek;
    }

    public override string ToString()
    {
        return $"{Month}/{WeekOfMonth}/{DayOfWeek}";
    }
}
