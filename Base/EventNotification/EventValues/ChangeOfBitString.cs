namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfBitString : EventValuesBase
    {
        public BacnetBitString ReferencedBitString;
        public BacnetBitString StatusFlags;
    }
}