using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Date and Time octets may individually be X'FF' (unspecified), and Date carries special values
/// (month 13/14 = odd/even, day 32/33/34 = last/odd/even - ASHRAE 135 §20.2.12/§20.2.13). Real
/// devices send these for "don't care" fields; the decoders previously threw from the DateTime
/// constructor, which upstream code swallows - e.g. an event notification stamped with a
/// partially-wildcarded time silently never reached OnEventNotify.
/// </summary>
public class DateTimeWildcardTests
{
    [Fact]
    public void Time_with_wildcard_seconds_decodes_with_specified_fields_kept()
    {
        var len = ASN1.decode_bacnet_time(new byte[] { 11, 22, 0xFF, 0xFF }, 0, out var time);

        Assert.Equal(4, len);
        Assert.Equal(new TimeSpan(11, 22, 0), time.TimeOfDay);
    }

    [Fact]
    public void Time_with_wildcard_hour_decodes_with_specified_fields_kept()
    {
        var len = ASN1.decode_bacnet_time(new byte[] { 0xFF, 22, 33, 44 }, 0, out var time);

        Assert.Equal(4, len);
        Assert.Equal(new TimeSpan(0, 0, 22, 33, 440), time.TimeOfDay);
    }

    [Fact]
    public void Fully_wildcarded_time_decodes_to_the_wildcard_marker()
    {
        ASN1.decode_bacnet_time(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, out var time);

        Assert.Equal(ASN1.BACNET_TIME_WILDCARD, time);
    }

    [Fact]
    public void Fully_wildcarded_time_round_trips_byte_identical()
    {
        ASN1.decode_bacnet_time(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, out var time);

        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_time(buffer, time);

        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, buffer.ToArray());
    }

    [Fact]
    public void Datetime_with_wildcard_time_component_combines_to_midnight()
    {
        // combined date+time values cannot carry an unspecified time: it degrades to 00:00
        var wire = new byte[]
        {
            0xA4, 126, 7, 6, 1,             // Date 2026-07-06 (Monday)
            0xB4, 0xFF, 0xFF, 0xFF, 0xFF    // Time unspecified
        };

        var len = ASN1.decode_bacnet_datetime(wire, 0, out var dateTime);

        Assert.Equal(wire.Length, len);
        Assert.Equal(new DateTime(2026, 7, 6, 0, 0, 0), dateTime);
    }

    [Theory]
    [InlineData(new byte[] { 126, 7, 0xFF, 0xFF })] // 2026-07, day unspecified
    [InlineData(new byte[] { 126, 13, 15, 0xFF })]  // odd months (13)
    [InlineData(new byte[] { 126, 7, 32, 0xFF })]   // last day of month (32)
    public void Unrepresentable_date_specials_degrade_to_minimum_instead_of_throwing(byte[] octets)
    {
        var len = ASN1.decode_date(octets, 0, out var date);

        Assert.Equal(4, len);
        Assert.Equal(new DateTime(1, 1, 1), date);
    }

    [Fact]
    public void Fully_specified_date_and_time_still_decode_exactly()
    {
        ASN1.decode_date(new byte[] { 126, 7, 5, 7 }, 0, out var date);
        ASN1.decode_bacnet_time(new byte[] { 11, 22, 33, 44 }, 0, out var time);

        Assert.Equal(new DateTime(2026, 7, 5), date);
        Assert.Equal(new TimeSpan(0, 11, 22, 33, 440), time.TimeOfDay);
    }

    [Fact]
    public void Midnight_encodes_as_zeros_not_as_the_time_wildcard()
    {
        // DateTime(1,1,1) is both the minimum sentinel and a plain 00:00:00.00 - and decoded
        // midnight IS that value, so re-encoding it must produce midnight, never FF FF FF FF
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_time(buffer, new DateTime(1, 1, 1));

        Assert.Equal(new byte[] { 0, 0, 0, 0 }, buffer.ToArray());
    }

    [Fact]
    public void Decoded_midnight_re_encodes_byte_identical()
    {
        ASN1.decode_bacnet_time(new byte[] { 0, 0, 0, 0 }, 0, out var midnight);

        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_time(buffer, midnight);

        Assert.Equal(new byte[] { 0, 0, 0, 0 }, buffer.ToArray());
    }

    [Fact]
    public void Time_wildcard_marker_encodes_as_all_unspecified_octets()
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_time(buffer, ASN1.BACNET_TIME_WILDCARD);

        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, buffer.ToArray());
    }

    [Fact]
    public void Date_wildcard_sentinel_still_encodes_as_all_unspecified_octets()
    {
        // no collision on the date side: no legal BACnet date maps to DateTime(1,1,1),
        // so the minimum sentinel remains the wildcard for dates
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_date(buffer, new DateTime(1, 1, 1));

        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, buffer.ToArray());
    }

    private static readonly BacnetAddress DummyAddress = new BacnetAddress(BacnetAddressTypes.None, 0, null);

    private static BacnetValue DecodeApplicationValue(byte[] wire)
    {
        var len = ASN1.bacapp_decode_application_data(DummyAddress, wire, 0, wire.Length,
            BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE, BacnetPropertyIds.MAX_BACNET_PROPERTY_ID, out var value);
        Assert.Equal(wire.Length, len);
        return value;
    }

    private static void AssertReencodesTo(byte[] wire, BacnetValue value)
    {
        var buffer = new EncodeBuffer();
        ASN1.bacapp_encode_application_data(buffer, value);
        Assert.Equal(wire, buffer.ToArray());
    }

    [Fact]
    public void Partially_wildcarded_time_value_survives_per_octet_and_round_trips()
    {
        var wire = new byte[] { 0xB4, 11, 22, 0xFF, 0xFF }; // 11:22:**.**

        var value = DecodeApplicationValue(wire);

        var time = Assert.IsType<BacnetTime>(value.Value);
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, value.Tag);
        Assert.Equal((11, 22, 255, 255), (time.hour, time.minute, time.second, time.hundredths));
        Assert.Equal(new TimeSpan(11, 22, 0), time.ToTimeSpan());
        AssertReencodesTo(wire, value);
    }

    [Fact]
    public void Fully_specified_time_value_stays_a_DateTime()
    {
        var value = DecodeApplicationValue(new byte[] { 0xB4, 11, 22, 33, 44 });

        Assert.IsType<DateTime>(value.Value);
        Assert.Equal(new TimeSpan(0, 11, 22, 33, 440), ((DateTime)value.Value).TimeOfDay);
    }

    [Theory]
    [InlineData(new byte[] { 0xA4, 0xFF, 13, 32, 0xFF })]  // last day of every odd month, any year
    [InlineData(new byte[] { 0xA4, 126, 7, 0xFF, 0xFF })]  // any day of July 2026
    [InlineData(new byte[] { 0xA4, 126, 2, 30, 0xFF })]    // invalid calendar date (Feb 30)
    public void Unrepresentable_date_value_survives_per_octet_and_round_trips(byte[] wire)
    {
        var value = DecodeApplicationValue(wire);

        Assert.IsType<BacnetDate>(value.Value);
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, value.Tag);
        AssertReencodesTo(wire, value);
    }

    [Fact]
    public void Specific_date_with_unspecified_weekday_stays_a_DateTime()
    {
        // the weekday of a specific date is redundant; it is normalized on re-encode (135 §20.2.12)
        var value = DecodeApplicationValue(new byte[] { 0xA4, 126, 7, 6, 0xFF });

        Assert.Equal(new DateTime(2026, 7, 6), value.Value);
        AssertReencodesTo(new byte[] { 0xA4, 126, 7, 6, 1 }, value); // 2026-07-06 is a Monday
    }

    [Theory]
    [InlineData(11, 22, 33, 44, true)]
    [InlineData(11, 22, 33, 45, false)]
    [InlineData(11, 22, 0, 0, false)]
    public void BacnetTime_fitting_checks_every_specified_octet(int h, int m, int s, int hs, bool expected)
    {
        var pattern = new BacnetTime(11, 22, 33, 44);

        Assert.Equal(expected, pattern.IsAFittingTime(new TimeSpan(0, h, m, s, hs * 10)));
    }

    [Fact]
    public void BacnetTime_wildcarded_octets_fit_anything_and_print_as_stars()
    {
        var pattern = new BacnetTime(11, 255, 255, 255);

        Assert.True(pattern.IsAFittingTime(new TimeSpan(0, 11, 59, 7, 130)));
        Assert.False(pattern.IsAFittingTime(new TimeSpan(0, 12, 0, 0, 0)));
        Assert.Equal("11:**:**.**", pattern.ToString());
    }
}
