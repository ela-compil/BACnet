namespace System.IO.BACnet.EventNotification.EventValues
{
    public class FloatingLimit : EventValuesBase
    {
        public float ReferenceValue;
        public BacnetBitString StatusFlags;
        public float SetPointValue;
        public float ErrorLimit;
    }
}