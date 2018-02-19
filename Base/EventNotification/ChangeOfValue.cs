namespace System.IO.BACnet.EventNotification
{
    public class ChangeOfValue : StateTransition
    {
        public BacnetBitString ChangedBits;
        public float ChangeValue;
        public BacnetCOVTypes Tag;
        public BacnetBitString StatusFlags;

        public ChangeOfValue(StateTransition transition) : base(transition)
        {
        }
    }
}