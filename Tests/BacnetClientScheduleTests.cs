using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// End-to-end schedule property flows over an in-memory transport pair - the scenarios from
/// #26 (WriteProperty of Weekly_Schedule threw "I cannot encode this") and #131 (Exception_Schedule
/// read values could not be written back): a client must be able to write what it read, unchanged.
/// </summary>
public class BacnetClientScheduleTests
{
    private static readonly BacnetObjectId ScheduleId = new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1);
    private static readonly BacnetObjectId CalendarId = new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 1);

    private static (BacnetClient client, BacnetClient server) CreateClientServerPair()
    {
        var (transportA, transportB) = LoopbackTransport.CreatePair();
        var client = new BacnetClient(transportA, timeout: 500);
        var server = new BacnetClient(transportB, timeout: 500);
        client.Start();
        server.Start();
        return (client, server);
    }

    private static void ServeAndStore(BacnetClient server, Dictionary<BacnetPropertyIds, IList<BacnetValue>> store)
    {
        server.OnReadPropertyRequest += (sender, adr, invokeId, objectId, property, maxSegments) =>
        {
            var values = store[(BacnetPropertyIds)property.propertyIdentifier];
            sender.ReadPropertyResponse(adr, invokeId, sender.GetSegmentBuffer(maxSegments), objectId, property, values);
        };
        server.OnWritePropertyRequest += (sender, adr, invokeId, objectId, value, maxSegments) =>
        {
            store[(BacnetPropertyIds)value.property.propertyIdentifier] = value.value;
            sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId);
        };
    }

    private static byte[] EncodeAll(IEnumerable<BacnetValue> values)
    {
        var buffer = new EncodeBuffer();
        foreach (var value in values)
            ASN1.bacapp_encode_application_data(buffer, value);
        return buffer.ToArray();
    }

    [Fact]
    public void Weekly_schedule_written_by_a_client_is_read_back_unchanged()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            var store = new Dictionary<BacnetPropertyIds, IList<BacnetValue>>();
            ServeAndStore(server, store);

            var week = new List<BacnetValue>
            {
                new BacnetValue(new BacnetDailySchedule(new[]
                {
                    new BacnetTimeValue(new TimeSpan(0, 0, 0), new BacnetValue(18.0f)),  // midnight entry
                    new BacnetTimeValue(new TimeSpan(6, 30, 0), new BacnetValue(21.5f)),
                    new BacnetTimeValue(new TimeSpan(19, 0, 0), new BacnetValue(null))
                }))
            };
            for (var i = 1; i < 7; i++)
                week.Add(new BacnetValue(new BacnetDailySchedule()));

            client.WritePropertyRequest(client.Transport.GetBroadcastAddress(), ScheduleId,
                BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, week);

            client.ReadPropertyRequest(client.Transport.GetBroadcastAddress(), ScheduleId,
                BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, out var readBack);

            Assert.Equal(7, readBack.Count);
            Assert.Equal(EncodeAll(week), EncodeAll(readBack));
            var monday = Assert.IsType<BacnetDailySchedule>(readBack[0].Value);
            Assert.Equal(TimeSpan.Zero, monday.DaySchedule[0].Time);
        }
    }

    [Fact]
    public void Exception_schedule_read_from_a_device_writes_back_unchanged()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            var original = new List<BacnetValue>
            {
                new BacnetValue(new BacnetSpecialEvent(
                    new BacnetCalendarEntry(new BacnetDate(new DateTime(2026, 12, 24))),
                    new[] { new BacnetTimeValue(new TimeSpan(8, 0, 0), new BacnetValue(42.0f)) }, 5)),
                new BacnetValue(new BacnetSpecialEvent(
                    new BacnetCalendarEntry(new BacnetWeekNDay(BacnetDayOfWeekOptions.Friday)),
                    new[] { new BacnetTimeValue(new TimeSpan(16, 0, 0), new BacnetValue(null)) }, 8)),
                new BacnetValue(new BacnetSpecialEvent(
                    CalendarId, new BacnetTimeValue[0], 12))
            };

            var store = new Dictionary<BacnetPropertyIds, IList<BacnetValue>>
            {
                [BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE] = original
            };
            ServeAndStore(server, store);

            // the #131 repro: read, then write the returned list straight back
            client.ReadPropertyRequest(client.Transport.GetBroadcastAddress(), ScheduleId,
                BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE, out var readValues);
            client.WritePropertyRequest(client.Transport.GetBroadcastAddress(), ScheduleId,
                BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE, readValues);

            var written = store[BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE];
            Assert.Equal(3, written.Count);
            Assert.Equal(EncodeAll(original), EncodeAll(written));
            Assert.All(written, v => Assert.IsType<BacnetSpecialEvent>(v.Value));
        }
    }

    [Fact]
    public void Calendar_date_list_read_from_a_device_writes_back_unchanged()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            var original = new List<BacnetValue>
            {
                new BacnetValue(new BacnetCalendarEntry(new BacnetDate(255, 12, 25))),
                new BacnetValue(new BacnetCalendarEntry(new BacnetDateRange(new DateTime(2026, 7, 1), new DateTime(2026, 8, 31)))),
                new BacnetValue(new BacnetCalendarEntry(new BacnetWeekNDay(BacnetDayOfWeekOptions.Sunday, BacnetMonthOptions.OddMonths)))
            };

            var store = new Dictionary<BacnetPropertyIds, IList<BacnetValue>>
            {
                [BacnetPropertyIds.PROP_DATE_LIST] = original
            };
            ServeAndStore(server, store);

            client.ReadPropertyRequest(client.Transport.GetBroadcastAddress(), CalendarId,
                BacnetPropertyIds.PROP_DATE_LIST, out var readValues);
            client.WritePropertyRequest(client.Transport.GetBroadcastAddress(), CalendarId,
                BacnetPropertyIds.PROP_DATE_LIST, readValues);

            var written = store[BacnetPropertyIds.PROP_DATE_LIST];
            Assert.Equal(3, written.Count);
            Assert.Equal(EncodeAll(original), EncodeAll(written));
        }
    }
}
