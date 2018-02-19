namespace System.IO.BACnet.EventNotification
{
    public class BufferReady : StateTransition
    {
        public BacnetDeviceObjectPropertyReference BufferProperty;
        public uint PreviousNotification;
        public uint CurrentNotification;

        public BufferReady(StateTransition transition) : base(transition)
        {
        }
    }
}