namespace System.IO.BACnet;

/// <summary>
/// Third octet of BACnetWeekNDay (ASHRAE 135-2016 Clause 21). BACnet counts Monday=1 through
/// Sunday=7, unlike <see cref="DayOfWeek"/> which starts at Sunday=0.
/// </summary>
public enum BacnetDayOfWeekOptions : byte
{
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6,
    Sunday = 7,
    AnyDayOfWeek = 0xFF
}
