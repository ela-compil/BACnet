namespace System.IO.BACnet;

/// <summary>
/// Second octet of BACnetWeekNDay (ASHRAE 135-2016 Clause 21): a 7-day slice of the month counted
/// either from the first day (1-5) or back from the last day (6-9), or any week.
/// </summary>
public enum BacnetWeekOfMonthOptions : byte
{
    Days1To7 = 1,
    Days8To14 = 2,
    Days15To21 = 3,
    Days22To28 = 4,
    Days29To31 = 5,
    Last7Days = 6,
    Prior7DaysToLast7Days = 7,
    Prior7DaysToLast14Days = 8,
    Prior7DaysToLast21Days = 9,
    AnyWeek = 0xFF
}
