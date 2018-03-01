using System.IO.BACnet.Serialize;

namespace System.IO.BACnet.Tests.TestData
{
    public static class ASHRAE
    {
        public static (BacnetLogRecord Record1, BacnetLogRecord Record2, BacnetObjectId ObjectId, BacnetPropertyIds
            PropertyId, BacnetBitString Flags, uint ItemCount, BacnetReadRangeRequestTypes RequestType, uint FirstSequence
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
