namespace System.IO.BACnet.EventNotification.EventValues
{
    public class BufferReady : EventValuesBase
    {
        public BacnetDeviceObjectPropertyReference BufferProperty { get; set; }
        public uint PreviousNotification { get; set; }
        public uint CurrentNotification { get; set; }
    }
}