namespace System.IO.BACnet.EventNotification.EventValues
{
    public class FloatingLimit : EventValuesBase
    {
        public float ReferenceValue { get; set; }
        public BacnetBitString StatusFlags { get; set; }
        public float SetPointValue { get; set; }
        public float ErrorLimit { get; set; }

        public override string ToString()
        {
            return $"ReferenceValue: {ReferenceValue}, StatusFlags: {StatusFlags}, "
                   + $"SetPointValue: {SetPointValue}, ErrorLimit: {ErrorLimit}";
        }
    }
}