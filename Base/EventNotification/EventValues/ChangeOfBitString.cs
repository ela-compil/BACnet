namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfBitString : EventValuesBase
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_CHANGE_OF_BITSTRING;

        public BacnetBitString ReferencedBitString { get; set; }
        public BacnetBitString StatusFlags { get; set; }
    }
}