namespace System.IO.BACnet.EventNotification.EventValues
{
    public class OutOfRange : EventValuesBase
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_OUT_OF_RANGE;

        public float ExceedingValue { get; set; }
        public BacnetBitString StatusFlags { get; set; }
        public float Deadband { get; set; }
        public float ExceededLimit { get; set; }
    }
}