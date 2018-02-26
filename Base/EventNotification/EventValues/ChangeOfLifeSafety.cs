namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfLifeSafety : EventValuesBase
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_CHANGE_OF_LIFE_SAFETY;

        public BacnetLifeSafetyStates NewState { get; set; }
        public BacnetLifeSafetyModes NewMode { get; set; }
        public BacnetBitString StatusFlags { get; set; }
        public BacnetLifeSafetyOperations OperationExpected { get; set; }
    }
}