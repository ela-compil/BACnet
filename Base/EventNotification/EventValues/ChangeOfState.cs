namespace System.IO.BACnet.EventNotification.EventValues
{
    public class ChangeOfState : EventValuesBase
    {
        public BacnetPropertyState NewState { get; set; }
        public BacnetBitString StatusFlags { get; set; }

        public override string ToString()
        {
            return $"NewState: {NewState}, StatusFlags: {StatusFlags}";
        }
    }
}