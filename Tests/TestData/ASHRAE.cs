using System.Collections.Generic;
using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using System.IO.BACnet.Serialize;

namespace System.IO.BACnet.Tests.TestData
{
    public static class ASHRAE
    {
        public static (uint SubscriberProcessIdentifier, uint InitiatingDeviceIdentifier, BacnetObjectId
            MonitoredObjectIdentifier, uint TimeRemaining, BacnetPropertyValue[] Values)
            F_1_2()
        {
            var data = new[]
            {
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_PRESENT_VALUE),
                    value = new List<BacnetValue> {new BacnetValue(65.0f)}
                },
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_STATUS_FLAGS),
                    value = new List<BacnetValue> {new BacnetValue(BacnetBitString.Parse("0000"))}
                }
            };

            return (18, 4, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), 0, data);
        }

        public static (uint SubscriberProcessIdentifier, uint InitiatingDeviceIdentifier, BacnetObjectId
            MonitoredObjectIdentifier, uint TimeRemaining, BacnetPropertyValue[] Values)
            F_1_3() => F_1_2();

        public static StateTransition<OutOfRange> F_1_4()
        {
            return new StateTransition<OutOfRange>(new OutOfRange()
            {
                ExceedingValue = 80.1f,
                StatusFlags = BacnetBitString.Parse("1000"),
                Deadband = 1.0f,
                ExceededLimit = 80.0f
            })
            {
                ProcessIdentifier = 1,
                InitiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 4),
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                TimeStamp = new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
                NotificationClass = 4,
                Priority = 100,
                NotifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                AckRequired = true,
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
                ToState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT
            };
        }

        public static (BacnetLogRecord Record1, BacnetLogRecord Record2, BacnetObjectId ObjectId, BacnetPropertyIds
            PropertyId, BacnetBitString Flags, uint ItemCount, BacnetReadRangeRequestTypes RequestType, uint
            FirstSequence
            ) F_3_8()
        {
            var record1 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.0,
                new DateTime(1998, 3, 23, 19, 54, 27), 0);
            var record2 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.1,
                new DateTime(1998, 3, 23, 19, 56, 27), 0);

            return (record1, record2, new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 1),
                BacnetPropertyIds.PROP_LOG_BUFFER, BacnetBitString.Parse("110"), 2,
                BacnetReadRangeRequestTypes.RR_BY_SEQUENCE, 79201);
        }
    }
}
