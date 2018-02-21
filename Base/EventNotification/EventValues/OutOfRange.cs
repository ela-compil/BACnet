namespace System.IO.BACnet.EventNotification.EventValues
{
    public class OutOfRange : EventValuesBase
    {
        public float ExceedingValue { get; set; }
        public BacnetBitString StatusFlags { get; set; }
        public float Deadband { get; set; }
        public float ExceededLimit { get; set; }

        public override string ToString()
        {
            return $"ExceedingValue: {ExceedingValue}, StatusFlags: {StatusFlags}, "
                   + $"Deadband: {Deadband}, ExceededLimit: {ExceededLimit}";
        }
    }
}