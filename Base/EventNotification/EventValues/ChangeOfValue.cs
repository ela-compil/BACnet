namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfValue : EventValuesBase
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_CHANGE_OF_VALUE;

        public BacnetBitString ChangedBits { get; set; }
        public float ChangeValue { get; set; }
        public BacnetCOVTypes Tag { get; set; }
        public BacnetBitString StatusFlags { get; set; }
    }
}