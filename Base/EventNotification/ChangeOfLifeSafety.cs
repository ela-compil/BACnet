namespace System.IO.BACnet.EventNotification
{
    public class ChangeOfLifeSafety : StateTransition
    {
        public BacnetLifeSafetyStates NewState;
        public BacnetLifeSafetyModes NewMode;
        public BacnetBitString StatusFlags;
        public BacnetLifeSafetyOperations OperationExpected;

        public ChangeOfLifeSafety(StateTransition transition) : base(transition)
        {
        }
    }
}