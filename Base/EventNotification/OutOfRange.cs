namespace System.IO.BACnet.EventNotification
{
    public class OutOfRange : StateTransition
    {
        public float ExceedingValue;
        public BacnetBitString StatusFlags;
        public float Deadband;
        public float ExceededLimit;

        public OutOfRange(StateTransition transition) : base(transition)
        {
        }
    }
}