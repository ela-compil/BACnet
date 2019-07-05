namespace System.IO.BACnet
{
    public struct BacnetAlarmSummaryData
    {
        public BacnetObjectId ObjectIdentifier { get; }
        public BacnetEventStates AlarmState { get; }
        public BacnetBitString AcknowledgedTransitions { get; }

        public BacnetAlarmSummaryData(BacnetObjectId objectIdentifier, BacnetEventStates alarmState, BacnetBitString acknowledgedTransitions)
        {
            ObjectIdentifier = objectIdentifier;
            AlarmState = alarmState;
            AcknowledgedTransitions = acknowledgedTransitions;
        }
    }
}