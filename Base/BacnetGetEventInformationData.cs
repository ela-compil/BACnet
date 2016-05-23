namespace System.IO.BACnet
{
    public struct BacnetGetEventInformationData
    {
        public BacnetObjectId objectIdentifier;
        public BacnetEventNotificationData.BacnetEventStates eventState;
        public BacnetBitString acknowledgedTransitions;
        public BacnetGenericTime[] eventTimeStamps;    //3
        public BacnetEventNotificationData.BacnetNotifyTypes notifyType;
        public BacnetBitString eventEnable;
        public uint[] eventPriorities;     //3
    }
}