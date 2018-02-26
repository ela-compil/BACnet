namespace System.IO.BACnet.EventNotification.EventValues
{
    public class UnsignedRange : EventValuesBase
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_UNSIGNED_RANGE;

        public uint ExceedingValue { get; set; }
        public BacnetBitString StatusFlags { get; set; }
        public uint ExceededLimit { get; set; }
    }
}