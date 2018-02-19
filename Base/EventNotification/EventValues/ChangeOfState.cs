namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfState : EventValuesBase
    {
        public BacnetPropertyState NewState;
        public BacnetBitString StatusFlags;
    }
}