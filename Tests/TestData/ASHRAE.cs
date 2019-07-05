using System.Collections.Generic;
using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using System.IO.BACnet.Serialize;
using System.Linq;
using System.Text;
using static System.IO.BACnet.Tests.Helper;

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

        public static BacnetAlarmSummaryData[]
            F_1_6()
        {
            return new[]
            {
                new BacnetAlarmSummaryData(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                    BacnetEventStates.EVENT_STATE_HIGH_LIMIT, BacnetBitString.Parse("011")),
                new BacnetAlarmSummaryData(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
                    BacnetEventStates.EVENT_STATE_LOW_LIMIT, BacnetBitString.Parse("111"))
            };
        }

        public static (BacnetLogRecord Record1, BacnetLogRecord Record2, BacnetObjectId ObjectId, BacnetPropertyIds
            PropertyId, BacnetBitString Flags, uint ItemCount, BacnetReadRangeRequestTypes RequestType, uint
            FirstSequence
            ) F_3_8_Ack()
        {
            var record1 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.0,
                new DateTime(1998, 3, 23, 19, 54, 27), 0);
            var record2 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.1,
                new DateTime(1998, 3, 23, 19, 56, 27), 0);

            return (record1, record2, new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 1),
                BacnetPropertyIds.PROP_LOG_BUFFER, BacnetBitString.Parse("110"), 2,
                BacnetReadRangeRequestTypes.RR_BY_SEQUENCE, 79201);
        }

        public static (BacnetGetEventInformationData[] Data, bool MoreEvents) F_1_8()
        {
            return (new[]
            {
                new BacnetGetEventInformationData()
                {
                    objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                    eventState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
                    acknowledgedTransitions = BacnetBitString.Parse("011"),
                    eventTimeStamps = new[]
                    {
                        new BacnetGenericTime(new DateTime(1, 1, 1, 15, 35, 00).AddMilliseconds(200),
                            BacnetTimestampTags.TIME_STAMP_TIME),
                        new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_TIME),
                        new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_TIME),
                    },
                    notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                    eventEnable = BacnetBitString.Parse("111"),
                    eventPriorities = new uint[] {15, 15, 20}
                },
                new BacnetGetEventInformationData()
                {
                    objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
                    eventState = BacnetEventStates.EVENT_STATE_NORMAL,
                    acknowledgedTransitions = BacnetBitString.Parse("110"),
                    eventTimeStamps = new[]
                    {
                        new BacnetGenericTime(new DateTime(1, 1, 1, 15, 40, 00), BacnetTimestampTags.TIME_STAMP_TIME),
                        new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_TIME),
                        new BacnetGenericTime(new DateTime(1, 1, 1, 15, 45, 30).AddMilliseconds(300),
                            BacnetTimestampTags.TIME_STAMP_TIME),
                    },
                    notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                    eventEnable = BacnetBitString.Parse("111"),
                    eventPriorities = new uint[] {15, 15, 20}
                }
            }, false);
        }

        public static (uint SubscriberProcessIdentifier, BacnetObjectId MonitoredObjectIdentifier, bool
            CancellationRequest, bool IssueConfirmedNotifications, uint Lifetime)
            F_1_10()
        {
            return (18, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10),
                false, true, 0);
        }

        public static (uint SubscriberProcessIdentifier, BacnetObjectId MonitoredObjectIdentifier, bool
            CancellationRequest, bool IssueConfirmedNotifications, uint Lifetime, BacnetPropertyReference
            MonitoredProperty, bool CovIncrementPresent, float CovIncrement)
            F_1_11()
        {
            return (18, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), false, true, 60,
                new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_PRESENT_VALUE), true, 1.0f);
        }

        public static (bool IsStream, bool EndOfFile, int Position, uint BlockCount, byte[][] Blocks, int[] Counts)
            F_2_1()
        {
            var data = new[]
            {
                Encoding.ASCII.GetBytes("Chiller01 On-Time=4.3 Hours")
            };

            return (true, false, 0, 1, data, data.Select(arr => arr.Length).ToArray());
        }

        public static (BacnetObjectTypes ObjectType, ICollection<BacnetPropertyValue> ValueList)
            F_3_3()
        {
            var data = new List<BacnetPropertyValue>
            {
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference(BacnetPropertyIds.PROP_OBJECT_NAME),
                    value = new List<BacnetValue> {new BacnetValue("Trend 1")}
                },
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference(BacnetPropertyIds.PROP_FILE_ACCESS_METHOD),
                    value = new List<BacnetValue> {new BacnetValue(BacnetFileAccessMethod.RECORD_ACCESS)}
                }
            };

            return (BacnetObjectTypes.OBJECT_FILE, data);
        }

        public static BacnetObjectId F_3_4()
            => new BacnetObjectId(BacnetObjectTypes.OBJECT_GROUP, 6);

        public static (BacnetObjectId ObjectId, BacnetPropertyIds PropertyId, uint ArrayIndex)
            F_3_5()
            => (new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 5), BacnetPropertyIds.PROP_PRESENT_VALUE,
                ASN1.BACNET_ARRAY_ALL);

        public static (BacnetObjectId ObjectId, BacnetPropertyIds PropertyId, IEnumerable<BacnetValue> ValueList, uint
            ArrayIndex)
            F_3_5_Ack()
            => (new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 5), BacnetPropertyIds.PROP_PRESENT_VALUE,
                new List<BacnetValue>
                {
                    new BacnetValue(72.3f)
                }, ASN1.BACNET_ARRAY_ALL);

        public static (BacnetObjectId ObjectId, IList<BacnetPropertyReference> Properties)
            F_3_7()
            => (new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 16), new List<BacnetPropertyReference>
            {
                new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                new BacnetPropertyReference(BacnetPropertyIds.PROP_RELIABILITY)
            });

        public static IList<BacnetReadAccessResult> F_3_7_Ack()
            => new List<BacnetReadAccessResult>
            {
                new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 16),
                    new List<BacnetPropertyValue>
                    {
                        new BacnetPropertyValue
                        {
                            property = new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                            value = new List<BacnetValue> {new BacnetValue(72.3f)},
                        },
                        new BacnetPropertyValue
                        {
                            property = new BacnetPropertyReference(BacnetPropertyIds.PROP_RELIABILITY),
                            value = new List<BacnetValue>
                            {
                                new BacnetValue(BacnetReliability.RELIABILITY_NO_FAULT_DETECTED)
                            },
                        }
                    })
            };

        public static (BacnetObjectId ObjectId, BacnetPropertyIds PropertyId, BacnetReadRangeRequestTypes RequestType,
            uint Position, DateTime Time, int Count, uint ArrayIndex)
            F_3_8()
            => (new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 1),
                BacnetPropertyIds.PROP_LOG_BUFFER, BacnetReadRangeRequestTypes.RR_BY_TIME, 0,
                new DateTime(1998, 3, 23, 19, 52, 34), 4, ASN1.BACNET_ARRAY_ALL);

        public static (BacnetObjectId ObjectId, BacnetPropertyIds PropertyId, IEnumerable<BacnetValue> ValueList, uint
            ArrayIndex, uint Priority)
            F_3_9()
            => (new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), BacnetPropertyIds.PROP_PRESENT_VALUE,
                new List<BacnetValue> {new BacnetValue(180f)}, ASN1.BACNET_ARRAY_ALL, 0);

        public static BacnetWriteAccessSpecification[] F_3_10()
            => new[]
            {
                new BacnetWriteAccessSpecification(
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 5),
                    A(new BacnetWriteAccessSpecification.Property(BacnetPropertyIds.PROP_PRESENT_VALUE,
                        new BacnetValue(67f)))),

                new BacnetWriteAccessSpecification(
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 6),
                    A(new BacnetWriteAccessSpecification.Property(BacnetPropertyIds.PROP_PRESENT_VALUE,
                        new BacnetValue(67f)))),

                new BacnetWriteAccessSpecification(
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 7),
                    A(new BacnetWriteAccessSpecification.Property(BacnetPropertyIds.PROP_PRESENT_VALUE,
                        new BacnetValue(72f)))),
            };

        public static (uint TimeDuration, EnableDisable EnableDisable, string Password)
            F_4_1()
            => (5, EnableDisable.DISABLE, "#egbdf!");

        public static (BacnetReinitializedStates State, string Password)
            F_4_4()
            => (BacnetReinitializedStates.BACNET_REINIT_WARMSTART, "AbCdEfGh");

        public static DateTime F_4_7()
            => new DateTime(1992, 11, 17, 22, 45, 30).AddMilliseconds(700);

        public static string F_4_8_Name()
            => "OATemp";

        public static BacnetObjectId F_4_8_Id()
            => new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3);

        public static (int LowLimit, int HighLimit) F_4_9()
            => (3, 3);
    }
}
