namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfValue : EventValuesBase
    {
        public BacnetBitString ChangedBits;
        public float ChangeValue;
        public BacnetCOVTypes Tag;
        public BacnetBitString StatusFlags;
    }
}