namespace System.IO.BACnet.EventNotification.EventValues
{
    public class OutOfRange : EventValuesBase
    {
        public float ExceedingValue;
        public BacnetBitString StatusFlags;
        public float Deadband;
        public float ExceededLimit;
    }
}