namespace System.IO.BACnet;

/// <summary>
/// First octet of BACnetWeekNDay (ASHRAE 135-2016 Clause 21): a specific month, odd/even months
/// or any month.
/// </summary>
public enum BacnetMonthOptions : byte
{
    January = 1,
    February = 2,
    March = 3,
    April = 4,
    May = 5,
    June = 6,
    July = 7,
    August = 8,
    September = 9,
    October = 10,
    November = 11,
    December = 12,
    OddMonths = 13,
    EvenMonths = 14,
    AnyMonth = 0xFF
}
