namespace System.IO.BACnet.EventNotification.EventValues
{
    public class UnsignedRange : EventValuesBase
    {
        public uint ExceedingValue;
        public BacnetBitString StatusFlags;
        public uint ExceededLimit;
    }
}