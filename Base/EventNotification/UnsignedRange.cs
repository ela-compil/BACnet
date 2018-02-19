namespace System.IO.BACnet.EventNotification
{
    public class UnsignedRange : StateTransition
    {
        public uint ExceedingValue;
        public BacnetBitString StatusFlags;
        public uint ExceededLimit;

        public UnsignedRange(StateTransition transition) : base(transition)
        {
        }
    }
}