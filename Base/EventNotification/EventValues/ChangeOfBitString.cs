namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfBitString : EventValuesBase
    {
        public BacnetBitString ReferencedBitString { get; set; }
        public BacnetBitString StatusFlags { get; set; }
    }
}