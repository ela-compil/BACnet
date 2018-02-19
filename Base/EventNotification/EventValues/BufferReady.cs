namespace System.IO.BACnet.EventNotification.EventValues
{
    public class BufferReady : EventValuesBase
    {
        public BacnetDeviceObjectPropertyReference BufferProperty;
        public uint PreviousNotification;
        public uint CurrentNotification;
    }
}