namespace System.IO.BACnet.EventNotification
{
    public class ChangeOfState : StateTransition
    {
        public BacnetPropetyState NewState;
        public BacnetBitString StatusFlags;

        public ChangeOfState(StateTransition transition) : base(transition)
        {
        }
    }
}