namespace System.IO.BACnet
{
    public struct BacnetAlarmSummaryData
    {
        public BacnetObjectId objectIdentifier;
        public BacnetEventStates alarmState;
        public BacnetBitString acknowledgedTransitions;
    }
}