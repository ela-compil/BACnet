namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfLifeSafety : EventValuesBase
    {
        public BacnetLifeSafetyStates NewState;
        public BacnetLifeSafetyModes NewMode;
        public BacnetBitString StatusFlags;
        public BacnetLifeSafetyOperations OperationExpected;
    }
}