namespace System.IO.BACnet
{
    public struct BacnetEventNotificationData
    {
        public uint processIdentifier;
        public BacnetObjectId initiatingObjectIdentifier;
        public BacnetObjectId eventObjectIdentifier;
        public BacnetGenericTime timeStamp;
        public uint notificationClass;
        public byte priority;
        public BacnetEventTypes eventType;
        public string messageText;       /* OPTIONAL - Set to NULL if not being used */
        public BacnetNotifyTypes notifyType;
        public bool ackRequired;
        public BacnetEventStates fromState;
        public BacnetEventStates toState;

        /*
         ** Each of these structures in the union maps to a particular eventtype
         ** Based on BACnetNotificationParameters
         */

        /*
         ** EVENT_CHANGE_OF_BITSTRING
         */
        public BacnetBitString changeOfBitstring_referencedBitString;
        public BacnetBitString changeOfBitstring_statusFlags;
        /*
         ** EVENT_CHANGE_OF_STATE
         */
        public BacnetPropetyState changeOfState_newState;
        public BacnetBitString changeOfState_statusFlags;
        /*
         ** EVENT_CHANGE_OF_VALUE
         */
        public BacnetBitString changeOfValue_changedBits;
        public float changeOfValue_changeValue;
        public BacnetCOVTypes changeOfValue_tag;
        public BacnetBitString changeOfValue_statusFlags;
        /*
         ** EVENT_COMMAND_FAILURE
         **
         ** Not Supported!
         */
        /*
         ** EVENT_FLOATING_LIMIT
         */
        public float floatingLimit_referenceValue;
        public BacnetBitString floatingLimit_statusFlags;
        public float floatingLimit_setPointValue;
        public float floatingLimit_errorLimit;
        /*
         ** EVENT_OUT_OF_RANGE
         */
        public float outOfRange_exceedingValue;
        public BacnetBitString outOfRange_statusFlags;
        public float outOfRange_deadband;
        public float outOfRange_exceededLimit;
        /*
         ** EVENT_CHANGE_OF_LIFE_SAFETY
         */
        public BacnetLifeSafetyStates changeOfLifeSafety_newState;
        public BacnetLifeSafetyModes changeOfLifeSafety_newMode;
        public BacnetBitString changeOfLifeSafety_statusFlags;
        public BacnetLifeSafetyOperations changeOfLifeSafety_operationExpected;
        /*
         ** EVENT_EXTENDED
         **
         ** Not Supported!
         */
        /*
         ** EVENT_BUFFER_READY
         */
        public BacnetDeviceObjectPropertyReference bufferReady_bufferProperty;
        public uint bufferReady_previousNotification;
        public uint bufferReady_currentNotification;
        /*
         ** EVENT_UNSIGNED_RANGE
         */
        public uint unsignedRange_exceedingValue;
        public BacnetBitString unsignedRange_statusFlags;
        public uint unsignedRange_exceededLimit;
    };
}