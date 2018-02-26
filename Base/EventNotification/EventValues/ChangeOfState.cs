namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfState : EventValuesBase
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_CHANGE_OF_STATE;

        public BacnetPropertyState NewState { get; set; }
        public BacnetBitString StatusFlags { get; set; }
    }
}