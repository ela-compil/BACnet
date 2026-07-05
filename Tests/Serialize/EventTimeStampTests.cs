using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// BACnetTimeStamp is a CHOICE of time [0], sequenceNumber [1] and dateTime [2]. The decoders
/// only handled dateTime: event notifications and alarm acknowledgements stamped with a time or
/// a sequence number (common on devices without clocks) failed to decode, so OnEventNotify never
/// fired for them.
/// </summary>
public class EventTimeStampTests
{
    private static BacnetEventNotificationData MakeEventData(BacnetGenericTime timeStamp) => new()
    {
        processIdentifier = 1,
        initiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 9876),
        eventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1),
        timeStamp = timeStamp,
        notificationClass = 1,
        priority = 100,
        eventType = BacnetEventTypes.EVENT_OUT_OF_RANGE,
        notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
        ackRequired = false,
        fromState = BacnetEventStates.EVENT_STATE_NORMAL,
        toState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
        outOfRange_exceedingValue = 88.5f,
        outOfRange_statusFlags = BacnetBitString.Parse("0000"),
        outOfRange_deadband = 0f,
        outOfRange_exceededLimit = 50f
    };

    private static BacnetEventNotificationData RoundTrip(BacnetGenericTime timeStamp)
    {
        var buffer = new EncodeBuffer();
        Services.EncodeEventNotifyUnconfirmed(buffer, MakeEventData(timeStamp));

        var len = Services.DecodeEventNotifyData(buffer.buffer, 0, buffer.offset, out var decoded);
        Assert.True(len >= 0, "DecodeEventNotifyData failed");
        return decoded;
    }

    [Fact]
    public void Event_with_sequence_number_timestamp_decodes()
    {
        var decoded = RoundTrip(new BacnetGenericTime(default, BacnetTimestampTags.TIME_STAMP_SEQUENCE, 1234));

        Assert.Equal(BacnetTimestampTags.TIME_STAMP_SEQUENCE, decoded.timeStamp.Tag);
        Assert.Equal(1234, decoded.timeStamp.Sequence);
        Assert.Equal(88.5f, decoded.outOfRange_exceedingValue);
    }

    [Fact]
    public void Event_with_time_timestamp_decodes()
    {
        var timeOfDay = new DateTime(1, 1, 1, 11, 22, 33, 440);
        var decoded = RoundTrip(new BacnetGenericTime(timeOfDay, BacnetTimestampTags.TIME_STAMP_TIME));

        Assert.Equal(BacnetTimestampTags.TIME_STAMP_TIME, decoded.timeStamp.Tag);
        Assert.Equal(timeOfDay.TimeOfDay, decoded.timeStamp.Time.TimeOfDay);
    }

    [Fact]
    public void Event_with_datetime_timestamp_decodes()
    {
        var stamp = new DateTime(2026, 7, 5, 11, 22, 33, 440);
        var decoded = RoundTrip(new BacnetGenericTime(stamp, BacnetTimestampTags.TIME_STAMP_DATETIME));

        Assert.Equal(BacnetTimestampTags.TIME_STAMP_DATETIME, decoded.timeStamp.Tag);
        Assert.Equal(stamp, decoded.timeStamp.Time);
    }

    [Fact]
    public void Alarm_acknowledge_with_mixed_timestamp_choices_decodes()
    {
        // the ack echoes the event's (sequence) stamp; the ack itself is dated
        var eventStamp = new BacnetGenericTime(default, BacnetTimestampTags.TIME_STAMP_SEQUENCE, 77);
        var ackStamp = new BacnetGenericTime(new DateTime(2026, 7, 5, 12, 0, 0), BacnetTimestampTags.TIME_STAMP_DATETIME);

        var buffer = new EncodeBuffer();
        Services.EncodeAlarmAcknowledge(buffer, 55,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), 3, "operator", eventStamp, ackStamp);

        var len = Services.DecodeAlarmAcknowledge(buffer.buffer, 0, buffer.offset, out var ackProcess,
            out var objectId, out var stateAcked, out var source, out var decodedEvent, out var decodedAck);

        Assert.True(len >= 0, "DecodeAlarmAcknowledge failed");
        Assert.Equal(55u, ackProcess);
        Assert.Equal(BacnetTimestampTags.TIME_STAMP_SEQUENCE, decodedEvent.Tag);
        Assert.Equal(77, decodedEvent.Sequence);
        Assert.Equal(BacnetTimestampTags.TIME_STAMP_DATETIME, decodedAck.Tag);
        Assert.Equal(ackStamp.Time, decodedAck.Time);
        Assert.Equal("operator", source);
    }

    [Fact]
    public void Timestamp_choice_wire_formats_decode()
    {
        // sequenceNumber [1]: context tag 1, length 1, value 1
        var len = ASN1.bacapp_decode_timestamp(new byte[] { 0x19, 0x01 }, 0, out var sequence);
        Assert.Equal(2, len);
        Assert.Equal(BacnetTimestampTags.TIME_STAMP_SEQUENCE, sequence.Tag);
        Assert.Equal(1, sequence.Sequence);

        // time [0]: context tag 0, length 4, 11:22:33.44
        len = ASN1.bacapp_decode_timestamp(new byte[] { 0x0C, 11, 22, 33, 44 }, 0, out var time);
        Assert.Equal(5, len);
        Assert.Equal(BacnetTimestampTags.TIME_STAMP_TIME, time.Tag);
        Assert.Equal(new TimeSpan(0, 11, 22, 33, 440), time.Time.TimeOfDay);

        // an unknown choice fails cleanly
        Assert.Equal(-1, ASN1.bacapp_decode_timestamp(new byte[] { 0x39, 0x01 }, 0, out _));
    }
}
