using System.IO.BACnet.Storage;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Storage serialization of DATE/TIME/DATETIME must be culture-invariant and round-trip
/// (regression test for #159; TIME resolution is hundredths of a second per BACnet).
/// </summary>
public class PropertyDateTimeTests
{
    private static string Serialize(BacnetApplicationTags tag, DateTime value) =>
        Property.SerializeValue(new BacnetValue(tag, value), tag);

    private static DateTime Deserialize(BacnetApplicationTags tag, string value) =>
        (DateTime)Property.DeserializeValue(value, tag).Value;

    [Fact]
    public void Date_serializes_invariant_and_roundtrips()
    {
        var date = new DateTime(2024, 3, 5);

        var s = Serialize(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, date);

        Assert.Equal("2024/03/05", s);
        Assert.Equal(date, Deserialize(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, s));
    }

    [Fact]
    public void Time_serializes_with_hundredths_and_roundtrips()
    {
        var time = new DateTime(1, 1, 1, 13, 45, 30).AddMilliseconds(120); // 13:45:30.12

        var s = Serialize(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, time);

        Assert.Equal("13:45:30.12", s);
        var back = Deserialize(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, s);
        Assert.Equal(13, back.Hour);
        Assert.Equal(12, back.Millisecond / 10); // hundredths preserved
    }

    [Fact]
    public void DateTime_serializes_invariant_and_roundtrips()
    {
        var dt = new DateTime(2024, 12, 31, 23, 59, 58).AddMilliseconds(90); // .09

        var s = Serialize(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME, dt);

        Assert.Equal("2024/12/31-23:59:58.09", s);
        Assert.Equal(dt, Deserialize(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME, s));
    }
}
