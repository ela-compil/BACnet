namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfValue : EventValuesBase
    {
        public BacnetBitString ChangedBits { get; set; }
        public float ChangeValue { get; set; }
        public BacnetCOVTypes Tag { get; set; }
        public BacnetBitString StatusFlags { get; set; }

        public override string ToString()
        {
            return $"ChangedBits: {ChangedBits}, ChangeValue: {ChangeValue}, "
                   + $"Tag: {Tag}, StatusFlags: {StatusFlags}";
        }
    }
}