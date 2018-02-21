namespace System.IO.BACnet.EventNotification.EventValues
{
    public class FloatingLimit : EventValuesBase
    {
        public float ReferenceValue { get; set; }
        public BacnetBitString StatusFlags { get; set; }
        public float SetPointValue { get; set; }
        public float ErrorLimit { get; set; }
    }
}