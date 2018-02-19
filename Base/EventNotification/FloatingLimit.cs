namespace System.IO.BACnet.EventNotification
{
    public class FloatingLimit : StateTransition
    {
        public float ReferenceValue;
        public BacnetBitString StatusFlags;
        public float SetPointValue;
        public float ErrorLimit;

        public FloatingLimit(StateTransition transition) : base(transition)
        {
        }
    }
}