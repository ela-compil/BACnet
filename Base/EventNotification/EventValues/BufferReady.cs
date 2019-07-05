namespace System.IO.BACnet.EventNotification.EventValues
{
    public class BufferReady : EventValuesBase
    {
        public override BacnetEventTypes EventType => BacnetEventTypes.EVENT_BUFFER_READY;

        public BacnetDeviceObjectPropertyReference BufferProperty { get; set; }
        public uint PreviousNotification { get; set; }
        public uint CurrentNotification { get; set; }
    }
}