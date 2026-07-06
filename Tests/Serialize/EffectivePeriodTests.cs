using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Effective_Period (ASHRAE 135-2016 Clause 12.24.6) is one BACnetDateRange serialized as two
/// application-tagged dates. It decodes as the typed value so open (wildcarded) boundaries and
/// date patterns survive a read/write round-trip - previously it decoded into two bare DateTimes,
/// which cannot carry them.
/// </summary>
public class EffectivePeriodTests
{
    private static readonly BacnetAddress DummyAddress = new BacnetAddress(BacnetAddressTypes.None, 0, null);
    private static readonly BacnetObjectId ScheduleId = new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1);

    [Fact]
    public void Read_yields_one_typed_date_range_and_writes_back_byte_identical()
    {
        var period = new List<BacnetValue>
        {
            new BacnetValue(new BacnetDateRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)))
        };

        var ack = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(ack, ScheduleId,
            (uint)BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, ASN1.BACNET_ARRAY_ALL, period);
        var ackBytes = ack.ToArray();

        var len = Services.DecodeReadPropertyAcknowledge(DummyAddress, ackBytes, 0, ackBytes.Length,
            out _, out _, out var values);

        Assert.Equal(ackBytes.Length, len);
        var value = Assert.Single(values);
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATERANGE, value.Tag);
        var range = Assert.IsType<BacnetDateRange>(value.Value);
        Assert.Equal(new DateTime(2026, 1, 1), range.startDate.ToDateTime());
        Assert.Equal(new DateTime(2026, 12, 31), range.endDate.ToDateTime());

        var rewritten = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(rewritten, ScheduleId,
            (uint)BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, ASN1.BACNET_ARRAY_ALL, values);
        Assert.Equal(ackBytes, rewritten.ToArray());
    }

    [Fact]
    public void Open_boundary_survives_the_round_trip()
    {
        // "always in effect until end of 2026": the wildcarded start used to degrade to a bare
        // DateTime sentinel and could not be written back faithfully
        var period = new List<BacnetValue>
        {
            new BacnetValue(new BacnetDateRange(new BacnetDate(255, 255, 255), new BacnetDate(126, 12, 31)))
        };

        var ack = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(ack, ScheduleId,
            (uint)BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, ASN1.BACNET_ARRAY_ALL, period);
        var ackBytes = ack.ToArray();

        Services.DecodeReadPropertyAcknowledge(DummyAddress, ackBytes, 0, ackBytes.Length,
            out _, out _, out var values);

        var range = Assert.IsType<BacnetDateRange>(Assert.Single(values).Value);
        Assert.True(range.startDate.IsPeriodic); // still fully unspecified, not a clamped date
        Assert.True(range.IsAFittingDate(new DateTime(1990, 5, 5)));
        Assert.False(range.IsAFittingDate(new DateTime(2027, 1, 1)));

        var rewritten = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(rewritten, ScheduleId,
            (uint)BacnetPropertyIds.PROP_EFFECTIVE_PERIOD, ASN1.BACNET_ARRAY_ALL, values);
        Assert.Equal(ackBytes, rewritten.ToArray());
    }

    [Fact]
    public void Legacy_two_date_write_arrives_as_one_typed_range_at_the_server()
    {
        // clients may still write the property as two plain DATE values; the server-side decode
        // converges both shapes on the typed BacnetDateRange
        var write = new EncodeBuffer();
        Services.EncodeWriteProperty(write, ScheduleId, (uint)BacnetPropertyIds.PROP_EFFECTIVE_PERIOD,
            ASN1.BACNET_ARRAY_ALL, 0, new[]
            {
                new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, new DateTime(2026, 1, 1)),
                new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, new DateTime(2030, 12, 31))
            });
        var bytes = write.ToArray();

        var len = Services.DecodeWriteProperty(DummyAddress, bytes, 0, bytes.Length, out _, out var value);

        Assert.Equal(bytes.Length, len);
        var range = Assert.IsType<BacnetDateRange>(Assert.Single(value.value).Value);
        Assert.Equal(new DateTime(2026, 1, 1), range.startDate.ToDateTime());
        Assert.Equal(new DateTime(2030, 12, 31), range.endDate.ToDateTime());
    }
}
