namespace System.IO.BACnet.EventNotification
{
    public class ChangeOfBitString : StateTransition
    {
        public BacnetBitString ReferencedBitString;
        public BacnetBitString StatusFlags;

        public ChangeOfBitString(StateTransition transition) : base(transition)
        {
        }
    }
}