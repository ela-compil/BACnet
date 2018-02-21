namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfLifeSafety : EventValuesBase
    {
        public BacnetLifeSafetyStates NewState { get; set; }
        public BacnetLifeSafetyModes NewMode { get; set; }
        public BacnetBitString StatusFlags { get; set; }
        public BacnetLifeSafetyOperations OperationExpected { get; set; }
    }
}